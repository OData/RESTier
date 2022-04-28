// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using ODataPath = Microsoft.AspNet.OData.Routing.ODataPath;

namespace Microsoft.Restier.AspNetCore
{
    /// <summary>
    /// The default routing convention implementation.
    /// </summary>
    internal class RestierRoutingConvention : IODataRoutingConvention
    {
        private const string RestierControllerName = "Restier";
        private const string MethodNameOfGet = "Get";
        private const string MethodNameOfPost = "Post";
        private const string MethodNameOfPut = "Put";
        private const string MethodNameOfPatch = "Patch";
        private const string MethodNameOfDelete = "Delete";
        private const string MethodNameOfPostAction = "PostAction";

        /// <summary>
        /// Selects the appropriate action based on the parsed OData URI.
        /// </summary>
        /// <param name="routeContext">The route context.</param>
        /// <returns>An enumerable of ControllerActionDescriptors.</returns>
        public IEnumerable<ControllerActionDescriptor> SelectAction(RouteContext routeContext)
        {
            Ensure.NotNull(routeContext, nameof(routeContext));

            ODataPath odataPath = routeContext.HttpContext.ODataFeature().Path;

            if (odataPath is null)
            {
                throw new InvalidOperationException(Resources.InvalidEmptyPathInRequest);
            }

            var services = routeContext.HttpContext.RequestServices;

            var actionCollectionProvider = services.GetRequiredService<IActionDescriptorCollectionProvider>();

            IEnumerable<ControllerActionDescriptor> actions;
            if (TryFindMatchingODataActions(routeContext, out actions))
            {
                return actions;
            }

            var restierControllerActionDescriptors = actionCollectionProvider
                .ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
                .Where(c => string.Equals(c.ControllerName, RestierControllerName, StringComparison.OrdinalIgnoreCase));

            if (!restierControllerActionDescriptors.Any())
            {
                // RESTier cannot select action on controller which is not RestierController.
                return null;
            }

            var method = routeContext.HttpContext.Request.Method;
            var lastSegment = odataPath.Segments.LastOrDefault();
            var isAction = IsAction(lastSegment);

            if (string.Equals(method, HttpMethod.Get.Method, StringComparison.OrdinalIgnoreCase) && !IsMetadataPath(odataPath) && !isAction)
            {
                return restierControllerActionDescriptors.Where(x => string.Equals(MethodNameOfGet, x.ActionName, StringComparison.OrdinalIgnoreCase));
            }

            if (string.Equals(method, HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase))
            {
                if (isAction)
                {
                    return restierControllerActionDescriptors.Where(x => string.Equals(MethodNameOfPostAction, x.ActionName, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    return restierControllerActionDescriptors.Where(x => string.Equals(MethodNameOfPost, x.ActionName, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (string.Equals(method, HttpMethod.Delete.Method, StringComparison.OrdinalIgnoreCase))
            {
                return restierControllerActionDescriptors.Where(x => string.Equals(MethodNameOfDelete, x.ActionName, StringComparison.OrdinalIgnoreCase));
            }

            if (string.Equals(method, HttpMethod.Put.Method, StringComparison.OrdinalIgnoreCase))
            {
                return restierControllerActionDescriptors.Where(x => string.Equals(MethodNameOfPut, x.ActionName, StringComparison.OrdinalIgnoreCase));
            }

            if (string.Equals(method, HttpMethod.Patch.Method, StringComparison.OrdinalIgnoreCase))
            {
                return restierControllerActionDescriptors.Where(x => string.Equals(MethodNameOfPatch, x.ActionName, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private bool TryFindMatchingODataActions(RouteContext context, out IEnumerable<ControllerActionDescriptor> actions)
        {
            IEnumerable<IODataRoutingConvention> routingConventions = context.HttpContext.Request.GetRoutingConventions();
            if (routingConventions != null)
            {
                foreach (IODataRoutingConvention convention in routingConventions)
                {
                    if (convention != this)
                    {
                        IEnumerable<ControllerActionDescriptor> actionDescriptor = convention.SelectAction(context);
                        if (actionDescriptor != null && actionDescriptor.Any())
                        {
                            actions = actionDescriptor;
                            return true;
                        }
                    }
                }
            }

            actions = null;
            return false;
        }

        private static bool IsMetadataPath(ODataPath odataPath)
        {
            return odataPath.PathTemplate == "~" || odataPath.PathTemplate == "~/$metadata";
        }

        private static bool IsAction(ODataPathSegment lastSegment)
        {
            if (lastSegment is OperationSegment operationSeg)
            {
                if (operationSeg.Operations.FirstOrDefault() is IEdmAction action)
                {
                    return true;
                }
            }

            if (lastSegment is OperationImportSegment operationImportSeg)
            {
                if (operationImportSeg.OperationImports.FirstOrDefault() is IEdmActionImport actionImport)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
