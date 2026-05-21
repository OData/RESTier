// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#pragma warning disable xUnit1051 // CancellationToken not passed to async methods — acceptable in integration tests

using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

/// <summary>
/// Integration tests for <see cref="RestierNamingConvention.LowerCamelCase"/> support.
/// Uses /Readers (no OnFilter convention) for GET assertions.
/// Write tests verify the immediate POST/PATCH/PUT response (data doesn't persist
/// between requests in the test infrastructure's per-server in-memory DB).
/// </summary>
public abstract class NamingConventionTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    private static readonly JsonSerializerOptions CamelCaseDeserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Sends a raw JSON request to a camelCase-configured server.
    /// </summary>
    private HttpClient CreateCamelCaseClient()
    {
        var server = RestierTestHelpers.GetTestableRestierServer<TApi>(
            apiServiceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        return server.CreateClient();
    }

    private static async Task<HttpResponseMessage> SendJsonAsync(HttpClient client, HttpMethod method, string resource,
        string json = null, string acceptHeader = null)
    {
        using var request = new HttpRequestMessage(method, $"http://localhost/api/tests{resource}");
        request.Headers.Add("Accept", acceptHeader ?? WebApiConstants.DefaultAcceptHeader);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        return await client.SendAsync(request);
    }

    #region GET / Query

    [TestMethod]
    public async Task GetEntitySet_ReturnsCamelCasePropertyNames()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/Readers", serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"fullName\"");
        content.Should().Contain("\"id\"");
        content.Should().NotContain("\"FullName\"");
    }

    [TestMethod]
    public async Task GetMetadata_ShowsCamelCasePropertyNames()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/$metadata", acceptHeader: "application/xml",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("Name=\"title\"");
        content.Should().Contain("Name=\"isbn\"");
        content.Should().Contain("Name=\"isActive\"");
        content.Should().Contain("Name=\"fullName\"");
    }

    [TestMethod]
    public async Task GetWithSelect_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/Readers?$select=fullName",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"fullName\"");
    }

    [TestMethod]
    public async Task GetWithFilter_WorksWithCamelCase()
    {
        // Test that $filter with camelCase property names returns 200 (not 400 Bad Request).
        // Don't assert on data content since the in-memory DB may or may not be seeded.
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/Readers?$filter=fullName eq 'p1'",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetWithExpand_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/Publishers?$expand=books",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"books\"");
    }

    [TestMethod]
    public async Task GetWithOrderBy_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/Readers?$orderby=fullName",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    #endregion

    #region POST creates entity with camelCase properties

    [TestMethod]
    public async Task PostBook_WithCamelCasePayload_CreatesEntity()
    {
        using var client = CreateCamelCaseClient();
        var response = await SendJsonAsync(client, HttpMethod.Post, "/Publishers('Publisher1')/Books",
            json: """{"title":"CamelCase Insert Test","isbn":"0118006345789"}""");
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"POST failed: {content}");
        content.Should().Contain("\"title\"");
        content.Should().Contain("CamelCase Insert Test");
        content.Should().Contain("\"isbn\"");
        content.Should().Contain("0118006345789");
    }

    [TestMethod]
    public async Task PatchPublisher_WithCamelCasePayload_Succeeds()
    {
        // PATCH against a seeded publisher with a camelCase property change
        using var client = CreateCamelCaseClient();
        var patchResponse = await SendJsonAsync(client, HttpMethod.Patch,
            "/Publishers('Publisher1')",
            json: """{"id":"Publisher1"}""");
        var content = await patchResponse.Content.ReadAsStringAsync();
        patchResponse.IsSuccessStatusCode.Should().BeTrue($"PATCH failed ({patchResponse.StatusCode}): {content}");
    }

    [TestMethod]
    public async Task PutPublisher_WithCamelCasePayload_Succeeds()
    {
        // PUT against a seeded publisher
        using var client = CreateCamelCaseClient();
        var putJson = """{"id":"Publisher1"}""";
        var putResponse = await SendJsonAsync(client, HttpMethod.Put,
            "/Publishers('Publisher1')",
            json: putJson);
        var content = await putResponse.Content.ReadAsStringAsync();
        putResponse.IsSuccessStatusCode.Should().BeTrue($"PUT failed ({putResponse.StatusCode}): {content}");
    }

    #endregion

    #region Key Handling

    [TestMethod]
    public async Task GetByKey_WorksWithCamelCase()
    {
        // Use a LibraryCard key (seeded with a known GUID, no OnFilter convention)
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/LibraryCards(a1111111-1111-1111-1111-111111111111)",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"dateRegistered\"");
        content.Should().Contain("\"id\"");
    }

    [TestMethod]
    public async Task DeleteLibraryCard_WithCamelCase_Returns428WithoutETag()
    {
        // DELETE without ETag against concurrency-enabled entity returns 428.
        // LibraryCards has [ConcurrencyCheck] so ETag is required.
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Delete, resource: "/LibraryCards(a1111111-1111-1111-1111-111111111111)",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        response.StatusCode.Should().Be((HttpStatusCode)428,
            $"DELETE without ETag should return 428. Got {response.StatusCode}: {await TraceListener.LogAndReturnMessageContentAsync(response)}");
    }

    [TestMethod]
    public async Task PatchPublisher_WithIfMatchETag_WorksWithCamelCase()
    {
        // Test the ETag normalization path: PATCH with If-Match wildcard ETag.
        // Uses a shared server so GET and PATCH hit the same in-memory DB.
        using var client = CreateCamelCaseClient();

        // PATCH with If-Match: * wildcard ETag header
        using var patchRequest = new HttpRequestMessage(new HttpMethod("PATCH"),
            "http://localhost/api/tests/Publishers('Publisher1')")
        {
            Content = new StringContent("""{"id":"Publisher1"}""", Encoding.UTF8, "application/json"),
        };
        patchRequest.Headers.Add("Accept", WebApiConstants.DefaultAcceptHeader);
        patchRequest.Headers.TryAddWithoutValidation("If-Match", "*");
        var patchResponse = await client.SendAsync(patchRequest);
        var patchContent = await TraceListener.LogAndReturnMessageContentAsync(patchResponse);
        patchResponse.IsSuccessStatusCode.Should().BeTrue($"PATCH with ETag failed: {patchContent}");
    }

    #endregion

    #region Concurrency (ETag)

    [TestMethod]
    public async Task GetLibraryCard_WithCamelCase_ReturnsCamelCasePropertyNames()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/LibraryCards(a1111111-1111-1111-1111-111111111111)",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCase);
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        content.Should().Contain("\"dateRegistered\"");
        content.Should().Contain("\"id\"");
    }

    #endregion

    #region Enum Members

    [TestMethod]
    public async Task PostBook_WithCamelCaseEnumValue_CreatesEntity()
    {
        using var client = CreateCamelCaseClient();
        var response = await SendJsonAsync(client, HttpMethod.Post, "/Publishers('Publisher1')/Books",
            json: """{"title":"Enum Test Book","isbn":"5555555555555","category":"fiction"}""");
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue($"Enum POST failed: {content}");
        content.Should().Contain("Enum Test Book");
    }

    [TestMethod]
    public async Task GetMetadata_WithEnumMembers_ShowsCamelCaseEnumValues()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get, resource: "/$metadata", acceptHeader: "application/xml",
            serviceCollection: ConfigureServices, namingConvention: RestierNamingConvention.LowerCamelCaseWithEnumMembers);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("Name=\"fiction\"");
    }

    #endregion
}
