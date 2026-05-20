// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Common;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class AuthorizationTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    [Fact]
    public async Task Authorization_FilterReturns403()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Readers?$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: services =>
            {
                ConfigureServices(services);
                services.AddSingleton<IChainedService<IQueryExpressionAuthorizer>, DisallowEverythingAuthorizer>();
            });
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authorization_UpdateEmployee_ShouldReturn400()
    {
        var settings = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        settings.Converters.Add(new SystemTextJsonTimeSpanConverter());
        settings.Converters.Add(new SystemTextJsonTimeOfDayConverter());

        Action<IServiceCollection> services = serviceCollection =>
        {
            ConfigureServices(serviceCollection);
        };

        var employeeResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Readers?$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            jsonSerializerSettings: settings,
            serviceCollection: services);

        _ = await TraceListener.LogAndReturnMessageContentAsync(employeeResponse);

        employeeResponse.IsSuccessStatusCode.Should().BeTrue();

        var employeeResult = await employeeResponse.DeserializeResponseAsync<ODataV4List<Employee>>(settings);
        var employeeList = employeeResult.Response;
        var errorContent = employeeResult.ErrorContent;
        employeeList.Should().NotBeNull();
        employeeList.Items.Should().NotBeNullOrEmpty();
        errorContent.Should().BeNullOrEmpty();

        var employee = employeeList.Items.First();
        employee.Should().NotBeNull();

        employee.FullName += " Can't Update";

        var employeeEditResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Put,
            resource: $"/Readers({employee.Id})",
            payload: employee,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            jsonSerializerSettings: settings,
            serviceCollection: services);
        _ = await TraceListener.LogAndReturnMessageContentAsync(employeeEditResponse);

        employeeEditResponse.IsSuccessStatusCode.Should().BeFalse();
        employeeEditResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
