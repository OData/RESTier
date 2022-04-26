// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System;
using System.Net;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

#if NETCOREAPP3_1_OR_GREATER
using CloudNimble.Breakdance.AspNetCore.OData;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else

using CloudNimble.Breakdance.WebApi;
using CloudNimble.Breakdance.WebApi.OData;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

    [TestClass]
    public class NavigationPropertyTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <LibraryApi>
#endif
    {

        [TestMethod]
        public async Task NavigationProperties_ChildrenShouldFilter_IsActive()
        {
            // set up the context to have the needed records for this test
            var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());

            var publisher1 = new Publisher 
            { 
                Id = "navtest-publisher-1", 
                Books = new System.Collections.ObjectModel.ObservableCollection<Book>
                {
                    new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-1", IsActive = true },
                    new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-2", IsActive = false }
                }, 
                Addr = new Shared.Scenarios.Library.Address { Zip = "12345" } 
            };
            context.Publishers.Add(publisher1);

            // save publishers to the context
            context.SaveChanges();

            // double check that the first publisher has the expected amount of books
            var request = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/Publishers('{publisher1.Id}')?$expand=Books", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            request.IsSuccessStatusCode.Should().BeTrue();
            var (publisher, ErrorContent1) = await request.DeserializeResponseAsync<Publisher>();
            publisher.Should().NotBeNull();
            publisher.Books.Should().HaveCount(1);

            // query books with the navigation filter
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/Publishers('{publisher1.Id}')/Books", serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

            response.IsSuccessStatusCode.Should().BeTrue();
            var (books, ErrorContent2) = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            books.Items.Should().HaveCount(1);

            // clean up the context
            var removeBooks = publisher1.Books.ToList();
            foreach (var book in removeBooks)
            {
                context.Books.Remove(book);
            }
            context.Publishers.Remove(publisher1);
            context.SaveChanges();
        }

        [TestMethod]
        public async Task NavigationProperties_ChildrenShouldFilter_Explicit()
        {
            // set up the context to have the needed records for this test
            var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());

            var publisher1 = new Publisher
            {
                Id = "navtest-publisher-1",
                Books = new System.Collections.ObjectModel.ObservableCollection<Book>
                {
                    new Book { Id = Guid.NewGuid(), Title = "top10-navtest-pub1-book-1", IsActive = true },
                    new Book { Id = Guid.NewGuid(), Title = "top5-navtest-pub1-book-2", IsActive = true },
                },
                Addr = new Shared.Scenarios.Library.Address { Zip = "12345" }
            };
            context.Publishers.Add(publisher1);

            // save publishers to the context
            context.SaveChanges();

            // double check that the first publisher has the expected amount of books
            var request = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/Publishers('{publisher1.Id}')?$expand=Books($filter=startswith(Title, 'top10'))", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            request.IsSuccessStatusCode.Should().BeTrue();
            var (publisher, ErrorContent1) = await request.DeserializeResponseAsync<Publisher>();
            publisher.Should().NotBeNull();
            publisher.Books.Should().HaveCount(1);

            // query books with the navigation filter
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/Publishers('{publisher1.Id}')/Books?$filter=startswith(Title, 'top10')", serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

            response.IsSuccessStatusCode.Should().BeTrue();
            var (bookData, ErrorContent2) = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            bookData.Items.Should().HaveCount(1);

            // clean up the context
            var removeBooks = publisher1.Books.ToList();
            foreach (var book in removeBooks)
            {
                context.Books.Remove(book);
            }
            context.Publishers.Remove(publisher1);
            context.SaveChanges();
        }

        [TestMethod]
        public async Task NavigationProperties_ChildrenShouldFilter_AcrossProviders()
        {
            // set up the context to have the needed records for this test
            var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());

            var publisher1 = new Publisher
            {
                Id = "navtest-publisher-1",
                Books = new System.Collections.ObjectModel.ObservableCollection<Book>
                {
                    new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-1", IsActive = true },
                    new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-2", IsActive = true },
                },
                Addr = new Shared.Scenarios.Library.Address { Zip = "12345" }
            };
            context.Publishers.Add(publisher1);

            var publisher2 = new Publisher
            {
                Id = "navtest-publisher-2",
                Books = new System.Collections.ObjectModel.ObservableCollection<Book>
                {
                    new Book { Id = Guid.NewGuid(), Title = "navtest-pub2-book-3", IsActive = true },
                },
                Addr = new Shared.Scenarios.Library.Address { Zip = "12345" }
            };
            context.Publishers.Add(publisher2);

            // save publishers to the context
            context.SaveChanges();

            // double check that the first publisher has the expected amount of books
            var request = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/Publishers('{publisher1.Id}')?$expand=Books", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            request.IsSuccessStatusCode.Should().BeTrue();
            var (publisher, ErrorContent1) = await request.DeserializeResponseAsync<Publisher>();
            publisher.Should().NotBeNull();
            publisher.Books.Should().HaveCount(2);

            // query books with the navigation filter
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/Publishers('{publisher1.Id}')/Books", serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

            response.IsSuccessStatusCode.Should().BeTrue();
            var (bookData, ErrorContent2) = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            bookData.Items.Should().HaveCount(2);

            // clean up the context
            var removeBooks = publisher1.Books.ToList();
            foreach (var book in removeBooks)
            {
                context.Books.Remove(book);
            }
            context.Publishers.Remove(publisher1);

            removeBooks = publisher2.Books.ToList();
            foreach (var book in removeBooks)
            {
                context.Books.Remove(book);
            }
            context.Publishers.Remove(publisher2);

            context.SaveChanges();

        }
    }

}