// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using ODataPath = Microsoft.AspNet.OData.Routing.ODataPath;

namespace Microsoft.Restier.AspNet
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
        /// Selects OData controller based on parsed OData URI
        /// </summary>
        /// <param name="odataPath">Parsed OData URI</param>
        /// <param name="request">Incoming HttpRequest</param>
        /// <returns>Prefix for controller name</returns>
        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            Ensure.NotNull(odataPath, nameof(odataPath));
            Ensure.NotNull(request, nameof(request));

            if (IsMetadataPath(odataPath))
            {
                return null;
            }

            // If user has defined something like PeopleController for the entity set People,
            // Then whether there is an action in that controller is checked
            // If controller has action for request, will be routed to that controller.
            // Cannot mark EntitySetRoutingConversion has higher priority as there will no way
            // to route to RESTier controller if there is EntitySet controller but no related action.
            if (HasControllerForEntitySetOrSingleton(odataPath, request))
            {
                // Fall back to routing conventions defined by OData Web API.
                return null;
            }

            return RestierControllerName;
        }

        /// <summary>
        /// Selects the appropriate action based on the parsed OData URI.
        /// </summary>
        /// <param name="odataPath">Parsed OData URI</param>
        /// <param name="controllerContext">Context for HttpController</param>
        /// <param name="actionMap">Mapping from action names to HttpActions</param>
        /// <returns>String corresponding to controller action name</returns>
        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            // TODO GitHubIssue#44 : implement action selection for $ref, navigation scenarios, etc.
            Ensure.NotNull(odataPath, nameof(odataPath));
            Ensure.NotNull(controllerContext, nameof(controllerContext));
            Ensure.NotNull(actionMap, nameof(actionMap));

            if (!(controllerContext.Controller is RestierController))
            {
                // RESTier cannot select action on controller which is not RestierController.
                return null;
            }

            var method = controllerContext.Request.Method;
            var lastSegment = odataPath.Segments.LastOrDefault();
            var isAction = IsAction(lastSegment);

            if (method == HttpMethod.Get && !IsMetadataPath(odataPath) && !isAction)
            {
                return MethodNameOfGet;
            }

            if (method == HttpMethod.Post && isAction)
            {
                return MethodNameOfPostAction;
            }

            if (method == HttpMethod.Post)
            {
                return MethodNameOfPost;
            }

            if (method == HttpMethod.Delete)
            {
                return MethodNameOfDelete;
            }

            if (method == HttpMethod.Put)
            {
                return MethodNameOfPut;
            }

            if (method == new HttpMethod("PATCH"))
            {
                return MethodNameOfPatch;
            }

            return null;
        }

        private static bool IsMetadataPath(ODataPath odataPath)
        {
            return odataPath.PathTemplate == "~" ||  odataPath.PathTemplate == "~/$metadata";
        }

        private static bool HasControllerForEntitySetOrSingleton(ODataPath odataPath, HttpRequestMessage request)
        {
            string controllerName = null;

            var firstSegment = odataPath.Segments.FirstOrDefault();
            if (firstSegment != null)
            {
                if (firstSegment is EntitySetSegment entitySetSegment)
                {
                    controllerName = entitySetSegment.EntitySet.Name;
                }
                else
                {
                    if (firstSegment is SingletonSegment singletonSegment)
                    {
                        controllerName = singletonSegment.Singleton.Name;
                    }
                }
            }

            if (controllerName != null)
            {
                var services = request.GetConfiguration().Services;

                var controllers = services.GetHttpControllerSelector().GetControllerMapping();
                if (controllers.TryGetValue(controllerName, out var descriptor) && descriptor != null)
                {
                    // If there is a controller, check whether there is an action
                    if (HasSelectableAction(request, descriptor))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasSelectableAction(HttpRequestMessage request, HttpControllerDescriptor descriptor)
        {
            var configuration = request.GetConfiguration();
            var actionSelector = configuration.Services.GetActionSelector();

            // Empty route as this is must and route data is not used by OData routing conversion
            var route = new HttpRoute();
            var routeData = new HttpRouteData(route);

            var context = new HttpControllerContext(configuration, routeData, request)
            {
                ControllerDescriptor = descriptor
            };

            try
            {
                var action = actionSelector.SelectAction(context);
                if (action != null)
                {
                    return true;
                }
            }
            catch (HttpResponseException)
            {
                // ignored
            }

            return false;
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
