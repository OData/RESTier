using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Core.Routing
{

    /// <summary>
    /// 
    /// </summary>
    public class RestierRouteBuilder
    {

        /// <summary>
        /// 
        /// </summary>
        internal List<RestierRouteEntry> Routes { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public RestierRouteBuilder()
        {
            Routes = new List<RestierRouteEntry>();
        }

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
            Routes.Add(new RestierRouteEntry(routeName, routePrefix, typeof(TApi), allowBatching));
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public RestierApiRouteDictionary ToRestierApiRouteDictionary(IServiceProvider sp)
        {
            Ensure.NotNull(sp, nameof(sp));
            var scope = sp.GetService<IServiceScopeFactory>().CreateScope();

            var routes = new RestierApiRouteDictionary();
            foreach (var route in Routes)
            {
                var api = scope.ServiceProvider.GetService(route.ApiType) as ApiBase;
                if (api == null)
                {
                    throw new Exception($"Could not find the API. Please make sure you registered the API using the new 'UseRestier((services) => services.AddRestierApi<{route.ApiType.Name}>());' syntax.");
                }

                var builder = sp.GetServices<IModelBuilder>();
                var modelBuilder = sp.GetService(typeof(IModelBuilder)) as IModelBuilder;
                if (modelBuilder == null)
                {
                    throw new InvalidOperationException(Resources.ModelBuilderNotRegistered);
                }

                var buildContext = new ModelContext(api);
                var model = modelBuilder.GetModel(buildContext);

                routes.Add(route.RouteName, new RestierApiModelMap(route.ApiType, model));
            }
            return routes;
        }

    }

}
