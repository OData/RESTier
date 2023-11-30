// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
#if NET6_0_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.AspNetCore.Http;
#else
using CloudNimble.Breakdance.WebApi;
#endif
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else
namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

    /// <summary>
    /// A class for testing OData Actions.
    /// </summary>
    [TestClass]
    public class ActionTests : RestierTestBase
#if NET6_0_OR_GREATER
        <LibraryApi>
#endif
    {

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
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Post, resource: "/CheckoutBook", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
#if !NET7_0_OR_GREATER
            content.Should().Contain("ArgumentNullException");
#else
            // RWM: ASP.NET Core 7.0 Breaking change: 
            // https://docs.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/7.0/mvc-empty-body-model-binding
            // TODO: RWM or JHC: Fix the RestierController to return the right result on .NET 7.
            content.Should().Contain("Model state is not valid");
#endif
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

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Post, resource: "/CheckoutBook", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

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

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Post, resource: "/CheckoutBook", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();

            content.Should().Contain("Robert McLaws");
            content.Should().Contain("| Submitted");
        }

    }

}