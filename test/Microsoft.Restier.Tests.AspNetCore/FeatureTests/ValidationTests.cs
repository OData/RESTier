// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class ValidationTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    [TestMethod]
    public async Task Validation_StringLengthExceeded()
    {
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=1",
            acceptHeader: ODataConstants.MinimalAcceptHeader,
            serviceCollection: ConfigureServices);
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, errorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

        bookList.Should().NotBeNull();
        bookList.Items.Should().NotBeNullOrEmpty();
        errorContent.Should().BeNullOrEmpty();

        var book = bookList.Items.First();
        book.Should().NotBeNull();

        book.Isbn = "This is a really really long string.";

        var bookEditResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Put,
            resource: $"/Books({book.Id})",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(bookEditResponse);

        bookEditResponse.IsSuccessStatusCode.Should().BeFalse();
        content.Should().Contain("validationentries");
        content.Should().Contain("MaxLengthAttribute");
    }
}
