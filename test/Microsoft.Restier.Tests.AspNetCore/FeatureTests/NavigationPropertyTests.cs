// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class NavigationPropertyTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    protected abstract Task<object> AddPublisherAndSaveAsync(Publisher publisher);

    protected abstract Task<object> AddPublishersAndSaveAsync(Publisher p1, Publisher p2);

    protected abstract void CleanupPublisherData(object contextObj, Publisher publisher);

    [TestMethod]
    public async Task NavigationProperties_ChildrenShouldFilter_IsActive()
    {
        var publisher = new Publisher
        {
            Id = "navtest-publisher-1",
            Books =
            [
                new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-1", IsActive = true },
                new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-2", IsActive = false },
            ],
            Addr = new Shared.Scenarios.Library.Address { Zip = "12345" },
        };
        var context = await AddPublisherAndSaveAsync(publisher);

        try
        {
            var request = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher.Id}')?$expand=Books",
                acceptHeader: ODataConstants.DefaultAcceptHeader,
                serviceCollection: ConfigureServices);
            request.IsSuccessStatusCode.Should().BeTrue();

            var (expandedPublisher, _) = await request.DeserializeResponseAsync<Publisher>();
            expandedPublisher.Should().NotBeNull();
            expandedPublisher.Books.Should().HaveCount(1);

            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher.Id}')/Books",
                serviceCollection: ConfigureServices);
            response.IsSuccessStatusCode.Should().BeTrue();

            var (books, _) = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            books.Items.Should().HaveCount(1);
        }
        finally
        {
            CleanupPublisherData(context, publisher);
        }
    }

    [TestMethod]
    public async Task NavigationProperties_ChildrenShouldFilter_Explicit()
    {
        var publisher = new Publisher
        {
            Id = "navtest-publisher-1",
            Books =
            [
                new Book { Id = Guid.NewGuid(), Title = "top10-navtest-pub1-book-1", IsActive = true },
                new Book { Id = Guid.NewGuid(), Title = "top5-navtest-pub1-book-2", IsActive = true },
            ],
            Addr = new Shared.Scenarios.Library.Address { Zip = "12345" },
        };
        var context = await AddPublisherAndSaveAsync(publisher);

        try
        {
            var request = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher.Id}')?$expand=Books($filter=startswith(Title, 'top10'))",
                acceptHeader: ODataConstants.DefaultAcceptHeader,
                serviceCollection: ConfigureServices);
            request.IsSuccessStatusCode.Should().BeTrue();

            var (expandedPublisher, _) = await request.DeserializeResponseAsync<Publisher>();
            expandedPublisher.Should().NotBeNull();
            expandedPublisher.Books.Should().HaveCount(1);

            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher.Id}')/Books?$filter=startswith(Title, 'top10')",
                serviceCollection: ConfigureServices);
            response.IsSuccessStatusCode.Should().BeTrue();

            var (books, _) = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            books.Items.Should().HaveCount(1);
        }
        finally
        {
            CleanupPublisherData(context, publisher);
        }
    }

    [TestMethod]
    public async Task NavigationProperties_ChildrenShouldFilter_AcrossProviders()
    {
        var publisher1 = new Publisher
        {
            Id = "navtest-publisher-1",
            Books =
            [
                new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-1", IsActive = true },
                new Book { Id = Guid.NewGuid(), Title = "navtest-pub1-book-2", IsActive = true },
            ],
            Addr = new Shared.Scenarios.Library.Address { Zip = "12345" },
        };
        var publisher2 = new Publisher
        {
            Id = "navtest-publisher-2",
            Books =
            [
                new Book { Id = Guid.NewGuid(), Title = "navtest-pub2-book-3", IsActive = true },
            ],
            Addr = new Shared.Scenarios.Library.Address { Zip = "12345" },
        };

        var context = await AddPublishersAndSaveAsync(publisher1, publisher2);

        try
        {
            var request = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher1.Id}')?$expand=Books",
                acceptHeader: ODataConstants.DefaultAcceptHeader,
                serviceCollection: ConfigureServices);
            request.IsSuccessStatusCode.Should().BeTrue();

            var (expandedPublisher, _) = await request.DeserializeResponseAsync<Publisher>();
            expandedPublisher.Should().NotBeNull();
            expandedPublisher.Books.Should().HaveCount(2);

            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: $"/Publishers('{publisher1.Id}')/Books",
                serviceCollection: ConfigureServices);
            response.IsSuccessStatusCode.Should().BeTrue();

            var (books, _) = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            books.Items.Should().HaveCount(2);
        }
        finally
        {
            CleanupPublisherData(context, publisher1);
            CleanupPublisherData(context, publisher2);
        }
    }
}
