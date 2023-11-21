// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER

using CloudNimble.Breakdance.AspNetCore;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Restier.Tests.Breakdance
{

    /// <summary>
    /// 
    /// </summary>
    public abstract class TestHarnessBase
    {

        /// <summary>
        /// 
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Public Members

        /// <summary>
        /// TODO: @robertmclaws: This needs to be modified for the new Endpoint Routing support.
        /// </summary>
        public Action<RestierApiBuilder> LibraryAddRestierAction = (apiBuilder) =>
        {
            apiBuilder.AddRestierApi<LibraryApi>(restierServices =>
            {
                restierServices
                    .AddEFCoreProviderServices<LibraryContext>()
                    .AddSingleton(new ODataValidationSettings
                    {
                        MaxTop = 5,
                        MaxAnyAllExpressionDepth = 3,
                        MaxExpansionDepth = 3,
                    });

#if EFCore
                using var tempServices = restierServices.BuildServiceProvider();

                var scopeFactory = tempServices.GetService<IServiceScopeFactory>();
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<LibraryContext>();

                // EnsureCreated() returns false if the database already exists
                if (dbContext.Database.EnsureCreated())
                {
                    var initializer = new LibraryTestInitializer();
                    initializer.Seed(dbContext);
                }
#endif

            });
        };

        /// <summary>
        /// 
        /// </summary>
        public Action<RestierRouteBuilder> LibraryMapRestierAction = (routeBuilder) =>
        {
            routeBuilder.MapApiRoute<LibraryApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix);
        };

        /// <summary>
        /// 
        /// </summary>
        public Action<RestierApiBuilder> MarvelAddRestierAction = (apiBuilder) =>
        {
            apiBuilder.AddRestierApi<MarvelApi>(restierServices =>
            {
                restierServices
                    .AddEFCoreProviderServices<MarvelContext>()
                    .AddSingleton(new ODataValidationSettings
                    {
                        MaxTop = 5,
                        MaxAnyAllExpressionDepth = 3,
                        MaxExpansionDepth = 3,
                    });

#if EFCore
                using var tempServices = restierServices.BuildServiceProvider();

                var scopeFactory = tempServices.GetService<IServiceScopeFactory>();
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<MarvelContext>();

                // EnsureCreated() returns false if the database already exists
                if (dbContext.Database.EnsureCreated())
                {
                    var initializer = new MarvelTestInitializer();
                    initializer.Seed(dbContext);
                }
#endif

            });
        };

        /// <summary>
        /// 
        /// </summary>
        public Action<RestierRouteBuilder> MarvelMapRestierAction = (routeBuilder) =>
        {
            routeBuilder.MapApiRoute<MarvelApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix);
        };

        /// <summary>
        /// 
        /// </summary>
        public Action<RestierApiBuilder> StoreAddRestierAction = (apiBuilder) =>
        {
            apiBuilder.AddRestierApi<StoreApi>(restierServices =>
            {
                restierServices
                    .AddTestStoreApiServices()
                    .AddSingleton(new ODataValidationSettings
                    {
                        MaxTop = 5,
                        MaxAnyAllExpressionDepth = 3,
                        MaxExpansionDepth = 3,
                    });
            });
        };

        /// <summary>
        /// 
        /// </summary>
        public Action<RestierRouteBuilder> StoreMapRestierAction = (routeBuilder) =>
        {
            routeBuilder.MapApiRoute<StoreApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix);
        };

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TApi"></typeparam>
        /// <returns></returns>
        public RestierBreakdanceTestBase<TApi> GetTestBaseInstance<TApi>() where TApi: ApiBase
        {
            var testBase = true switch
            {
                true when typeof(TApi) == typeof(LibraryApi) => new RestierBreakdanceTestBase<TApi>
                {
                    AddRestierAction = LibraryAddRestierAction,
                    MapRestierAction = LibraryMapRestierAction
                },
                true when typeof(TApi) == typeof(MarvelApi) => new RestierBreakdanceTestBase<TApi>
                {
                    AddRestierAction = MarvelAddRestierAction,
                    MapRestierAction = MarvelMapRestierAction
                },
                true when typeof(TApi) == typeof(StoreApi) => new RestierBreakdanceTestBase<TApi>
                {
                    AddRestierAction = StoreAddRestierAction,
                    MapRestierAction = StoreMapRestierAction
                },
                _ => null,
            };
            testBase?.TestSetup();
            return testBase;
        }

        #endregion

    }

}

#endif