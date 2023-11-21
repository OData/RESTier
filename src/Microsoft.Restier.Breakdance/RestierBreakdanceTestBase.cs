#if NET6_0_OR_GREATER

using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.Restier.Breakdance
{

    /// <summary>
    /// Simplifies testing Restier services by providing the necessary infrastructure to ensure correct test setup &amp; teardown.
    /// </summary>
    public class RestierBreakdanceTestBase<TApi> : AspNetCoreBreakdanceTestBase
        where TApi : ApiBase
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
        /// 
        /// </summary>
        public Action<IApplicationBuilder> ApplicationBuilderAction { get; set; }

        /// <summary>
        /// Helps people that decide to use RestierTestHelpers specify which 
        /// </summary>
        public bool UseEndpointRouting { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="RestierBreakdanceTestBase{TApi}"/>.
        /// </summary>
        /// <param name="useEndpointRouting">Whether to use endpoint routing or not.</param>
        /// <remarks>
        /// To properly configure these tests, please set your <see cref="AddRestierAction"/> and <see cref="MapRestierAction"/> actions before
        /// calling <see cref="AspNetCoreBreakdanceTestBase.AssemblySetup"/> or <see cref="AspNetCoreBreakdanceTestBase.TestSetup"/>.
        /// </remarks>
        public RestierBreakdanceTestBase(bool useEndpointRouting = false)
        {
            UseEndpointRouting = useEndpointRouting;
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(options =>
                        {
                            options.Events.OnRedirectToAccessDenied = context => {
                                context.Response.StatusCode = 403;
                                return Task.CompletedTask;
                            };
                        });

                services
                    .AddRestier(apiBuilder =>
                    {
                        AddRestierAction?.Invoke(apiBuilder);
                    },
                    useEndpointRouting)

                    .AddApplicationPart(typeof(TApi).Assembly)
                    .AddApplicationPart(typeof(RestierController).Assembly);
            })

           .Configure(builder =>
            {
                ApplicationBuilderAction?.Invoke(builder);


                if (useEndpointRouting)
                {
                    builder.UseRestierBatching();

                    builder.UseRouting();
                    builder.UseAuthorization();

                    builder.UseDeveloperExceptionPage();
                    builder.UseEndpoints(endpoints =>
                    {
                        endpoints
                            .Select().Expand().Filter().OrderBy().MaxTop(null).Count().SetTimeZoneInfo(TimeZoneInfo.Utc)               
                            .MapRestier(restierRouteBuilder =>
                            {
                                MapRestierAction?.Invoke(restierRouteBuilder);
                            });
                    });
                }
                else
                {
                    builder.UseAuthorization();
                    builder.UseDeveloperExceptionPage();

                    builder.UseRestierBatching();
                    builder.UseMvc(routeBuilder =>
                    {
                        routeBuilder
                            .Select().Expand().Filter().OrderBy().MaxTop(null).Count().SetTimeZoneInfo(TimeZoneInfo.Utc)
                            .MapRestier(restierRouteBuilder =>
                            {
                                MapRestierAction?.Invoke(restierRouteBuilder);
                            })
                            .MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
                    });
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="host"></param>
        /// <param name="routePrefix"></param>
        /// <param name="resource"></param>
        /// <param name="acceptHeader"></param>
        /// <param name="payload"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> ExecuteTestRequest(HttpMethod httpMethod, string host = WebApiConstants.Localhost,
            string routePrefix = WebApiConstants.RoutePrefix, string resource = null, string acceptHeader = ODataConstants.MinimalAcceptHeader,
            object payload = null, JsonSerializerOptions jsonSerializerOptions = null)
        {
            var client = GetHttpClient(routePrefix);
            using var message = HttpClientHelpers.GetTestableHttpRequestMessage(httpMethod, host, routePrefix, resource, acceptHeader, payload, jsonSerializerOptions);
            //var metadata = await GetApiMetadataAsync().ConfigureAwait(false);
            return await client.SendAsync(message).ConfigureAwait(false);
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
            var response = await client.GetAsync("$metadata").ConfigureAwait(false);
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
        /// <param name="useEndpointRouting">Specifies whether or not to use Endpoint Routing. Defaults to false for backwards compatibility, but will change in Restier 2.0.</param>
        /// <returns>A scoped <see cref="IServiceProvider"/> containing all of the services available to the specified route.</returns>
        public IServiceProvider GetScopedRequestContainer(string routeName = WebApiConstants.RouteName, bool useEndpointRouting = false)
        {
            var context = new DefaultHttpContext
            {
                RequestServices = TestServer.Services
            };

            if (useEndpointRouting)
            {
                routeName = Restier_IEndpointRouteBuilderExtensions.GetCleanRouteName(routeName);
            }
            
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
        /// <param name="useEndpointRouting">Specifies whether or not to use Endpoint Routing. Defaults to false for backwards compatibility, but will change in Restier 2.0.</param>
        /// <returns>An <typeparamref name="TApi"/> instance from the scoped <see cref="IServiceProvider"/> for the specified route.</returns>
        public TApi GetApiInstance(string routeName = WebApiConstants.RouteName, bool useEndpointRouting = false) => GetScopedRequestContainer(routeName, useEndpointRouting).GetService<TApi>();

        /// <summary>
        /// Retrieves the <see cref="IEdmModel"/> instance from <typeparamref name="TApi"/> for the specified route.
        /// </summary>
        /// <param name="routeName">
        /// The name of the registered route to retrieve the <see cref="IEdmModel"/> for.
        /// </param>
        /// <param name="useEndpointRouting">Specifies whether or not to use Endpoint Routing. Defaults to false for backwards compatibility, but will change in Restier 2.0.</param>
        /// <returns>The <see cref="IEdmModel"/> instance from <typeparamref name="TApi"/> for the specified route.</returns>
        public IEdmModel GetModel(string routeName = WebApiConstants.RouteName, bool useEndpointRouting = false) => GetApiInstance(routeName, useEndpointRouting).GetModel();

    }
}

#endif