// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Restier.Core
{

    /// <summary>
    /// A fluent configuration helper that maps <see cref="ApiBase"/> instances to ASP.NET OData routes.
    /// </summary>
    public class RestierRouteBuilder
    {

        #region Internal Properties

        /// <summary>
        /// 
        /// </summary>
        internal Dictionary<string, RestierRouteEntry> Routes { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public RestierRouteBuilder()
        {
            Routes = new();
        }

        #endregion

        /// <summary>
        /// Maps the specified Restier API to an ASP.NET OData Route.
        /// </summary>
        /// <typeparam name="TApi"></typeparam>
        /// <param name="routeName">The name of the Route. Used to map the Route to a specific OData per-route container. Defaults to 'RestierDefault'.</param>
        /// <param name="routePrefix">A string </param>
        /// <param name="allowBatching">A boolean specifying if the <see cref="RestierBatchHandler" /> will be mapped to the '$batch' route.</param>
        /// <returns>The <see cref="RestierRouteBuilder"/> instance to allow for fluent method chaining.</returns>
        public RestierRouteBuilder MapApiRoute<TApi>(string routeName, string routePrefix, bool allowBatching = true) where TApi : ApiBase
        {
            if (string.IsNullOrWhiteSpace(routeName))
            {
                Trace.TraceWarning("Restier: You mapped an ApiRoute with a blank RouteName. Registering the route as 'RestierDefault' for now, if this doesn't work for you then please change the name.");
                routeName = "RestierDefault";
            }

            Routes.Add(routeName, new RestierRouteEntry(routeName, routePrefix, typeof(TApi), allowBatching));
            return this;
        }

    }

}
