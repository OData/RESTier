// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
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
/// Regression tests for https://github.com/OData/RESTier/issues/671.
/// Tests a single LibraryContext registration.
/// </summary>
public abstract class Issue671_MultipleContexts_SingleLibraryContext<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    protected Issue671_MultipleContexts_SingleLibraryContext()
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

    [TestMethod]
    public async Task SingleContext_LibraryApiWorks()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/LibraryCards");
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

/// <summary>
/// Regression tests for https://github.com/OData/RESTier/issues/671.
/// Tests a single MarvelContext registration.
/// </summary>
public abstract class Issue671_MultipleContexts_SingleMarvelContext<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    protected Issue671_MultipleContexts_SingleMarvelContext()
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

    [TestMethod]
    public async Task SingleContext_MarvelApiWorks()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Characters");
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

/// <summary>
/// Regression tests for https://github.com/OData/RESTier/issues/671.
/// Tests multiple context registrations (Library + Marvel).
/// </summary>
public abstract class Issue671_MultipleContexts<TLibraryApi, TMarvelApi> : RestierTestBase<TLibraryApi>
    where TLibraryApi : ApiBase
    where TMarvelApi : ApiBase
{
    protected abstract Action<IServiceCollection> ConfigureLibraryServices { get; }
    protected abstract Action<IServiceCollection> ConfigureMarvelServices { get; }

    protected Issue671_MultipleContexts()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<TLibraryApi>("Library", services =>
            {
                ConfigureLibraryServices(services);
            });
            options.AddRestierRoute<TMarvelApi>("Marvel", services =>
            {
                ConfigureMarvelServices(services);
            });
        };
        TestSetup();
    }

    [TestMethod]
    public async Task MultipleContexts_ShouldQueryFirstContext()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, routePrefix: "Library", resource: "/Books?$count=true");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();

        // Other tests (e.g., DeepInsert) may add Books to the shared database,
        // so assert that the count is at least the seeded baseline rather than exact.
        var match = Regex.Match(content, @"""@odata\.count"":(\d+),");
        match.Success.Should().BeTrue(because: "$count should be present in the response");
        int.Parse(match.Groups[1].Value).Should().BeGreaterThanOrEqualTo(5,
            because: "the database is seeded with 5 active books (OnFilterBooks hides inactive)");
    }

    [TestMethod]
    public async Task MultipleContexts_ShouldQuerySecondContext()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, routePrefix: "Marvel", resource: "/Characters?$count=true");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"@odata.count\":1,");
    }
}
