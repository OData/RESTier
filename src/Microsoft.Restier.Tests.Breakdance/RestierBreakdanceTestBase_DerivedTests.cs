// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER

using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.Breakdance
{

    [TestClass]
    [TestCategory("Endpoint Routing")]
    public class RestierBreakdanceTestBase_DerivedTests_EndpointRouting : RestierBreakdanceTestBase_DerivedTests
    {
        public RestierBreakdanceTestBase_DerivedTests_EndpointRouting() : base(true)
        {
        }
    }

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class RestierBreakdanceTestBase_DerivedTests_LegacyRouting : RestierBreakdanceTestBase_DerivedTests
    {
        public RestierBreakdanceTestBase_DerivedTests_LegacyRouting() : base(false)
        {
        }
    }

    [TestClass]
    public abstract class RestierBreakdanceTestBase_DerivedTests : RestierBreakdanceTestBase<LibraryApi>
    {

        #region Constructors

        public RestierBreakdanceTestBase_DerivedTests(bool useEndpointRouting) : base(useEndpointRouting)
        {
            AddRestierAction = (apiBuilder) =>
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
            MapRestierAction = (routeBuilder) =>
            {
                routeBuilder.MapApiRoute<LibraryApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix);
            };
        }

        #endregion

        [TestInitialize]
        public void TestInitialize() => base.TestSetup();

        [TestMethod]
        public void TestSetup_ServerAndServicesAreAvailable()
        {
            TestServer.Should().NotBeNull();
            TestServer.Services.Should().NotBeNull();
        }

        [TestMethod]
        public void TestSetup_ScopeFactoryIsPresent()
        {
            var factory = TestServer.Services.GetRequiredService<IServiceScopeFactory>();
            factory.Should().NotBeNull();
        }

        [TestMethod]
        public async Task HttpClient_ShouldReturnRootContent()
        {

            var client = GetHttpClient();
            var result = await client.GetAsync("");
            var resultContent = await result.Content.ReadAsStringAsync();

            resultContent.Should().ContainAll("$metadata", "Books", "LibraryCards", "Publishers", "Readers");
        }

        [TestMethod]
        public async Task GetApiMetadataAsync_ReturnsXDocument()
        {
            var metadata = await GetApiMetadataAsync();
            metadata.Should().NotBeNull();
        }

        [TestMethod]
        public void GetScopedRequestContainer_ReturnsInstance()
        {
            var container = GetScopedRequestContainer(useEndpointRouting: UseEndpointRouting);
            container.Should().NotBeNull();
        }

        [TestMethod]
        public void GetApiInstance_ReturnsInstanceFromRequestScope()
        {
            var api = GetApiInstance(useEndpointRouting: UseEndpointRouting);
            api.Should().NotBeNull();
        }

    }

}

#endif