// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests;

/// <summary>
/// Regression tests for https://github.com/OData/RESTier/issues/519.
/// Verifies that OnFilter methods are applied to single navigation properties during $expand.
/// </summary>
public abstract class Issue519_SingleNavPropertyFilter<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    protected Issue519_SingleNavPropertyFilter()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<TApi>(WebApiConstants.RoutePrefix, services =>
            {
                ConfigureServices(services);
            });
        };
        TestSetup();
    }

    /// <summary>
    /// Verifies that OnFilter is applied to single navigation properties during $expand.
    /// Books whose publisher does not pass the OnFilterPublishers filter should have null Publisher.
    /// </summary>
    [TestMethod]
    public async Task ExpandSingleNavProperty_ShouldApplyFilter()
    {
        // Query books with expanded Publisher. The FilteredPublisherLibraryApi filters publishers
        // to only include "Publisher1". Books belonging to "Publisher2" should have a null Publisher
        // in the response, and books belonging to "Publisher1" should still have their Publisher.
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Books?$expand=Publisher");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();

        // "A Clockwork Orange" belongs to Publisher1 — its Publisher should be present
        content.Should().Contain("A Clockwork Orange");
        content.Should().Contain("Publisher1");

        // "Color Purple, The" belongs to Publisher2 — its Publisher should be filtered out (null)
        content.Should().Contain("Color Purple");
        // Publisher2's Publisher navigation object should NOT appear in the response because the filter excludes it.
        // Note: PublisherId may still appear as a scalar FK value; we check the navigation object is absent.
        content.Should().NotContain("\"Id\":\"Publisher2\"");
    }

    /// <summary>
    /// Verifies that the collection navigation $expand still works with filters applied.
    /// </summary>
    [TestMethod]
    public async Task ExpandCollectionNavProperty_ShouldStillApplyFilter()
    {
        // Query publishers with expanded Books. The OnFilterBooks filter (from LibraryApi)
        // should still apply, filtering inactive books.
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Publishers?$expand=Books");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();

        // Active books should be present
        content.Should().Contain("A Clockwork Orange");

        // "Sea of Rustoleum" is inactive and should be filtered out by OnFilterBooks
        content.Should().NotContain("Sea of Rustoleum");
    }
}
