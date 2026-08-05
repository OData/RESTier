// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#pragma warning disable xUnit1051 // CancellationToken not passed to async methods — acceptable in integration tests

using System;
using System.IO;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Annotated;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

[TestClass]
public class AnnotationMetadataTests
{
    private const string RelativePath = "..//..//..//..//Microsoft.Restier.Tests.AspNetCore//";
    private const string BaselineFolder = "Baselines//";

    private static Action<IServiceCollection> BuildServices(string dbName) => services =>
    {
        services.AddEFCoreProviderServices<AnnotatedContext>(options =>
            options.UseInMemoryDatabase(dbName));
    };

    private static Action<IServiceCollection> ConfigureServices => BuildServices($"AnnotationTests-{Guid.NewGuid()}");

    [TestMethod]
    public async Task AnnotatedApi_MetadataMatchesBaseline()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}{nameof(AnnotatedApi)}-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue($"baseline file not found at: {Path.GetFullPath(fileName)}");

        var oldReport = File.ReadAllText(fileName);
        var newReport = await RestierTestHelpers.GetApiMetadataAsync<AnnotatedApi>(
            serviceCollection: ConfigureServices);

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }

    [TestMethod]
    public async Task PostingComputedProperty_ReturnsServerAssignedId()
    {
        // Arrange — POST with Id=9999 in the body. Expect the server to ignore it.
        var services = BuildServices($"PostTest-{Guid.NewGuid()}");
        var payload = new
        {
            Id = 9999,
            Name = "Widget",
            CreatedOn = DateTimeOffset.Parse("2026-05-01T00:00:00Z"),
            Score = 42,
            CountryCode = "US",
        };

        // Act
        var response = await RestierTestHelpers.ExecuteTestRequest<AnnotatedApi>(
            System.Net.Http.HttpMethod.Post,
            resource: "/AnnotatedEntities",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services);

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var idInResponse = doc.RootElement.GetProperty("Id").GetInt32();
        idInResponse.Should().NotBe(9999,
            "Core.V1.Computed should cause Restier to drop the client-supplied Id from the change set");
    }

    [TestMethod]
    public async Task PatchingImmutableProperty_DoesNotChangePersistedValue()
    {
        // Arrange — single stable DB; POST creates a row, PATCH attempts to change CreatedOn,
        // GET confirms the original value persists.
        var services = BuildServices($"PatchTest-{Guid.NewGuid()}");
        var originalCreatedOn = DateTimeOffset.Parse("2026-05-01T00:00:00Z");

        var postPayload = new
        {
            Name = "Widget",
            CreatedOn = originalCreatedOn,
            Score = 42,
            CountryCode = "US",
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<AnnotatedApi>(
            System.Net.Http.HttpMethod.Post,
            resource: "/AnnotatedEntities",
            payload: postPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services);
        postResponse.IsSuccessStatusCode.Should().BeTrue(await postResponse.Content.ReadAsStringAsync());

        using var postDoc = System.Text.Json.JsonDocument.Parse(await postResponse.Content.ReadAsStringAsync());
        var id = postDoc.RootElement.GetProperty("Id").GetInt32();

        // Act — PATCH with a different CreatedOn.
        var patchPayload = new { CreatedOn = DateTimeOffset.Parse("1900-01-01T00:00:00Z") };
        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<AnnotatedApi>(
            System.Net.Http.HttpMethod.Patch,
            resource: $"/AnnotatedEntities({id})",
            payload: patchPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services);
        patchResponse.IsSuccessStatusCode.Should().BeTrue(await patchResponse.Content.ReadAsStringAsync());

        // GET to confirm CreatedOn is unchanged.
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<AnnotatedApi>(
            System.Net.Http.HttpMethod.Get,
            resource: $"/AnnotatedEntities({id})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: services);
        getResponse.IsSuccessStatusCode.Should().BeTrue(await getResponse.Content.ReadAsStringAsync());

        using var getDoc = System.Text.Json.JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        var persistedCreated = DateTimeOffset.Parse(getDoc.RootElement.GetProperty("CreatedOn").GetString());
        persistedCreated.Should().Be(originalCreatedOn,
            "Core.V1.Immutable should cause Restier to drop the PATCH value for CreatedOn");
    }
}
