// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.OData;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.Restier.AspNetCore.Batch
{
    /// <summary>
    /// Resolves $ContentId dependencies within OData batch changeset requests.
    /// </summary>
    internal static class ChangeSetDependencyResolver
    {
        /// <summary>
        /// Regex pattern that matches $ContentId references in URLs.
        /// </summary>
        private static readonly Regex ContentIdPattern = new Regex(
            @"\$([A-Za-z0-9\-._~]+)",
            RegexOptions.Compiled);

        /// <summary>
        /// OData system query options that use a $ prefix but are not ContentId references.
        /// </summary>
        private static readonly HashSet<string> ODataSystemQueryOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "filter", "orderby", "select", "expand", "top", "skip", "count",
            "search", "format", "compute", "index", "schemaversion", "batch",
            "crossjoin", "all", "entity", "root", "id", "ref", "value",
            "metadata", "type", "levels", "apply",
        };

        /// <summary>
        /// Scans the content-id-to-URL mapping for $ContentId references and returns a dependency map.
        /// </summary>
        /// <param name="contentIdToUrl">A dictionary mapping ContentId values to their request URLs.</param>
        /// <returns>
        /// A dictionary where each key is a ContentId whose URL references other ContentIds,
        /// and the value is the list of referenced ContentIds. Only entries with dependencies are included.
        /// </returns>
        public static Dictionary<string, List<string>> DetectDependencies(IDictionary<string, string> contentIdToUrl)
        {
            Ensure.NotNull(contentIdToUrl, nameof(contentIdToUrl));

            var dependencies = new Dictionary<string, List<string>>();

            foreach (var kvp in contentIdToUrl)
            {
                var contentId = kvp.Key;
                var url = kvp.Value;

                var matches = ContentIdPattern.Matches(url);
                var deps = new List<string>();

                foreach (Match match in matches)
                {
                    var referencedId = match.Groups[1].Value;

                    if (ODataSystemQueryOptions.Contains(referencedId))
                    {
                        continue;
                    }

                    if (contentIdToUrl.ContainsKey(referencedId) && !deps.Contains(referencedId))
                    {
                        deps.Add(referencedId);
                    }
                }

                if (deps.Count > 0)
                {
                    dependencies[contentId] = deps;
                }
            }

            return dependencies;
        }

        /// <summary>
        /// Computes the expected entity URL for a request based on its HTTP method and the EDM model.
        /// </summary>
        /// <param name="context">The HTTP context for the request.</param>
        /// <param name="model">The EDM model.</param>
        /// <returns>
        /// The expected entity URL, or null if the URL cannot be computed.
        /// For PUT/PATCH/DELETE, returns the request URL. For POST, constructs the entity URL from key values in the body.
        /// </returns>
        public static string ComputeExpectedEntityUrl(HttpContext context, IEdmModel model)
        {
            Ensure.NotNull(context, nameof(context));
            Ensure.NotNull(model, nameof(model));

            var method = context.Request.Method;

            if (string.Equals(method, HttpMethods.Put, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, HttpMethods.Patch, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, HttpMethods.Delete, StringComparison.OrdinalIgnoreCase))
            {
                return context.Request.GetEncodedUrl();
            }

            if (string.Equals(method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
            {
                return ComputePostEntityUrl(context, model);
            }

            return null;
        }

        /// <summary>
        /// Pre-resolves $ContentId references in dependent request URLs by computing expected entity URLs
        /// for referenced requests and updating dependent request URLs accordingly.
        /// Dependencies are processed in topological order so that chained references (A→B→C) resolve correctly.
        /// </summary>
        /// <param name="contexts">The HTTP contexts for all requests in the changeset.</param>
        /// <param name="dependencies">The dependency map from <see cref="DetectDependencies"/>.</param>
        /// <param name="model">The EDM model.</param>
        /// <param name="contentIdToLocationMapping">The mapping to pre-populate with resolved entity URLs.</param>
        /// <returns>True if all references were resolved; false if any resolution failed.</returns>
        public static bool PreResolveContentIdReferences(
            IEnumerable<HttpContext> contexts,
            Dictionary<string, List<string>> dependencies,
            IEdmModel model,
            IDictionary<string, string> contentIdToLocationMapping)
        {
            Ensure.NotNull(contexts, nameof(contexts));
            Ensure.NotNull(dependencies, nameof(dependencies));
            Ensure.NotNull(model, nameof(model));
            Ensure.NotNull(contentIdToLocationMapping, nameof(contentIdToLocationMapping));

            // Build lookup: ContentId -> HttpContext
            var contentIdToContext = new Dictionary<string, HttpContext>();
            foreach (var ctx in contexts)
            {
                var contentId = ctx.Request.GetODataContentId();
                if (!string.IsNullOrEmpty(contentId))
                {
                    contentIdToContext[contentId] = ctx;
                }
            }

            // Process in topological order: compute entity URL for a request only after
            // its own dependencies have been resolved. This handles chained references (A→B→C).
            var resolved = new HashSet<string>();
            var allDependentIds = new HashSet<string>(dependencies.Keys);

            // First pass: compute entity URLs for requests that are NOT themselves dependent
            // (i.e., they are referenced but don't reference others).
            var referencedIds = dependencies.Values.SelectMany(d => d).Distinct();
            foreach (var referencedId in referencedIds)
            {
                if (allDependentIds.Contains(referencedId))
                {
                    continue; // This request is itself dependent; handle in topological order below
                }

                if (!contentIdToContext.TryGetValue(referencedId, out var referencedContext))
                {
                    return false;
                }

                var entityUrl = ComputeExpectedEntityUrl(referencedContext, model);
                if (entityUrl is null)
                {
                    return false;
                }

                contentIdToLocationMapping[referencedId] = entityUrl;
                resolved.Add(referencedId);
            }

            // Iteratively resolve dependent requests whose dependencies are all resolved.
            // This handles chains: once A is resolved, B (which depends on A) can be resolved,
            // then C (which depends on B) can be resolved.
            var remaining = new HashSet<string>(allDependentIds);
            while (remaining.Count > 0)
            {
                var resolvedThisRound = new List<string>();

                foreach (var dependentId in remaining)
                {
                    var deps = dependencies[dependentId];
                    if (!deps.All(d => resolved.Contains(d)))
                    {
                        continue; // Not all dependencies resolved yet
                    }

                    if (!contentIdToContext.TryGetValue(dependentId, out var dependentContext))
                    {
                        return false;
                    }

                    // Resolve $ContentId references in this request's URL
                    ResolveRequestUrl(dependentContext, contentIdToLocationMapping);

                    // If this request is itself referenced by others, compute its entity URL now
                    if (referencedIds.Contains(dependentId))
                    {
                        var entityUrl = ComputeExpectedEntityUrl(dependentContext, model);
                        if (entityUrl is null)
                        {
                            return false;
                        }

                        contentIdToLocationMapping[dependentId] = entityUrl;
                    }

                    resolvedThisRound.Add(dependentId);
                }

                if (resolvedThisRound.Count == 0)
                {
                    return false; // Circular dependency or unresolvable
                }

                foreach (var id in resolvedThisRound)
                {
                    resolved.Add(id);
                    remaining.Remove(id);
                }
            }

            return true;
        }

        /// <summary>
        /// Resolves $ContentId references in a request's URL path and updates the request.
        /// Works with the path portion to avoid producing doubled scheme://host URLs.
        /// </summary>
        private static void ResolveRequestUrl(
            HttpContext context,
            IDictionary<string, string> contentIdToLocationMapping)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var query = context.Request.QueryString.Value ?? string.Empty;

            // The path is like "/$1" or "/$1/Details". Strip leading "/" then resolve.
            var trimmedPath = path.TrimStart('/');
            var match = ContentIdPattern.Match(trimmedPath);

            if (!match.Success || match.Index != 0)
            {
                return;
            }

            var referencedId = match.Groups[1].Value;
            if (ODataSystemQueryOptions.Contains(referencedId))
            {
                return;
            }

            if (!contentIdToLocationMapping.TryGetValue(referencedId, out var entityUrl))
            {
                return;
            }

            // Build the resolved URL: entity URL + any suffix after the $ContentId + query string
            var suffix = trimmedPath.Substring(match.Length);
            var resolvedUrl = entityUrl + suffix + query;

            if (Uri.TryCreate(resolvedUrl, UriKind.Absolute, out var resolvedUri))
            {
                context.Request.Scheme = resolvedUri.Scheme;
                context.Request.Host = new HostString(resolvedUri.Authority);
                context.Request.Path = resolvedUri.AbsolutePath;
                context.Request.QueryString = new QueryString(resolvedUri.Query);
            }
        }

        /// <summary>
        /// Resolves a leading $ContentId reference in a request-line URL to the referenced entity URL.
        /// The reference must be the first path segment (e.g. <c>$1/Books</c> or
        /// <c>http://host/$1/Books</c>); the scheme/host/$ContentId portion is replaced by the entity
        /// URL and any trailing path plus query string is preserved. Unlike <see cref="ResolveContentIdInUrl"/>,
        /// this substitutes the whole entity-addressing prefix rather than the bare <c>$N</c> token, so an
        /// absolute source URL does not end up with a doubled scheme/host.
        /// </summary>
        /// <param name="url">The request-line URL that may begin with a $ContentId reference.</param>
        /// <param name="contentIdToLocationMapping">The mapping of ContentId to entity URL.</param>
        /// <returns>The resolved URL, or the original URL when no leading reference resolves.</returns>
        internal static string ResolveContentIdReferenceUrl(string url, IDictionary<string, string> contentIdToLocationMapping)
        {
            Ensure.NotNull(url, nameof(url));
            Ensure.NotNull(contentIdToLocationMapping, nameof(contentIdToLocationMapping));

            string path;
            var query = string.Empty;

            if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            {
                path = absolute.AbsolutePath;
                query = absolute.Query;
            }
            else
            {
                var queryIndex = url.IndexOf('?');
                if (queryIndex >= 0)
                {
                    path = url.Substring(0, queryIndex);
                    query = url.Substring(queryIndex);
                }
                else
                {
                    path = url;
                }
            }

            var trimmedPath = path.TrimStart('/');
            var match = ContentIdPattern.Match(trimmedPath);

            if (!match.Success || match.Index != 0)
            {
                return url;
            }

            var referencedId = match.Groups[1].Value;
            if (ODataSystemQueryOptions.Contains(referencedId))
            {
                return url;
            }

            if (!contentIdToLocationMapping.TryGetValue(referencedId, out var entityUrl))
            {
                return url;
            }

            var suffix = trimmedPath.Substring(match.Length);
            return entityUrl + suffix + query;
        }

        /// <summary>
        /// Resolves $ContentId references in a URL by replacing them with the corresponding entity URLs.
        /// </summary>
        /// <param name="url">The URL that may contain $ContentId references.</param>
        /// <param name="contentIdToLocationMapping">The mapping of ContentId to entity URL.</param>
        /// <returns>The URL with all $ContentId references resolved.</returns>
        internal static string ResolveContentIdInUrl(string url, IDictionary<string, string> contentIdToLocationMapping)
        {
            Ensure.NotNull(url, nameof(url));
            Ensure.NotNull(contentIdToLocationMapping, nameof(contentIdToLocationMapping));

            return ContentIdPattern.Replace(url, match =>
            {
                var referencedId = match.Groups[1].Value;

                if (ODataSystemQueryOptions.Contains(referencedId))
                {
                    return match.Value;
                }

                if (contentIdToLocationMapping.TryGetValue(referencedId, out var resolvedUrl))
                {
                    return resolvedUrl;
                }

                return match.Value;
            });
        }

        /// <summary>
        /// Computes the expected entity URL for a raw batch sub-request that has not yet been
        /// materialized into an <see cref="HttpContext"/>. Used during batch parsing, before the
        /// framework validates each sub-request URI against the OData service root.
        /// </summary>
        /// <param name="method">The HTTP method of the sub-request.</param>
        /// <param name="url">The absolute request URL of the sub-request.</param>
        /// <param name="body">The request body stream (read for POST key extraction).</param>
        /// <param name="model">The EDM model.</param>
        /// <returns>
        /// The expected entity URL, or null if it cannot be computed. For PUT/PATCH/DELETE, returns
        /// the request URL. For POST, constructs the entity URL from key values in the body.
        /// </returns>
        public static string ComputeEntityUrl(string method, Uri url, Stream body, IEdmModel model)
        {
            Ensure.NotNull(method, nameof(method));
            Ensure.NotNull(url, nameof(url));
            Ensure.NotNull(model, nameof(model));

            if (!url.IsAbsoluteUri)
            {
                return null;
            }

            if (string.Equals(method, HttpMethods.Put, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, HttpMethods.Patch, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, HttpMethods.Delete, StringComparison.OrdinalIgnoreCase))
            {
                return url.AbsoluteUri;
            }

            if (string.Equals(method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
            {
                return ComputePostEntityUrlCore(url.AbsolutePath, url.AbsoluteUri, body, model);
            }

            return null;
        }

        /// <summary>
        /// Computes the entity URL for a POST request by extracting key values from the request body.
        /// </summary>
        private static string ComputePostEntityUrl(HttpContext context, IEdmModel model)
        {
            var request = context.Request;
            return ComputePostEntityUrlCore(request.Path.Value, request.GetEncodedUrl(), request.Body, model);
        }

        /// <summary>
        /// Computes the entity URL for a POST from its path, encoded URL, body, and the EDM model.
        /// </summary>
        private static string ComputePostEntityUrlCore(string path, string encodedUrl, Stream body, IEdmModel model)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // Extract entity set name from the last path segment
            var segments = path.TrimEnd('/').Split('/');
            var entitySetName = segments[segments.Length - 1];

            // Find the entity set in the model
            var container = model.EntityContainer;
            if (container is null)
            {
                return null;
            }

            var entitySet = container.FindEntitySet(entitySetName);
            if (entitySet is null)
            {
                return null;
            }

            var entityType = entitySet.EntityType;
            var keyProperties = entityType.Key().ToList();

            if (keyProperties.Count == 0)
            {
                return null;
            }

            // Extract key values from the request body
            var keyValues = ExtractKeyValuesFromBody(body, keyProperties);
            if (keyValues is null)
            {
                return null;
            }

            var keySegment = FormatKeySegment(keyValues);
            var postUrl = encodedUrl.TrimEnd('/');

            return $"{postUrl}({keySegment})";
        }

        /// <summary>
        /// Extracts key property values from a JSON request body.
        /// </summary>
        private static Dictionary<string, string> ExtractKeyValuesFromBody(
            Stream body,
            List<IEdmStructuralProperty> keyProperties)
        {
            if (body is null || !body.CanRead)
            {
                return null;
            }

            var originalPosition = body.CanSeek ? body.Position : -1;

            try
            {
                if (body.CanSeek)
                {
                    body.Position = 0;
                }

                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                var keyValues = new Dictionary<string, string>();

                foreach (var keyProperty in keyProperties)
                {
                    if (!root.TryGetProperty(keyProperty.Name, out var jsonValue))
                    {
                        return null;
                    }

                    var edmType = keyProperty.Type.PrimitiveKind();
                    var clrValue = ConvertJsonToClrValue(jsonValue, edmType);

                    if (clrValue is null)
                    {
                        return null;
                    }

                    var uriLiteral = ODataUriUtils.ConvertToUriLiteral(clrValue, ODataVersion.V4);
                    keyValues[keyProperty.Name] = uriLiteral;
                }

                return keyValues;
            }
            catch (JsonException)
            {
                return null;
            }
            finally
            {
                if (body.CanSeek && originalPosition >= 0)
                {
                    body.Position = originalPosition;
                }
            }
        }

        /// <summary>
        /// Converts a JSON element to a CLR value based on the EDM primitive type.
        /// </summary>
        private static object ConvertJsonToClrValue(JsonElement element, EdmPrimitiveTypeKind typeKind)
        {
            switch (typeKind)
            {
                case EdmPrimitiveTypeKind.Guid:
                    if (element.TryGetGuid(out var guidValue))
                    {
                        return guidValue;
                    }

                    return null;

                case EdmPrimitiveTypeKind.Int16:
                    if (element.TryGetInt16(out var int16Value))
                    {
                        return int16Value;
                    }

                    return null;

                case EdmPrimitiveTypeKind.Int32:
                    if (element.TryGetInt32(out var int32Value))
                    {
                        return int32Value;
                    }

                    return null;

                case EdmPrimitiveTypeKind.Int64:
                    if (element.TryGetInt64(out var int64Value))
                    {
                        return int64Value;
                    }

                    return null;

                case EdmPrimitiveTypeKind.String:
                    return element.GetString();

                case EdmPrimitiveTypeKind.Boolean:
                    if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                    {
                        return element.GetBoolean();
                    }

                    return null;

                case EdmPrimitiveTypeKind.Decimal:
                    if (element.TryGetDecimal(out var decimalValue))
                    {
                        return decimalValue;
                    }

                    return null;

                case EdmPrimitiveTypeKind.Double:
                    if (element.TryGetDouble(out var doubleValue))
                    {
                        return doubleValue;
                    }

                    return null;

                case EdmPrimitiveTypeKind.Single:
                    if (element.TryGetSingle(out var singleValue))
                    {
                        return singleValue;
                    }

                    return null;

                case EdmPrimitiveTypeKind.DateTimeOffset:
                    if (element.TryGetDateTimeOffset(out var dateTimeOffsetValue))
                    {
                        return dateTimeOffsetValue;
                    }

                    return null;

                default:
                    // For unsupported types, try to use the raw text
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        return element.GetString();
                    }

                    return null;
            }
        }

        /// <summary>
        /// Formats key values into an OData key segment string.
        /// Single keys use the value directly; composite keys use name=value pairs.
        /// </summary>
        private static string FormatKeySegment(Dictionary<string, string> keyValues)
        {
            if (keyValues.Count == 1)
            {
                return keyValues.Values.First();
            }

            var parts = keyValues.Select(kvp => $"{kvp.Key}={kvp.Value}");
            return string.Join(",", parts);
        }
    }
}
