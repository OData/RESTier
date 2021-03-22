using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Restier.Core.Startup
{

    /// <summary>
    /// 
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
        /// 
        /// </summary>
        /// <typeparam name="TApi"></typeparam>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <param name="allowBatching"></param>
        /// <returns></returns>
        public RestierRouteBuilder MapApiRoute<TApi>(string routeName, string routePrefix, bool allowBatching = true)
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
