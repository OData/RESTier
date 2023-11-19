// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER

using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests
{

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class Issue714_ComplexTypes : RestierTestBase<ComplexTypesApi>
    {

        #region Constructors

        /// <summary>
        /// Initializes the Test Server with the configuration it needs to run Restier services.
        /// </summary>
        public Issue714_ComplexTypes() : base()
        {
            ApplicationBuilderAction = (app) =>
            {
                app.UseResponseCompression();
                app.UseHttpsRedirection();
                app.UseRestierBatching();
            };

            TestHostBuilder.ConfigureServices((builder, services) =>
            {
                services
                    .AddHttpContextAccessor()
                    .AddResponseCompression()
                    .AddCors();
            });

            AddRestierAction = (apiBuilder) =>
            {
                apiBuilder.AddRestierApi<ComplexTypesApi>(routeServices =>
                {
                    routeServices
#if EF6
                        .AddEF6ProviderServices<MarvelContext>()
#elif EFCore
                        .AddEFCoreProviderServices<MarvelContext>()
#endif
                        .AddChainedService<IModelBuilder, ComplexTypesModelBuilder>();

                });
            };

            MapRestierAction = (routeBuilder) =>
            {
                routeBuilder.MapApiRoute<ComplexTypesApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix);
            };

        }

#endregion

#region Test Setup / Teardown

        /// <summary>
        /// Calls the base class to configure the test host and sets up test data.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            TestSetup();
        }

        /// <summary>
        /// Cleans up test data and calls base class to shut down the test host.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown();
        }

#endregion

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task ComplexTypes_WorkAsExpected()
        {
            var responseMessage = await ExecuteTestRequest(HttpMethod.Get, resource: "ComplexTypeTest()");
            responseMessage.Should().NotBeNull();

            responseMessage.IsSuccessStatusCode.Should().BeTrue();
            var content = await TestContext.LogAndReturnMessageContentAsync(responseMessage);

            content.Should().NotBeNullOrWhiteSpace();

        }

    }

#region ComplexTypesApi

    public class ComplexTypesApi : MarvelApi
    {

        public ComplexTypesApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [UnboundOperation(OperationType = OperationType.Function)]
        public LibraryCard ComplexTypeTest()
        {
            return new()
            {
                Id = Guid.NewGuid()
            };
        }

    }

#endregion

    /// <summary>
    /// Builds the EdmModel for the Restier API.
    /// </summary>
    /// <remarks>
    /// Hopefully this won't be necessary if we can get the OperationAttribute to register types it does not recognize.
    /// </remarks>
    public class ComplexTypesModelBuilder : IModelBuilder
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEdmModel GetModel(ModelContext context)
        {
            var modelBuilder = new ODataConventionModelBuilder();
            modelBuilder.ComplexType<LibraryCard>();
            return modelBuilder.GetEdmModel();
        }

    }

}

#endif