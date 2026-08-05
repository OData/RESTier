// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class InsertTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    [TestMethod]
    public async Task InsertBook()
    {
        var book = new Book
        {
            Title = "Inserting Yourself into Every Situation",
            Isbn = "0118006345789",
        };

        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers('Publisher1')/Books",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        response.Should().NotBeNull();

        var createdBookResult = await response.DeserializeResponseAsync<Book>();
        var createdBook = createdBookResult.Response;

        response.IsSuccessStatusCode.Should().BeTrue();
        createdBook.Should().NotBeNull();
        createdBook.Id.Should().NotBeEmpty();
    }
}
