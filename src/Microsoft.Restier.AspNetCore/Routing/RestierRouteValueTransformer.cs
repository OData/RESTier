// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// A <see cref="DynamicRouteValueTransformer"/> that dynamically parses OData URLs at runtime,
/// populates the OData feature on the <see cref="HttpContext"/>, and routes requests
/// to the appropriate <see cref="RestierController"/> action.
/// </summary>
internal sealed class RestierRouteValueTransformer : DynamicRouteValueTransformer
{
    private const string ControllerName = "Restier";
    private const string MethodNameOfGet = "Get";
    private const string MethodNameOfPost = "Post";
    private const string MethodNameOfPut = "Put";
    private const string MethodNameOfPatch = "Patch";
    private const string MethodNameOfDelete = "Delete";
    private const string MethodNameOfPostAction = "PostAction";
    private const string MethodNameOfGetMetadata = "GetMetadata";
    private const string MethodNameOfGetServiceDocument = "GetServiceDocument";

    private readonly IOptions<ODataOptions> _odataOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestierRouteValueTransformer"/> class.
    /// </summary>
    /// <param name="odataOptions">The OData options containing route components and EDM models.</param>
    public RestierRouteValueTransformer(IOptions<ODataOptions> odataOptions)
    {
        _odataOptions = odataOptions ?? throw new ArgumentNullException(nameof(odataOptions));
    }

    /// <inheritdoc/>
    public override ValueTask<RouteValueDictionary> TransformAsync(
        HttpContext httpContext, RouteValueDictionary values)
    {
        if (httpContext is null)
        {
            return new ValueTask<RouteValueDictionary>((RouteValueDictionary)null);
        }

        var odataPath = values["odataPath"] as string ?? string.Empty;

        // The route prefix is passed via DynamicRouteValueTransformer.State,
        // set by MapRestier() when registering the dynamic route.
        var routePrefix = State as string ?? string.Empty;

        // Look up the EDM model for this route prefix.
        if (!TryGetModel(routePrefix, out var model))
        {
            return new ValueTask<RouteValueDictionary>((RouteValueDictionary)null);
        }

        // Parse the OData path using ODataUriParser.
        ODataPath parsedPath;
        try
        {
            var parser = new ODataUriParser(model, new Uri(odataPath, UriKind.Relative));
            parser.Resolver = new UnqualifiedODataUriResolver { EnableCaseInsensitive = true };
            parsedPath = parser.ParsePath();
        }
        catch (ODataException)
        {
            // Not a valid OData path - fall through to other endpoints (404).
            return new ValueTask<RouteValueDictionary>((RouteValueDictionary)null);
        }

        // Populate ODataFeature on the HttpContext.
        var feature = httpContext.ODataFeature();
        feature.Path = parsedPath;
        feature.Model = model;
        feature.RoutePrefix = routePrefix;
        feature.BaseAddress = BuildBaseAddress(httpContext.Request, routePrefix);

        // Determine the controller action based on HTTP method and path.
        var actionName = DetermineActionName(httpContext.Request.Method, parsedPath);
        if (actionName is null)
        {
            return new ValueTask<RouteValueDictionary>((RouteValueDictionary)null);
        }

        var result = new RouteValueDictionary
        {
            ["controller"] = ControllerName,
            ["action"] = actionName
        };

        return new ValueTask<RouteValueDictionary>(result);
    }

    /// <summary>
    /// Looks up the EDM model for the given route prefix.
    /// </summary>
    private bool TryGetModel(string routePrefix, out IEdmModel model)
    {
        var options = _odataOptions.Value;

        if (options.RouteComponents.TryGetValue(routePrefix, out var components))
        {
            // Verify this is a Restier route (identified by the RestierRouteMarker sentinel).
            var routeServices = options.GetRouteServices(routePrefix);
            if (routeServices.GetService(typeof(RestierRouteMarker)) is not null)
            {
                model = components.EdmModel;
                return true;
            }
        }

        model = null;
        return false;
    }

    /// <summary>
    /// Determines the RestierController action name from the HTTP method and parsed OData path.
    /// </summary>
    internal static string DetermineActionName(string httpMethod, ODataPath path)
    {
        var lastSegment = path.LastOrDefault();

        // $metadata and service document are read-only; reject non-GET requests.
        if (lastSegment is MetadataSegment)
        {
            return string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                ? MethodNameOfGetMetadata
                : null;
        }

        if (path.Count == 0)
        {
            return string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                ? MethodNameOfGetServiceDocument
                : null;
        }

        var isAction = IsAction(lastSegment);

        if (string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase) && !isAction)
        {
            return MethodNameOfGet;
        }

        if (string.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return isAction ? MethodNameOfPostAction : MethodNameOfPost;
        }

        if (string.Equals(httpMethod, "PUT", StringComparison.OrdinalIgnoreCase))
        {
            return MethodNameOfPut;
        }

        if (string.Equals(httpMethod, "PATCH", StringComparison.OrdinalIgnoreCase))
        {
            return MethodNameOfPatch;
        }

        if (string.Equals(httpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
        {
            return MethodNameOfDelete;
        }

        return null;
    }

    /// <summary>
    /// Determines whether the given path segment represents an OData action.
    /// </summary>
    private static bool IsAction(ODataPathSegment lastSegment)
    {
        if (lastSegment is OperationSegment operationSeg)
        {
            if (operationSeg.Operations.FirstOrDefault() is IEdmAction)
            {
                return true;
            }
        }

        if (lastSegment is OperationImportSegment operationImportSeg)
        {
            if (operationImportSeg.OperationImports.FirstOrDefault() is IEdmActionImport)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds the OData base address from the request and route prefix.
    /// </summary>
    private static string BuildBaseAddress(HttpRequest request, string routePrefix)
    {
        var baseUri = $"{request.Scheme}://{request.Host}";
        if (request.PathBase.HasValue)
        {
            var pathBase = request.PathBase.Value.TrimStart('/');
            if (pathBase.Length > 0)
            {
                baseUri += "/" + pathBase;
            }
        }
        if (!string.IsNullOrEmpty(routePrefix))
        {
            baseUri += "/" + routePrefix;
        }
        return baseUri + "/";
    }
}
