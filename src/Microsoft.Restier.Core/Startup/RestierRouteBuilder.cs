using System.Collections.Generic;

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
            Routes.Add(routeName, new RestierRouteEntry(routeName, routePrefix, typeof(TApi), allowBatching));
            return this;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public RestierApiRouteDictionary ToRestierApiRouteDictionary(IServiceProvider sp)
        //{
        //    Ensure.NotNull(sp, nameof(sp));
        //    var scope = sp.GetService<IServiceScopeFactory>().CreateScope();

        //    var routes = new RestierApiRouteDictionary();
        //    foreach (var route in Routes)
        //    {
        //        var api = scope.ServiceProvider.GetService(route.ApiType) as ApiBase;
        //        if (api == null)
        //        {
        //            throw new Exception($"Could not find the API. Please make sure you registered the API using the new 'UseRestier((services) => services.AddRestierApi<{route.ApiType.Name}>());' syntax.");
        //        }

        //        var builder = sp.GetServices<IModelBuilder>();
        //        if (sp.GetService(typeof(IModelBuilder)) is not IModelBuilder modelBuilder)
        //        {
        //            throw new InvalidOperationException(Resources.ModelBuilderNotRegistered);
        //        }

        //        var buildContext = new ModelContext(api);
        //        var model = modelBuilder.GetModel(buildContext);

        //        routes.Add(route.RouteName, new RestierApiModelMap(route.ApiType, model));
        //    }
        //    return routes;
        //}







    }

}
