#if NET6_0_OR_GREATER

using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        /// Registers the <typeparamref name="TApi"/> (and any additional APIs) with Restier. Invoked from inside
        /// <c>services.AddRestier(...)</c> when the <see cref="AspNetCoreBreakdanceTestBase.TestServer"/> is built.
        /// </summary>
        /// <example>
        /// <code>
        /// AddRestierAction = apiBuilder =>
        /// {
        ///     apiBuilder.AddRestierApi&lt;LibraryApi&gt;(services =>
        ///     {
        ///         services.AddEFCoreProviderServices&lt;LibraryContext&gt;((_, options) => options.UseInMemoryDatabase("Library"));
        ///     });
        /// };
        /// </code>
        /// </example>
        /// <remarks>
        /// Set this in your test class constructor, before <see cref="AspNetCoreBreakdanceTestBase.AssemblySetup"/> or
        /// <see cref="AspNetCoreBreakdanceTestBase.TestSetup"/> runs. Every API you expect to route to must be added here;
        /// the base class does not register <typeparamref name="TApi"/> for you.
        /// </remarks>
        public Action<RestierApiBuilder> AddRestierAction { get; set; }

        /// <summary>
        /// Maps the OData routes for the APIs registered in <see cref="AddRestierAction"/>. Invoked from inside
        /// <c>MapRestier(...)</c> while the request pipeline is being built.
        /// </summary>
        /// <example>
        /// <code>
        /// MapRestierAction = routeBuilder =>
        /// {
        ///     routeBuilder.MapApiRoute&lt;LibraryApi&gt;("Library", "library");
        /// };
        /// </code>
        /// </example>
        /// <remarks>
        /// Set this in your test class constructor. The route name and prefix you choose here are the values to pass to
        /// <see cref="GetScopedRequestContainer"/>, <see cref="GetApiInstance"/>, <see cref="GetModel"/>, and
        /// <see cref="ExecuteTestRequest"/>; the defaults on those methods assume <see cref="WebApiConstants.RouteName"/>
        /// and <see cref="WebApiConstants.RoutePrefix"/>.
        /// </remarks>
        public Action<RestierRouteBuilder> MapRestierAction { get; set; }

        /// <summary>
        /// Adds middleware to the <b>front</b> of the request pipeline, before routing, authorization, and the Restier
        /// endpoints are registered.
        /// </summary>
        /// <example>
        /// <code>
        /// ApplicationBuilderAction = app =>
        /// {
        ///     app.UseCors("AllowAll");
        /// };
        /// </code>
        /// </example>
        /// <remarks>
        /// Use this for middleware that must observe every request before it is routed, such as CORS, request logging, or
        /// exception handling. Nothing has been mapped yet when this runs, so middleware that inspects the endpoint table
        /// (anything that calls <c>UseEndpoints</c> or reads <see cref="EndpointDataSource"/>) belongs in
        /// <see cref="ApplicationBuilderLastAction"/> instead.
        /// </remarks>
        public Action<IApplicationBuilder> ApplicationBuilderAction { get; set; }

        /// <summary>
        /// Adds middleware to the <b>end</b> of the request pipeline, after the Restier endpoints have been mapped and
        /// the endpoint table is complete.
        /// </summary>
        /// <example>
        /// <code>
        /// ApplicationBuilderLastAction = app =>
        /// {
        ///     app.UseODataMcp();
        /// };
        /// </code>
        /// </example>
        /// <remarks>
        /// Use this for middleware that discovers the routes Restier mapped, or that needs <c>UseRouting</c> to already be
        /// in the pipeline. Because the <see cref="AspNetCoreBreakdanceTestBase.TestHostBuilder"/> replaces the entire
        /// pipeline on each <c>Configure</c> call, this hook is the supported way to append to the pipeline the base class
        /// builds; calling <c>TestHostBuilder.Configure</c> yourself would discard the Restier configuration.
        /// </remarks>
        public Action<IApplicationBuilder> ApplicationBuilderLastAction { get; set; }

        /// <summary>
        /// Gets a value indicating whether the pipeline was built with ASP.NET Core endpoint routing (<c>true</c>) or the
        /// legacy MVC router (<c>false</c>). Set by the constructor's <c>useEndpointRouting</c> argument.
        /// </summary>
        /// <remarks>
        /// Pass the same value to the <c>useEndpointRouting</c> parameter of <see cref="GetScopedRequestContainer"/>,
        /// <see cref="GetApiInstance"/>, and <see cref="GetModel"/>, because endpoint routing rewrites route names and those
        /// helpers must look the route up under the rewritten name.
        /// </remarks>
        public bool UseEndpointRouting { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="RestierBreakdanceTestBase{TApi}"/> and queues the Restier service
        /// registration and request pipeline on the <see cref="AspNetCoreBreakdanceTestBase.TestHostBuilder"/>.
        /// </summary>
        /// <param name="useEndpointRouting">
        /// <c>true</c> to build the pipeline with ASP.NET Core endpoint routing (<c>UseRouting</c> / <c>UseEndpoints</c>);
        /// <c>false</c> to use the legacy MVC router (<c>UseMvc</c>). Defaults to <c>false</c> for backwards compatibility.
        /// </param>
        /// <example>
        /// <code>
        /// [TestClass]
        /// public class LibraryApiTests : RestierBreakdanceTestBase&lt;LibraryApi&gt;
        /// {
        ///     public LibraryApiTests() : base(useEndpointRouting: true)
        ///     {
        ///         AddRestierAction = apiBuilder => apiBuilder.AddRestierApi&lt;LibraryApi&gt;(services => services.AddEFCoreProviderServices&lt;LibraryContext&gt;());
        ///         MapRestierAction = routeBuilder => routeBuilder.MapApiRoute&lt;LibraryApi&gt;("Library", "library");
        ///         ApplicationBuilderLastAction = app => app.UseODataMcp();
        ///     }
        ///
        ///     [TestInitialize]
        ///     public void Setup() => TestSetup();
        ///
        ///     [TestCleanup]
        ///     public void TearDown() => TestTearDown();
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// <para>
        /// The pipeline is assembled in this order: <see cref="ApplicationBuilderAction"/>, Restier batching, routing,
        /// authorization, the developer exception page, the Restier endpoints from <see cref="MapRestierAction"/>, and finally
        /// <see cref="ApplicationBuilderLastAction"/>. Cookie authentication is registered so that access-denied responses
        /// return 403 instead of redirecting.
        /// </para>
        /// <para>
        /// The hook properties are read when the <see cref="AspNetCoreBreakdanceTestBase.TestServer"/> starts, not when this
        /// constructor runs, so set <see cref="AddRestierAction"/> and <see cref="MapRestierAction"/> (and the optional
        /// <see cref="ApplicationBuilderAction"/> / <see cref="ApplicationBuilderLastAction"/>) in your constructor before
        /// calling <see cref="AspNetCoreBreakdanceTestBase.AssemblySetup"/> or <see cref="AspNetCoreBreakdanceTestBase.TestSetup"/>.
        /// Do not call <c>TestHostBuilder.Configure</c> yourself; it replaces the pipeline built here.
        /// </para>
        /// </remarks>
        public RestierBreakdanceTestBase(bool useEndpointRouting = false)
        {
            UseEndpointRouting = useEndpointRouting;
            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(options =>
                        {
                            options.Events.OnRedirectToAccessDenied = context =>
                            {
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
            });

            TestHostBuilder.Configure(builder =>
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

                ApplicationBuilderLastAction?.Invoke(builder);
            });
        }

        /// <summary>
        /// Sends a single OData request to the <typeparamref name="TApi"/> hosted in the
        /// <see cref="AspNetCoreBreakdanceTestBase.TestServer"/> and returns the raw response.
        /// </summary>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to use for the request.</param>
        /// <param name="host">The scheme and host for the request. Defaults to <see cref="WebApiConstants.Localhost"/>; change it only if that collides with another service on the machine.</param>
        /// <param name="routePrefix">The route prefix mapped in <see cref="MapRestierAction"/>. Defaults to <see cref="WebApiConstants.RoutePrefix"/>.</param>
        /// <param name="resource">The OData resource path relative to the route prefix, for example <c>Books?$top=5</c> or <c>Books(1)</c>. <c>null</c> requests the service root.</param>
        /// <param name="acceptHeader">The <c>Accept</c> header value. Defaults to <see cref="ODataConstants.MinimalAcceptHeader"/>.</param>
        /// <param name="payload">An object to serialize as the JSON request body for POST, PUT, and PATCH requests, or <c>null</c> for no body.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> used to serialize <paramref name="payload"/>, or <c>null</c> for the defaults.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> returned by the test server. The caller owns the response and should dispose it.</returns>
        /// <example>
        /// <code>
        /// var response = await ExecuteTestRequest(HttpMethod.Get, routePrefix: "library", resource: "Books?$top=5");
        /// response.StatusCode.Should().Be(HttpStatusCode.OK);
        /// var json = await response.Content.ReadAsStringAsync();
        /// </code>
        /// </example>
        /// <remarks>
        /// The request is built by <see cref="HttpClientHelpers.GetTestableHttpRequestMessage"/> and sent through
        /// <see cref="AspNetCoreBreakdanceTestBase.GetHttpClient(string)"/>, so the <see cref="HttpClient.BaseAddress"/> already
        /// includes <paramref name="routePrefix"/>. Non-success status codes are returned, not thrown.
        /// </remarks>
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