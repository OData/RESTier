// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FallbackTests;

public class ODataControllerFallbackTests : RestierTestBase<FallbackApi>
{
    public ODataControllerFallbackTests()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<FallbackApi>(WebApiConstants.RoutePrefix, restierServices =>
            {
                AddTestServices(restierServices);
            },
            bag =>
            {
                bag.Validation.MaxTop = 5;
                bag.Validation.MaxAnyAllExpressionDepth = 3;
                bag.Validation.MaxExpansionDepth = 3;
            });
        };
        TestSetup();
    }

    private static void AddTestServices(IServiceCollection services)
    {
        services
            .AddSingleton<IChainedService<IModelBuilder>>(new StoreModelProducer(FallbackModel.Model))
            .AddSingleton<IChainedService<IModelMapper>, FallbackModelMapper>()
            .AddSingleton<IChainedService<IQueryExpressionSourcer>, FallbackQueryExpressionSourcer>()
            .AddSingleton<IChangeSetInitializer, StoreChangeSetInitializer>()
            .AddSingleton<ISubmitExecutor, DefaultSubmitExecutor>();
    }

    [Fact]
    public async Task FallbackApi_EntitySet_ShouldFallBack()
    {
        // Should fallback to PeopleController.
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/People");
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4List<Person>>();
        var first = Response.Items.FirstOrDefault();
        first.Should().NotBeNull();
        first.Id.Should().Be(999);
    }

    [Fact]
    public async Task FallbackApi_NavigationProperty_ShouldFallBack()
    {
        // Should fallback to PeopleController.
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/People(1)/Orders");
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();

        var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4List<Order>>();
        var first = Response.Items.FirstOrDefault();
        first.Should().NotBeNull();
        first.Id.Should().Be(123);
    }

    [Fact]
    public async Task FallbackApi_EntitySet_ShouldNotFallBack()
    {
        // Should be routed to RestierController.
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Orders");
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        (await response.Content.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken)).Should().Contain("\"Id\":234");
    }

    [Fact]
    public async Task FallbackApi_Resource_ShouldNotFallBack()
    {
        // Should be routed to RestierController.
        var metadata = await GetApiMetadataAsync();
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/PreservedOrders");

        metadata.Should().NotBeNull();
        metadata.Descendants().Where(c => c.Name.LocalName == "EntitySet").Should().HaveCount(3);

        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"Id\":234");
    }
}
