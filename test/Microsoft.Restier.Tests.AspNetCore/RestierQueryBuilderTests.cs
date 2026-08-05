// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore;

/// <summary>
/// Tests that verify various key types work correctly with the RESTier query builder.
/// </summary>
[TestClass]
public class RestierQueryBuilderTests : RestierTestBase<StoreApi>
{
    private static void di(IServiceCollection services)
    {
        services.AddTestStoreApiServices();
    }

    [TestMethod]
    public async Task TestInt16AsKey()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Customers(1)", serviceCollection: di);
        response.IsSuccessStatusCode.Should().BeTrue();
        TraceListener.WriteLine(await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task TestInt64AsKey()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Stores(1)", serviceCollection: di);
        response.IsSuccessStatusCode.Should().BeTrue();
        TraceListener.WriteLine(await response.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));
    }
}
