// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

#if NETCOREAPP3_1_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else

using CloudNimble.Breakdance.WebApi;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

    [TestClass]
    public class InsertTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <LibraryApi>
#endif
    {

        [TestMethod]
        public async Task InsertBook()
        {
            var book = new Book
            {
                Title = "Inserting Yourself into Every Situation",
                Isbn = "0118006345789"
            };

            var bookInsertRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Post, resource: $"/Publishers('Publisher1')/Books", payload: book, acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            bookInsertRequest.Should().NotBeNull();
            
            var (book2, errorContent2) = await bookInsertRequest.DeserializeResponseAsync<Book>();
            
            bookInsertRequest.IsSuccessStatusCode.Should().BeTrue();
            book2.Should().NotBeNull();
            book2.Id.Should().NotBeEmpty();
        }

    }

}