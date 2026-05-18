// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// A <see cref="MatcherPolicy"/> that augments the matched <see cref="Microsoft.AspNetCore.Http.Endpoint"/>
/// for a Restier route with any <see cref="Microsoft.AspNetCore.Authorization.IAuthorizeData"/> or
/// <see cref="Microsoft.AspNetCore.Authorization.IAllowAnonymous"/> attributes found on the user's
/// <see cref="Core.ApiBase"/> subclass, its <see cref="Model.ResourceAttribute"/>-decorated
/// properties, or its <see cref="Model.BoundOperationAttribute"/> /
/// <see cref="Model.UnboundOperationAttribute"/> methods.
/// </summary>
internal sealed class RestierAuthorizationMetadataPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
    private const string ClassKey = "class";
    private const string OperationPrefix = "operation:";

    private readonly IOptions<ODataOptions> odataOptions;
    private readonly ConcurrentDictionary<(Type apiType, string targetKey), object[]> attributeCache = new();

    public RestierAuthorizationMetadataPolicy(IOptions<ODataOptions> odataOptions)
    {
        this.odataOptions = odataOptions ?? throw new ArgumentNullException(nameof(odataOptions));
    }

    /// <summary>
    /// Maps an <see cref="ODataPath"/> to a stable string key identifying the user-code target
    /// whose attributes should be honored: either the API class (the default) or a named
    /// operation method. The key doubles as a cache key for the discovered attribute list.
    /// Entity-set and singleton paths return <c>"class"</c> — see <c>DbSet-backed entity sets</c>
    /// in the design spec for why per-entity-set placement isn't supported (the standard
    /// <c>[AllowAnonymous]</c> / <c>[Authorize]</c> attributes target <c>class | method</c> only,
    /// so there is no anchor for them on an entity-set property).
    /// </summary>
    internal static string ComputeTargetKey(ODataPath path)
    {
        if (path is null || path.Count == 0)
        {
            return ClassKey;
        }

        var lastSegment = path.LastOrDefault();
        if (lastSegment is MetadataSegment)
        {
            return ClassKey;
        }

        // Operations are the only non-class surface where standard auth attributes can land.
        // A bound operation (path ending in OperationSegment) overrides the entity-set's class-level attribute.
        if (lastSegment is OperationImportSegment opImport)
        {
            var op = opImport.OperationImports.FirstOrDefault();
            return op is null ? ClassKey : OperationPrefix + op.Name;
        }
        if (lastSegment is OperationSegment opSeg)
        {
            var op = opSeg.Operations.FirstOrDefault();
            return op is null ? ClassKey : OperationPrefix + op.Name;
        }

        return ClassKey;
    }

    private static readonly object[] EmptyAttributes = Array.Empty<object>();

    /// <summary>
    /// Reflects on <paramref name="apiType"/> and the target identified by <paramref name="targetKey"/>
    /// (one of <c>"class"</c>, <c>"resource:Name"</c>, or <c>"operation:Name"</c>) to collect every
    /// <see cref="IAuthorizeData"/> and <see cref="IAllowAnonymous"/> attribute placed on the API class
    /// and (where applicable) on a <see cref="ResourceAttribute"/>-decorated property or a
    /// <see cref="BoundOperationAttribute"/> / <see cref="UnboundOperationAttribute"/>-decorated method.
    /// Class attributes come first, member attributes second; ASP.NET Core's
    /// <c>AuthorizationMiddleware</c> applies its standard "AllowAnonymous wins" precedence later.
    /// Returns an empty array when nothing is found, so callers can fast-path-skip.
    /// </summary>
    internal static object[] DiscoverAttributes(Type apiType, string targetKey)
    {
        if (apiType is null) throw new ArgumentNullException(nameof(apiType));
        if (targetKey is null) throw new ArgumentNullException(nameof(targetKey));

        var classAttrs = CollectAuthAttributes(apiType.GetCustomAttributes(inherit: true));
        var memberAttrs = CollectMemberAttributes(apiType, targetKey);

        if (classAttrs.Count == 0 && memberAttrs.Count == 0)
        {
            return EmptyAttributes;
        }

        var combined = new object[classAttrs.Count + memberAttrs.Count];
        classAttrs.CopyTo(combined, 0);
        memberAttrs.CopyTo(combined, classAttrs.Count);
        return combined;
    }

    private static List<object> CollectMemberAttributes(Type apiType, string targetKey)
    {
        if (targetKey.StartsWith(OperationPrefix, StringComparison.Ordinal))
        {
            var name = targetKey.Substring(OperationPrefix.Length);
            var method = apiType.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            // The method must be a real Restier operation — otherwise we'd be honoring attributes
            // on arbitrary methods, which would surprise users.
            if (method is null
                || (!method.IsDefined(typeof(BoundOperationAttribute), inherit: true)
                    && !method.IsDefined(typeof(UnboundOperationAttribute), inherit: true)))
            {
                return new List<object>(0);
            }

            return CollectAuthAttributes(method.GetCustomAttributes(inherit: true));
        }

        return new List<object>(0);
    }

    private static List<object> CollectAuthAttributes(object[] attributes)
    {
        var result = new List<object>(attributes.Length);
        foreach (var attr in attributes)
        {
            if (attr is IAuthorizeData || attr is IAllowAnonymous)
            {
                result.Add(attr);
            }
        }
        return result;
    }

    /// <inheritdoc/>
    // DynamicControllerEndpointMatcherPolicy.Order == int.MinValue + 100. We run after it so the
    // OData path is already parsed and the candidate endpoint is the RestierController action.
    public override int Order => int.MinValue + 110;

    /// <inheritdoc/>
    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        // Fast path: only engage if at least one candidate endpoint is a RestierController action.
        // The dynamic-route trick routes every Restier request through this controller.
        for (var i = 0; i < endpoints.Count; i++)
        {
            var descriptor = endpoints[i].Metadata.GetMetadata<ControllerActionDescriptor>();
            if (descriptor?.ControllerTypeInfo.AsType() == typeof(RestierController))
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        // Locate the Restier route this request belongs to. ODataFeature.RoutePrefix is set by
        // RestierRouteValueTransformer earlier in the routing pipeline. From there we reach the
        // per-route service provider where the marker (carrying the API type) was registered.
        var routePrefix = httpContext.ODataFeature().RoutePrefix ?? string.Empty;
        var routeServices = odataOptions.Value.GetRouteServices(routePrefix);
        var marker = routeServices?.GetService<RestierRouteMarker>();
        if (marker is null)
        {
            return Task.CompletedTask;
        }

        var path = httpContext.ODataFeature().Path;
        var targetKey = ComputeTargetKey(path);
        var cacheKey = (marker.ApiType, targetKey);

        var attributes = attributeCache.GetOrAdd(
            cacheKey,
            static key => DiscoverAttributes(key.apiType, key.targetKey));

        if (attributes.Length == 0)
        {
            // No auth metadata to add — fastest path: skip allocation entirely.
            return Task.CompletedTask;
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            var candidate = candidates[i];
            var descriptor = candidate.Endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (descriptor?.ControllerTypeInfo.AsType() != typeof(RestierController))
            {
                continue;
            }

            // Build a fresh wrapped endpoint per candidate. This is intentional:
            // the same (apiType, targetKey) tuple can map to different RestierController actions
            // (Get / Post / Put / …) depending on HTTP method, and different route prefixes.
            // We cache the attribute LIST, never the wrapped endpoint, so candidates always get
            // metadata appropriate to the actual underlying action.
            var wrapped = WrapEndpoint(candidate.Endpoint, attributes);
            candidates.ReplaceEndpoint(i, wrapped, candidate.Values);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Builds a fresh <see cref="Endpoint"/> whose metadata is the original's metadata concatenated
    /// with the discovered auth attributes.
    /// </summary>
    internal static Endpoint WrapEndpoint(Endpoint original, object[] extraAttributes)
    {
        var originalMetadata = original.Metadata;
        var combined = new object[originalMetadata.Count + extraAttributes.Length];
        var index = 0;
        foreach (var item in originalMetadata)
        {
            combined[index++] = item;
        }
        for (var i = 0; i < extraAttributes.Length; i++)
        {
            combined[index++] = extraAttributes[i];
        }
        var combinedMetadata = new EndpointMetadataCollection(combined);

        if (original is RouteEndpoint routeEndpoint)
        {
            return new RouteEndpoint(
                routeEndpoint.RequestDelegate,
                routeEndpoint.RoutePattern,
                routeEndpoint.Order,
                combinedMetadata,
                routeEndpoint.DisplayName);
        }

        return new Endpoint(original.RequestDelegate, combinedMetadata, original.DisplayName);
    }
}
