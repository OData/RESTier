// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;

namespace System.Web.OData.Domain.Routing
{
    public class DefaultODataRoutingConvention : IODataRoutingConvention
    {
        private string controllerName;

        public DefaultODataRoutingConvention(string controllerName)
        {
            if (controllerName.EndsWith("Controller"))
            {
                controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);
            }

            this.controllerName = controllerName;
        }

        /// <summary>
        /// Selects OData controller based on parsed OData URI
        /// </summary>
        /// <param name="odataPath">Parsed OData URI</param>
        /// <param name="request">Incoming HttpRequest</param>
        /// <returns>Prefix for controller name</returns>
        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            if (IsMetadataPath(odataPath))
            {
                return null;
            }

            return this.controllerName;
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
            // TODO: implement action selection for $ref, navigation scenarios, etc.

            Ensure.NotNull(odataPath, "odataPath");
            Ensure.NotNull(controllerContext, "controllerContext");
            Ensure.NotNull(actionMap, "actionMap");

            HttpMethod method = controllerContext.Request.Method;

            if (method == HttpMethod.Get && !IsMetadataPath(odataPath))
            {
                return "Get";
            }

            ODataPathSegment lastSegment = odataPath.Segments.LastOrDefault();
            if (lastSegment != null && lastSegment.SegmentKind == ODataSegmentKinds.UnboundAction)
            {
                return "PostAction";
            }

            // Let WebAPI select default action
            return null;
        }

        private static bool IsMetadataPath(ODataPath odataPath)
        {
            return odataPath.PathTemplate == "~" ||
                odataPath.PathTemplate == "~/$metadata";
        }
    }
}
