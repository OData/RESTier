// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

#if NET6_0_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else

using CloudNimble.Breakdance.WebApi;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

#if NET6_0_OR_GREATER

    [TestClass]
    [TestCategory("Endpoint Routing")]
    public class InsertTests_EndpointRouting : InsertTests
    {
        public InsertTests_EndpointRouting() : base(true)
        {
        }
    }

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class InsertTests_LegacyRouting : InsertTests
    {
        public InsertTests_LegacyRouting() : base(false)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public abstract class InsertTests : RestierTestBase<LibraryApi>
    {

        public InsertTests(bool useEndpointRouting) : base(useEndpointRouting)
        {
            //AddRestierAction = builder =>
            //{
            //    builder.AddRestierApi<LibraryApi>(services => services.AddEntityFrameworkServices<LibraryContext>());
            //};
            //MapRestierAction = routeBuilder =>
            //{
            //    routeBuilder.MapApiRoute<LibraryApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix, false);
            //};
        }

        //[TestInitialize]
        //public void ClaimsTestSetup() => TestSetup();

#else

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class InsertTests : RestierTestBase
    {

#endif

            [TestMethod]
            public async Task InsertBook()
            {
                var book = new Book
                {
                    Title = "Inserting Yourself into Every Situation",
                    Isbn = "0118006345789"
                };

                var bookInsertRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Post, resource: $"/Publishers('Publisher1')/Books",
                    payload: book, acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                    useEndpointRouting: UseEndpointRouting);
                bookInsertRequest.Should().NotBeNull();

                var (book2, errorContent2) = await bookInsertRequest.DeserializeResponseAsync<Book>();

                bookInsertRequest.IsSuccessStatusCode.Should().BeTrue();
                book2.Should().NotBeNull();
                book2.Id.Should().NotBeEmpty();
            }

        }

    }