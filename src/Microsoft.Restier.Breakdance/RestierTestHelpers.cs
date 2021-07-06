// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Newtonsoft.Json;
using System.Text;
#if NET5_0_OR_GREATER
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.EntityFrameworkCore;
    using CloudNimble.Breakdance.AspNetCore;
#else
    using System.Web.Http;
    using System.Data.Entity;
    using CloudNimble.Breakdance.WebApi;
#endif


namespace Microsoft.Restier.Breakdance
{

    /// <summary>
    /// A set of methods that make it easier to pull out Restier runtime components for unit testing.
    /// </summary>
    /// <remarks>See RestierTestHelperTests.cs for more examples of how to use these methods.</remarks>
    public static class RestierTestHelpers
    {

        #region Private Members

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1823:Avoid unused private fields", Justification = "<Pending>")]
        private static readonly DefaultQuerySettings QueryDefaults = new()
        {
            EnableCount = true,
            EnableExpand = true,
            EnableFilter = true,
            EnableOrderBy = true,
            EnableSelect = true,
            MaxTop = 10
        };

        #endregion

        #region Public Methods

        #region ExecuteTestRequest

        /// <summary>
        /// Configures the Restier pipeline in-memory and executes a test request against a given service, returning an <see cref="HttpResponseMessage"/> for inspection.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to use for the request.</param>
        /// <param name="host">
        /// The protocol and host to connect to in order to run the tests. Must end with a forward-slash. Defaults to "http://localhost/", and should not normally be changed. NOTE: This should
        /// NOT be the same as any of your actual running environments, and does not require a port assignment in order to function.
        /// </param>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">
        /// The string that will be appended in between the Host and the Resource when constructing a URL. NOTE: DO NOT set this to the same URL as your deployment environments. 
        /// The prefix is irrelevant, is only for internal testing, and should ONLY be changed if you are testing more than one API in a test method (which is not recommended).
        /// </param>
        /// <param name="resource">The specific resource on the endpoint that will be called. Must start with a forward-slash.</param>
        /// <param name="serviceCollection"></param>
        /// <param name="acceptHeader">The "Accept" header that should be added to the request. Defaults to "application/json;odata.metadata=full".</param>
        /// <param name="defaultQuerySettings">A <see cref="DefaultQuerySettings"/> instabce that defines how OData operations should work. Defaults to everything enabled with a <see cref="DefaultQuerySettings.MaxTop"/> of 10.</param>
        /// <param name="timeZoneInfo">A <see cref="TimeZoneInfo"/> instenace specifying what time zone should be used to translate time payloads into. Defaults to <see cref="TimeZoneInfo.Utc"/>.</param>
        /// <param name="payload">When the <paramref name="httpMethod"/> is <see cref="HttpMethod.Post"/> or <see cref="HttpMethod.Put"/>, this object is serialized to JSON and inserted into the <see cref="HttpRequestMessage.Content"/>.</param>
        /// <param name="jsonSerializerSettings">A <see cref="JsonSerializerSettings"/> instance defining how the payload should be serialized into the request body. Defaults to using Zulu time and will include all properties in the payload, even null ones.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> that contains the managed response for the request for inspection.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "<Pending>")]
        public static async Task<HttpResponseMessage> ExecuteTestRequest<TApi, TDbContext>(HttpMethod httpMethod, string host = WebApiConstants.Localhost, string routeName = WebApiConstants.RouteName,
            string routePrefix = WebApiConstants.RoutePrefix, string resource = null, Action<IServiceCollection> serviceCollection = default, string acceptHeader = ODataConstants.MinimalAcceptHeader,
            DefaultQuerySettings defaultQuerySettings = null, TimeZoneInfo timeZoneInfo = null, object payload = null, JsonSerializerSettings jsonSerializerSettings = null)
            where TApi : ApiBase
            where TDbContext : DbContext
        {

#if NET5_0_OR_GREATER
            var server = GetTestableRestierServer<TApi, TDbContext>(routeName, routePrefix, serviceCollection);
            var client = server.CreateClient();
            using var message = new HttpRequestMessage(httpMethod, new Uri($"{host}{routePrefix}{resource}"));    // this way fails
            message.Headers.Add("accept", acceptHeader);
            
            if (payload != null)
            {
                message.Content = new StringContent(JsonConvert.SerializeObject(payload, jsonSerializerSettings), Encoding.UTF8, "application/json");
            }

            return await client.SendAsync(message).ConfigureAwait(false);

#else
            using var config = await GetTestableRestierConfiguration<TApi, TDbContext>(routeName, routePrefix, defaultQuerySettings, timeZoneInfo, serviceCollection).ConfigureAwait(false);
            var client = config.GetTestableHttpClient();
            return await client.ExecuteTestRequest(httpMethod, host, routePrefix, resource, acceptHeader, payload, jsonSerializerSettings).ConfigureAwait(false);
#endif
        }

        #endregion

        #region GetModelBuilderHierarchy

        /// <summary>
        /// Gets a list of fully-qualified builder instances that are registered down the ModelBuilder chain. The order is really important, so this is a great way to troubleshoot.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
        public static async Task<List<string>> GetModelBuilderHierarchy<TApi, TDbContext>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> serviceCollection = default)
            where TApi : ApiBase
            where TDbContext : DbContext

        {
            var modelBuilder = await GetTestableInjectedService<TApi, TDbContext, IModelBuilder>(routeName, routePrefix, serviceCollection).ConfigureAwait(false);

            var innerBuilders = new List<string>
            {
                modelBuilder.GetType().FullName
            };
            var builder = GetInnerBuilder(modelBuilder);
            do
            {
                innerBuilders.Add(builder.GetType().FullName);
                builder = GetInnerBuilder(builder);
            }
            while (builder != null);
            return innerBuilders;

            //RWM: Only need this here, so make it a private function
            static IModelBuilder GetInnerBuilder(object builder)
            {
                return (IModelBuilder)builder.GetPropertyValue("InnerHandler", false) ?? (IModelBuilder)builder.GetPropertyValue("InnerModelBuilder", false);
            }

        }

        #endregion

        #region GetTestableApiInstance

        /// <summary>
        /// Retrieves the instance of the Restier API (inheriting from <see cref="ApiBase"/> from the Dependency Injection container.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
        public static async Task<TApi> GetTestableApiInstance<TApi, TDbContext>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> serviceCollection = default)
            where TApi : ApiBase
            where TDbContext : DbContext => await GetTestableInjectedService<TApi, TDbContext, ApiBase>(routeName, routePrefix, serviceCollection).ConfigureAwait(false) as TApi;

        #endregion

        #region GetTestableInjectedService

        /// <summary>
        /// Retrieves class instance of type <typeparamref name="TService"/> from the Dependency Injection container.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <typeparam name="TService">The type whose instance should be retrieved from the DI container.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
        public static async Task<TService> GetTestableInjectedService<TApi, TDbContext, TService>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> serviceCollection = default)
            where TApi : ApiBase
            where TDbContext : DbContext
            where TService : class => (await GetTestableInjectionContainer<TApi, TDbContext>(routeName, routePrefix, serviceCollection).ConfigureAwait(false)).GetService<TService>();

        #endregion

        #region GetTestableInjectionContainer

        /// <summary>
        /// Retrieves the Dependency Injection container that was created as a part of the request pipeline.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "<Pending>")]
        public static async Task<IServiceProvider> GetTestableInjectionContainer<TApi, TDbContext>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> serviceCollection = default)
             where TApi : ApiBase
            where TDbContext : DbContext
        {

#if NET5_0_OR_GREATER

            using var testBase = GetTestBaseInstance<TApi, TDbContext>(routeName, routePrefix, serviceCollection);
            return await Task.FromResult(testBase.GetScopedRequestContainer()).ConfigureAwait(false);
#else
            using var config = await GetTestableRestierConfiguration<TApi, TDbContext>(routeName, routePrefix, serviceCollection: serviceCollection).ConfigureAwait(false);
            var request = HttpClientHelpers.GetTestableHttpRequestMessage(HttpMethod.Get, WebApiConstants.Localhost, routePrefix);
            request.SetConfiguration(config);
            return request.CreateRequestContainer(routeName);
#endif

        }

        #endregion

        #region GetTestableRestierConfiguration

#if !NET5_0_OR_GREATER

        /// <summary>
        /// Retrieves an <see cref="HttpConfiguration"> instance that has been configured to execute a given Restier API, along with settings suitable for easy troubleshooting.</see>
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <param name="defaultQuerySettings">A <see cref="DefaultQuerySettings"/> instabce that defines how OData operations should work. Defaults to everything enabled with a <see cref="DefaultQuerySettings.MaxTop"/> of 10.</param>
        /// <param name="timeZoneInfo">A <see cref="TimeZoneInfo"/> instenace specifying what time zone should be used to translate time payloads into. Defaults to <see cref="TimeZoneInfo.Utc"/>.</param>
        /// <param name="serviceCollection"></param>
        /// <returns>An <see cref="HttpConfiguration"/> instance</returns>
        public static async Task<HttpConfiguration> GetTestableRestierConfiguration<TApi, TDbContext>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix,
            DefaultQuerySettings defaultQuerySettings = null, TimeZoneInfo timeZoneInfo = null, Action<IServiceCollection> serviceCollection = default)
            where TApi : ApiBase
            where TDbContext : DbContext
        {
            var config = new HttpConfiguration();
            Action<IServiceCollection> defaultConfigureServices = (services) => { services.AddEF6ProviderServices<TDbContext>(); };
            config.SetDefaultQuerySettings(defaultQuerySettings ?? QueryDefaults);
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.SetTimeZoneInfo(timeZoneInfo ?? TimeZoneInfo.Utc);
            config.UseRestier((builder) => builder.AddRestierApi<TApi>(services =>
            {
                if (serviceCollection != null)
                {
                    serviceCollection.Invoke(services);
                }
                else
                {
                    defaultConfigureServices.Invoke(services);
                }
            }));
            config.MapRestier((builder) => builder.MapApiRoute<TApi>(routeName, routePrefix, true), config.GetTestableHttpServer());
            return await Task.FromResult(config).ConfigureAwait(false);
        }

#endif

        #endregion

        #region GetTestableHttpClient

        /// <summary>
        /// Returns a properly configured <see cref="HttpClient"/> that can make reqests to the in-memory Restier context.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <param name="serviceCollection"></param>
        /// <returns>A properly configured <see cref="HttpClient"/> that can make reqests to the in-memory Restier context.</returns>
        public static async Task<HttpClient> GetTestableHttpClient<TApi, TDbContext>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> serviceCollection = default)
            where TApi : ApiBase
            where TDbContext : DbContext
        {

#if NET5_0_OR_GREATER
            var server = GetTestableRestierServer<TApi, TDbContext>(routeName, routePrefix, serviceCollection);
            var client = server.CreateClient();
            return await Task.FromResult(client).ConfigureAwait(false);
#else
            // JHC TODO: change this so that GetTestableHttpClient() is no longer async and refactor the net472 code as well
            using var config = await GetTestableRestierConfiguration<TApi, TDbContext>(routeName, routePrefix, serviceCollection: serviceCollection).ConfigureAwait(false);
            using var server = new HttpServer(config);
            return new HttpClient(server);
#endif

        }

        #endregion

        #region GetTestableModelAsync

        /// <summary>
        /// Retrieves the <see cref="IEdmModel"/> instance for a given API, whether it used a custom ModelBuilder or the RestierModelBuilder.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <param name="serviceCollection"></param>
        /// <returns>An <see cref="IEdmModel"/> instance containing the model used to configure both OData and Restier processing.</returns>
        public static async Task<IEdmModel> GetTestableModelAsync<TApi, TDbContext>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> serviceCollection = default)
            where TApi : ApiBase
            where TDbContext : DbContext
        {
            var api = await GetTestableApiInstance<TApi, TDbContext>(routeName, routePrefix, serviceCollection: serviceCollection).ConfigureAwait(false);
            return api.GetModel();
        }

        #endregion

        #region GetApiMetadataAsync

        /// <summary>
        /// Executes a test request against the configured API endpoint and retrieves the content from the /$metadata endpoint.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <param name="host"></param>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <param name="serviceCollection"></param>
        /// <returns>An <see cref="XDocument"/> containing the results of the metadata request.</returns>
        public static async Task<XDocument> GetApiMetadataAsync<TApi, TDbContext>(string host = WebApiConstants.Localhost, string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> serviceCollection = default)
            where TApi : ApiBase
            where TDbContext : DbContext
        {
            var response = await ExecuteTestRequest<TApi, TDbContext>(HttpMethod.Get, host, routeName, routePrefix, "/$metadata", acceptHeader: "application/xml", serviceCollection: serviceCollection).ConfigureAwait(false);
            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                Trace.WriteLine(result);
                return null;
            }
            return XDocument.Parse(result);
        }

        #endregion

        #region WriteCurrentApiMetadata

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <param name="sourceDirectory"></param>
        /// <param name="suffix"></param>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
        public static async Task WriteCurrentApiMetadata<TApi, TDbContext>(string sourceDirectory = "", string suffix = "ApiMetadata", Action<IServiceCollection> serviceCollection = default)
            where TApi : ApiBase
            where TDbContext : DbContext
        {
            var filePath = $"{sourceDirectory}{typeof(TApi).Name}-{suffix}.txt";
            var result = await GetApiMetadataAsync<TApi, TDbContext>(serviceCollection: serviceCollection).ConfigureAwait(false);
            System.IO.File.WriteAllText(filePath, result.ToString());
        }

        #endregion

        #endregion

        #region Private Methods

#if NET5_0_OR_GREATER

        /// <summary>
        /// Gets a new <see cref="TestServer" />, configured for Restier and using the provided <see cref="Action{IServiceCollection}"/> to add additional services.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <returns>A new <see cref="TestServer" /> instance.</returns>
        public static TestServer GetTestableRestierServer<TApi, TDbContext>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix, Action<IServiceCollection> apiServiceCollection = default)
            where TApi : ApiBase
            where TDbContext : DbContext => GetTestBaseInstance<TApi, TDbContext>(routeName, routePrefix, apiServiceCollection).TestServer;

        /// <summary>
        /// Gets a new <see cref="TestServer" />, configured for Restier and using the provided <see cref="Action{IServiceCollection}"/> to add additional services.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TDbContext">The class inheriting from <see cref="DbContext"/> that connects to the database used bt <typeparamref name="TApi"/>.</typeparam>
        /// <returns>A new <see cref="TestServer" /> instance.</returns>
        public static RestierBreakdanceTestBase<TApi> GetTestBaseInstance<TApi, TDbContext>(string routeName = WebApiConstants.RouteName, 
            string routePrefix = WebApiConstants.RoutePrefix, Action<IServiceCollection> apiServiceCollection = default)
            where TApi : ApiBase
            where TDbContext : DbContext
        {
            using var restierTests = new RestierBreakdanceTestBase<TApi>();

            restierTests.AddRestierAction = (apiBuilder) =>
            {
                apiBuilder.AddRestierApi<TApi>(restierServices =>
                {
                    restierServices
                        .AddEFCoreProviderServices<TDbContext>()
                        .AddSingleton(new ODataValidationSettings
                        {
                            MaxTop = 5,
                            MaxAnyAllExpressionDepth = 3,
                            MaxExpansionDepth = 3,
                        });
                    apiServiceCollection?.Invoke(restierServices);
                });
            };

            restierTests.MapRestierAction = (routeBuilder) =>
            {
                routeBuilder.MapApiRoute<TApi>(routeName, routePrefix);
            };

            // make sure the TestServer has been started
            restierTests.TestSetup();

            // JHC TODO: my attempts at doing seeding in the unit test after the service starts up

            //using (var scope = restierTests.TestServer.Services.CreateScope())
            //{
            //    var services = scope.ServiceProvider;
            //    var context = services.GetRequiredService<TDbContext>();
            //    // NOTE: we aren't adding the initializer to the services, so this won't work
            //    //initializer.Seed(context);
            //}

            /* NOTE: or is this right?  if we do it this way, then it is up to the initializer to initializer ALL DbContexts found in the service collection
             * */


            return restierTests;
        }

#endif

        #endregion

    }
}