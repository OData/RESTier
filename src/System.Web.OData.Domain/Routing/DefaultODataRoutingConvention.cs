// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
