// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class ValidationOptionsTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    [Fact]
    public async Task Bag_MaxTop_RejectsOverLimitTopWithBadRequest()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=99",
            serviceCollection: ConfigureServices,
            configureOptions: o => o.Validation.MaxTop = 3);

        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeFalse();
        ((int)response.StatusCode).Should().Be(400);
        content.Should().Contain("Top");
    }

    [Fact]
    public async Task Bag_MaxTop_AllowsUnderLimitTop()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=2",
            serviceCollection: ConfigureServices,
            configureOptions: o => o.Validation.MaxTop = 3);

        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("@odata.context");
    }
}
