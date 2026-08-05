// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#pragma warning disable xUnit1051 // CancellationToken not passed to async methods — acceptable in integration tests

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.AspNetCore.Scenarios.MagicalOps;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

[TestClass]
public class MagicalOperationsTests
{
    private static System.Action<IServiceCollection> ConfigureServices => services => services.AddTestDefaultServices();

    [TestMethod]
    public async Task Echo_WithNullParameter_ReturnsNoContent()
    {
        // Literal #656 repro: ?parameter1=null on a Nullable<int> parameter must succeed
        // (previously threw NullReferenceException → 500). The function returns null,
        // so Restier emits 204 No Content — that is the correct OData response for a
        // nullable function that evaluates to null.
        var response = await RestierTestHelpers.ExecuteTestRequest<MagicalOpsApi>(
            HttpMethod.Get,
            resource: "/Echo(parameter1=null)",
            serviceCollection: ConfigureServices);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [TestMethod]
    public async Task WithDefault_OmittedParameter_PassesDeclaredDefault()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<MagicalOpsApi>(
            HttpMethod.Get,
            resource: "/WithDefault()",
            serviceCollection: ConfigureServices);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        body.Should().Contain("\"value\":5");
    }

    [TestMethod]
    public async Task NullableWithDefault_ExplicitNull_PassesNull()
    {
        // int? p = 5 is both nullable and optional. Explicit null must beat default.
        // The function returns null → Restier emits 204 No Content.
        var response = await RestierTestHelpers.ExecuteTestRequest<MagicalOpsApi>(
            HttpMethod.Get,
            resource: "/NullableWithDefault(parameter1=null)",
            serviceCollection: ConfigureServices);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [TestMethod]
    public async Task NullableWithDefault_Omitted_PassesDefault()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<MagicalOpsApi>(
            HttpMethod.Get,
            resource: "/NullableWithDefault()",
            serviceCollection: ConfigureServices);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        body.Should().Contain("\"value\":5");
    }

    [TestMethod]
    public async Task Metadata_DeprecatedMethod_EmitsRevisionsAnnotation()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<MagicalOpsApi>(
            serviceCollection: ConfigureServices);
        metadata.Should().NotBeNull();
        var xml = metadata.ToString();
        xml.Should().Contain("Core.V1.Revisions");
        xml.Should().Contain("Use NewMethod instead.");
    }

    [TestMethod]
    public async Task Metadata_DescribedMethod_EmitsDescriptionAnnotation()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<MagicalOpsApi>(
            serviceCollection: ConfigureServices);
        metadata.Should().NotBeNull();
        var xml = metadata.ToString();
        xml.Should().Contain("Core.V1.Description");
        xml.Should().Contain("Returns nothing.");
    }

    [TestMethod]
    public async Task Metadata_UnknownComplexType_IsRegistered()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<MagicalOpsApi>(
            serviceCollection: ConfigureServices);
        metadata.Should().NotBeNull();
        var xml = metadata.ToString();
        xml.Should().Contain("SearchCriteria");
        xml.Should().Contain("SearchResult");
    }
}
