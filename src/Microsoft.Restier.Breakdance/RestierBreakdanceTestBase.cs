using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using Flurl;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        public Action<ODataOptions> AddRestierAction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Action<IApplicationBuilder> ApplicationBuilderAction { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="RestierBreakdanceTestBase{TApi}"/>.
        /// </summary>
        /// <remarks>
        /// To properly configure these tests, please set your <see cref="AddRestierAction"/>  action before
        /// calling <see cref="AspNetCoreBreakdanceTestBase.AssemblySetup"/> or <see cref="AspNetCoreBreakdanceTestBase.TestSetup"/>.
        /// </remarks>
        public RestierBreakdanceTestBase()
        {
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(options =>
                        {
                            options.Events.OnRedirectToAccessDenied = ctx => {
                                ctx.Response.StatusCode = 403;
                                return Task.CompletedTask;
                            };
                        });
                services
                    .AddControllers()
                    .AddRestier(options =>
                    {
                        options.Select().Expand().Filter().OrderBy().SetMaxTop(null).Count();
                        options.TimeZone = TimeZoneInfo.Utc;
                        AddRestierAction?.Invoke(options);
                    })
                    .AddApplicationPart(typeof(TApi).Assembly)
                    .AddApplicationPart(typeof(RestierController).Assembly);
            });
        }

        // Workaround for Breakdance 8.0 bug: AspNetCoreBreakdanceTestBase.TestSetupAsync() calls
        // base.TestSetup() instead of base.TestSetupAsync(), and BreakdanceTestBase.TestSetup()
        // calls TestSetupAsync() — creating infinite mutual recursion that stack overflows.
        // Remove these overrides once Breakdance ships a fix.

        /// <inheritdoc/>
        public override void TestSetup()
        {
            EnsureTestServerAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override async Task TestSetupAsync()
        {
            await EnsureTestServerAsync();
        }

        private new async Task EnsureTestServerAsync()
        {
            if (TestServer is not null)
            {
                return;
            }

            TestHostBuilder.ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.Configure(builder =>
                {
                    ApplicationBuilderAction?.Invoke(builder);
                    builder.UseDeveloperExceptionPage();
                    builder.UseODataRouteDebug();
                    builder.UseRouting();
                    builder.UseAuthorization();
                    builder.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                        endpoints.MapRestier();
                    });
                });
            });

            var host = TestHostBuilder.Build();
            await host.StartAsync();
            TestServer = host.GetTestServer();
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
        /// <returns>A scoped <see cref="IServiceProvider"/> containing all of the services available to the specified route.</returns>
        public IServiceProvider GetScopedRequestContainer(string routeName = WebApiConstants.RouteName)
        {
            var context = new DefaultHttpContext
            {
                RequestServices = TestServer.Services
            };

            //if (useEndpointRouting)
            //{
            //    routeName = Restier_IEndpointRouteBuilderExtensions.GetCleanRouteName(routeName);
            //}
            
            context.ODataFeature().RoutePrefix = routeName;
            return context.Request.GetRouteServices();
        }

        /// <summary>
        /// Retrieves an <typeparamref name="TApi"/> instance from the scoped <see cref="IServiceProvider"/> for the specified route.
        /// </summary>
        /// <param name="routeName">
        /// The name of the registered route to retrieve the <typeparamref name="TApi"/> for.
        /// </param>
        /// <param name="useEndpointRouting">Specifies whether or not to use Endpoint Routing. Defaults to false for backwards compatibility, but will change in Restier 2.0.</param>
        /// <returns>An <typeparamref name="TApi"/> instance from the scoped <see cref="IServiceProvider"/> for the specified route.</returns>
        public TApi GetApiInstance(string routeName = WebApiConstants.RouteName, bool useEndpointRouting = false) => GetScopedRequestContainer(routeName).GetService<TApi>();

        /// <summary>
        /// Retrieves the <see cref="IEdmModel"/> instance from <typeparamref name="TApi"/> for the specified route.
        /// </summary>
        /// <param name="routeName">
        /// The name of the registered route to retrieve the <see cref="IEdmModel"/> for.
        /// </param>
        /// <param name="useEndpointRouting">Specifies whether or not to use Endpoint Routing. Defaults to false for backwards compatibility, but will change in Restier 2.0.</param>
        /// <returns>The <see cref="IEdmModel"/> instance from <typeparamref name="TApi"/> for the specified route.</returns>
        public IEdmModel GetModel(string routeName = WebApiConstants.RouteName, bool useEndpointRouting = false) => GetApiInstance(routeName, useEndpointRouting).Model;
    }
}
