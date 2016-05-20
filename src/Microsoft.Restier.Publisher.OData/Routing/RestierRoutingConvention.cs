// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Publisher.OData.Routing
{
    /// <summary>
    /// The default routing convention implementation.
    /// </summary>
    internal class RestierRoutingConvention : IODataRoutingConvention
    {
        private const string RestierControllerName = "Restier";
        private const string MethodNameOfGet = "Get";
        private const string MethodNameOfPostAction = "PostAction";

        private readonly Func<ApiBase> apiFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierRoutingConvention" /> class.
        /// </summary>
        /// <param name="apiFactory">The API factory method.</param>
        public RestierRoutingConvention(Func<ApiBase> apiFactory)
        {
            this.apiFactory = apiFactory;
        }

        /// <summary>
        /// Selects OData controller based on parsed OData URI
        /// </summary>
        /// <param name="odataPath">Parsed OData URI</param>
        /// <param name="request">Incoming HttpRequest</param>
        /// <returns>Prefix for controller name</returns>
        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            Ensure.NotNull(odataPath, "odataPath");
            Ensure.NotNull(request, "request");

            if (IsMetadataPath(odataPath))
            {
                return null;
            }

            // If user has defined something like PeopleController for the entity set People,
            // we should let the request being routed to that controller.
            if (HasControllerForEntitySetOrSingleton(odataPath, request))
            {
                // Fallback to routing conventions defined by OData Web API.
                return null;
            }

            // Create ApiBase instance
            request.SetApiInstance(apiFactory.Invoke());
            return RestierControllerName;
        }

        /// <summary>
        /// Selects the appropriate action based on the parsed OData URI.
        /// </summary>
        /// <param name="odataPath">Parsed OData URI</param>
        /// <param name="controllerContext">Context for HttpController</param>
        /// <param name="actionMap">Mapping from action names to HttpActions</param>
        /// <returns>String corresponding to controller action name</returns>
        public string SelectAction(
            ODataPath odataPath,
            HttpControllerContext controllerContext,
            ILookup<string, HttpActionDescriptor> actionMap)
        {
            // TODO GitHubIssue#44 : implement action selection for $ref, navigation scenarios, etc.
            Ensure.NotNull(odataPath, "odataPath");
            Ensure.NotNull(controllerContext, "controllerContext");
            Ensure.NotNull(actionMap, "actionMap");

            if (!(controllerContext.Controller is RestierController))
            {
                // RESTier cannot select action on controller which is not RestierController.
                return null;
            }

            HttpMethod method = controllerContext.Request.Method;

            if (method == HttpMethod.Get && !IsMetadataPath(odataPath))
            {
                return MethodNameOfGet;
            }

            ODataPathSegment lastSegment = odataPath.Segments.LastOrDefault();
            if (lastSegment != null && lastSegment.SegmentKind == ODataSegmentKinds.UnboundAction)
            {
                return MethodNameOfPostAction;
            }

            // Let WebAPI select default action
            return null;
        }

        private static bool IsMetadataPath(ODataPath odataPath)
        {
            return odataPath.PathTemplate == "~" ||
                odataPath.PathTemplate == "~/$metadata";
        }

        private static bool HasControllerForEntitySetOrSingleton(
            ODataPath odataPath, HttpRequestMessage request)
        {
            string controllerName = null;

            ODataPathSegment firstSegment = odataPath.Segments.FirstOrDefault();
            if (firstSegment != null)
            {
                var entitySetSegment = firstSegment as EntitySetPathSegment;
                if (entitySetSegment != null)
                {
                    controllerName = entitySetSegment.EntitySetName;
                }
                else
                {
                    var singletonSegment = firstSegment as SingletonPathSegment;
                    if (singletonSegment != null)
                    {
                        controllerName = singletonSegment.SingletonName;
                    }
                }
            }

            if (controllerName != null)
            {
                IDictionary<string, HttpControllerDescriptor> controllers =
                    request.GetConfiguration().Services.GetHttpControllerSelector().GetControllerMapping();
                HttpControllerDescriptor descriptor;
                if (controllers.TryGetValue(controllerName, out descriptor) && descriptor != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
