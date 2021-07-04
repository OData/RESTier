﻿#if NET5_0_OR_GREATER

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Breakdance
{

    /// <summary>
    /// Simplifies testing Restier services by providing the necessary infrastructure to ensure correct test setup &amp; teardown.
    /// </summary>
    public class RestierBreakdanceTestBase<TApi, TDbContext> : AspNetCoreBreakdanceTestBase
        where TApi : ApiBase
        where TDbContext : DbContext
    {

        /// <summary>
        /// 
        /// </summary>
        public Action<RestierApiBuilder> AddRestierAction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Action<RestierRouteBuilder> MapRestierAction { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="RestierBreakdanceTestBase{TApi, TDbContext}"/>.
        /// </summary>
        /// <remarks>
        /// To properly configure these tests, please set your <see cref="AddRestierAction"/> and <see cref="MapRestierAction"/> actions before
        /// calling <see cref="AspNetCoreBreakdanceTestBase.AssemblySetup"/> or <see cref="AspNetCoreBreakdanceTestBase.TestSetup"/>.
        /// </remarks>
        public RestierBreakdanceTestBase()
        {
            TestHostBuilder.ConfigureServices(services =>
            {
                services
                    .AddRestier(apiBuilder =>
                    {
                        AddRestierAction?.Invoke(apiBuilder);
                    })
                    .AddApplicationPart(typeof(RestierController).Assembly);
            })
           .Configure(builder =>
            {
                builder.UseMvc(routeBuilder =>
                {
                    routeBuilder
                        .Select().Expand().Filter().OrderBy().MaxTop(null).Count().SetTimeZoneInfo(TimeZoneInfo.Utc)
                        .MapRestier(restierRouteBuilder =>
                        {
                            MapRestierAction?.Invoke(restierRouteBuilder);
                        });
                });
            });
        }

        /// <summary>
        /// Retrieves an <see cref="HttpClient"/> instance from the <see cref="TestServer"/> and properly configures the <see cref="HttpClient.BaseAddress"/>.
        /// </summary>
        /// <param name="routePrefix">
        /// The string to append to the <see cref="HttpClient.BaseAddress"/> for all requests. Defaults to <see cref="WebApiConstants.RoutePrefix"/>.
        /// </param>
        /// <returns>A properly configured <see cref="HttpClient"/>instance from the <see cref="TestServer"/>.</returns>
        public HttpClient GetHttpClient(string routePrefix = WebApiConstants.RoutePrefix)
        {
            var client = TestServer.CreateClient();
            client.BaseAddress = new Uri(WebApiConstants.Localhost + routePrefix);
            return client;
        }

        /// <summary>
        /// Retrieves an <see cref="XDocument"/> containing the full result from calling "/$metadata" on the <typeparamref name="TApi"/>.
        /// </summary>
        /// <param name="routePrefix">
        /// The string to append to the <see cref="HttpClient.BaseAddress"/> for all requests. Defaults to <see cref="WebApiConstants.RoutePrefix"/>.
        /// </param>
        /// <returns>An <see cref="XDocument"/> containing the full result from calling "/$metadata" on the <typeparamref name="TApi"/>.</returns>
        public async Task<XDocument> GetApiMetadataAsync(string routePrefix = WebApiConstants.RoutePrefix)
        {
            var client = GetHttpClient(routePrefix);
            var response = await client.GetAsync(new Uri($"{WebApiConstants.Localhost}{routePrefix}/$metadata")).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                Trace.WriteLine(responseContent);
                return null;
            }
            return XDocument.Parse(responseContent);
        }

        /// <summary>
        /// Retrieves a scoped <see cref="IServiceProvider"/> containing all of the services available to the specified route.
        /// </summary>
        /// <param name="routeName">
        /// The name of the registered route to retrieve the <see cref="IServiceProvider"/> for. Defaults to <see cref="WebApiConstants.RouteName"/>.
        /// </param>
        /// <returns>A scoped <see cref="IServiceProvider"/> containing all of the services available to the specified route.</returns>
        public IServiceProvider GetScopedRequestContainer(string routeName = WebApiConstants.RouteName)
        {
            var context = new DefaultHttpContext
            {
                RequestServices = TestServer.Services
            };
            context.ODataFeature().RouteName = routeName;
            context.Request.CreateRequestContainer(routeName);

            return context.Request.ODataFeature().RequestScope.ServiceProvider;
        }

        /// <summary>
        /// Retrieves an <typeparamref name="TApi"/> instance from the scoped <see cref="IServiceProvider"/> for the specified route.
        /// </summary>
        /// <param name="routeName">
        /// The name of the registered route to retrieve the <typeparamref name="TApi"/> for.
        /// </param>
        /// <returns>An <typeparamref name="TApi"/> instance from the scoped <see cref="IServiceProvider"/> for the specified route.</returns>
        public TApi GetApiInstance(string routeName = WebApiConstants.RouteName) => GetScopedRequestContainer(routeName).GetService<TApi>();

        /// <summary>
        /// Retrieves the <see cref="IEdmModel"/> instance from <typeparamref name="TApi"/> for the specified route.
        /// </summary>
        /// <param name="routeName">
        /// The name of the registered route to retrieve the <see cref="IEdmModel"/> for.
        /// </param>
        /// <returns>The <see cref="IEdmModel"/> instance from <typeparamref name="TApi"/> for the specified route.</returns>
        public IEdmModel GetModel(string routeName = WebApiConstants.RouteName) => GetApiInstance(routeName).GetModel();

    }
}

#endif