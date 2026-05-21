// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
{

    /// <summary>
    /// A class for testing OData Actions.
    /// </summary>
    public abstract class ActionTests<TApi, TContext> : RestierTestBase<TApi>
        where TApi : ApiBase where TContext : class
    {
        protected abstract Action<IServiceCollection> ConfigureServices { get; }

        /* JHC note: just leaving this here temporarily for reference
        #if EF6
                void addTestServices<TDbContext>(IServiceCollection services) where TDbContext : DbContext => services.AddEF6ProviderServices<TDbContext>();
        #endif

        #if EFCore
                void addTestServices<TDbContext>(IServiceCollection services) where TDbContext : DbContext => services.AddEFCoreProviderServices<TDbContext>();
        #endif
        */
        //[Ignore]
        [TestMethod]
        public async Task ActionParameters_MissingParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Post, resource: "/CheckoutBook", serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Contain("Error: A non-empty request body is required.");
        }

        [TestMethod]
        public async Task ActionParameters_WrongParameterName()
        {
            var bookPayload = new {
                john = new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "Constantly Frustrated: the Robert McLaws Story",
                }
            };

            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Post, resource: "/CheckoutBook", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload, serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeFalse();

            content.Should().Contain("Model state is not valid");
        }

        [TestMethod]
        public async Task ActionParameters_HasParameter()
        {
            var bookPayload = new {
                book = new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "Constantly Frustrated: the Robert McLaws Story",
                }
            };

            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Post, resource: "/CheckoutBook", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload, serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();

            content.Should().Contain("Robert McLaws");
            content.Should().Contain("| Submitted");
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [TestMethod]
        public async Task BoundAction_WithParameter_Returns200()
        {
            var metadata = RestierTestHelpers.GetApiMetadataAsync<TApi>(serviceCollection: ConfigureServices);

            var payload = new { bookId = new Guid("2D760F15-974D-4556-8CDF-D610128B537E") };

             var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Post, resource: "/Publishers('Publisher1')/PublishNewBook", payload: payload,
                acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: ConfigureServices);

            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = await response.DeserializeResponseAsync<Publisher>();
            results.Should().NotBeNull();
            results.Response.Should().NotBeNull();
            results.Response.Books.All(c => c.Title == "Sea of Rust").Should().BeTrue();
        }
    }

}
