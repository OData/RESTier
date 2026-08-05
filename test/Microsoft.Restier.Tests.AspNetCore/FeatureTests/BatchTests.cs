// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class BatchTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    protected abstract Task CleanupBatchBooksAsync();

    protected abstract Task CleanupBatchBindAsync();

    [TestMethod]
    public async Task BatchTests_AddMultipleEntries()
    {
        await CleanupBatchBooksAsync();

        try
        {
            var client = await GetHttpClientAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
            {
                Content = new StringContent(MimeBatchRequest, Encoding.UTF8),
            };
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("multipart/mixed;boundary=batch_2e6281b5-fc5f-47c1-9692-5ad43fa6088b");

            var batchResponse = await client.SendAsync(request, TestContext.CancellationTokenSource.Token);
            _ = await TraceListener.LogAndReturnMessageContentAsync(batchResponse);
            batchResponse.IsSuccessStatusCode.Should().BeTrue();

            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: "/Books?$expand=Publisher",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("1111111111111");
            content.Should().Contain("2222222222222");
        }
        finally
        {
            await CleanupBatchBooksAsync();
        }
    }

    [TestMethod]
    public async Task BatchTests_MimePayloadTest()
    {
        await CleanupBatchBooksAsync();

        try
        {
            var client = await GetHttpClientAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
            {
                Content = new StringContent(MimeBatchRequest, Encoding.UTF8),
            };
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("multipart/mixed;boundary=batch_2e6281b5-fc5f-47c1-9692-5ad43fa6088b");

            var response = await client.SendAsync(request, TestContext.CancellationTokenSource.Token);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            // Normalize line endings on both sides: MIME responses use CRLF; verbatim string
            // constants take whatever line endings the source file was checked out with (CRLF
            // on Windows, LF or CRLF on Unix depending on core.autocrlf).
            var normalizedContent = content.Replace("\r\n", Environment.NewLine);
            normalizedContent.Should().Contain(BatchResponse1.Replace("\r\n", Environment.NewLine));
            normalizedContent.Should().Contain(BatchResponse2.Replace("\r\n", Environment.NewLine));
        }
        finally
        {
            await CleanupBatchBooksAsync();
        }
    }

    [TestMethod]
    public async Task BatchTests_JsonPayloadTest()
    {
        await CleanupBatchBooksAsync();

        try
        {
            var client = await GetHttpClientAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
            {
                Content = new StringContent(JsonBatchRequest, Encoding.UTF8),
            };
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("application/json");

            var response = await client.SendAsync(request, TestContext.CancellationTokenSource.Token);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Be(JsonBatchResponse);
        }
        finally
        {
            await CleanupBatchBooksAsync();
        }
    }

    [TestMethod]
    public async Task BatchTests_SelectPlusFunctionResult()
    {
        var client = await GetHttpClientAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
        {
            Content = new StringContent(SelectPlusFunctionBatchRequest, Encoding.UTF8),
        };
        request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("application/json");

        var response = await client.SendAsync(request, TestContext.CancellationTokenSource.Token);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("Publisher1");
        content.Should().Contain("The Cat in the Hat");
    }

    [TestMethod]
    public async Task BatchTests_CollectionBindToExistingBook()
    {
        // Regression coverage for OData/RESTier#663: a $batch changeset containing a POST whose
        // body uses the OData 4.0 collection-valued @odata.bind syntax (an array of entity URLs)
        // must link the referenced existing entities to the newly-created parent. This is the
        // many-to-many shape from the issue payload, modelled here as Publisher 1->* Book.
        await CleanupBatchBindAsync();

        try
        {
            var client = await GetHttpClientAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
            {
                Content = new StringContent(CollectionBindBatchRequest, Encoding.UTF8),
            };
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("multipart/mixed;boundary=batch_3f83a52d-e1bc-4dca-b1f0-c14b35cce0df");

            var batchResponse = await client.SendAsync(request, TestContext.CancellationTokenSource.Token);
            var batchBody = await TraceListener.LogAndReturnMessageContentAsync(batchResponse);
            batchResponse.IsSuccessStatusCode.Should().BeTrue(
                because: $"the batched POST with Books@odata.bind should succeed. Body: {batchBody}");

            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: "/Publishers('BatchBindPub')?$expand=Books",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Sea of Rust",
                because: "the existing Book referenced via the Books@odata.bind array should now be linked to the new Publisher");
            content.Should().Contain("2d760f15-974d-4556-8cdf-d610128b537e",
                because: "the expanded Books collection on the new Publisher should include the bound Book by id");
        }
        finally
        {
            await CleanupBatchBindAsync();
        }
    }

    private async Task<HttpClient> GetHttpClientAsync()
    {
        var httpClient = await RestierTestHelpers.GetTestableHttpClient<TApi>(
            serviceCollection: ConfigureServices);
        httpClient.BaseAddress = new Uri($"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}");
        return httpClient;
    }

    private const string MimeBatchRequest =
@"--batch_2e6281b5-fc5f-47c1-9692-5ad43fa6088b
Content-Type: multipart/mixed;boundary=changeset_ee671721-3d96-462d-ac58-67530e4b530c

--changeset_ee671721-3d96-462d-ac58-67530e4b530c
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1

POST http://localhost/api/tests/Books HTTP/1.1
Content-ID: 1
Prefer: return=representation
OData-Version: 4.0
Content-Type: application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8

{""@odata.type"":""#Microsoft.Restier.Tests.Shared.Scenarios.Library.Book"",""Id"":""79874b37-ce46-4f4c-aa74-8e02ce4d8b67"",""Isbn"":""1111111111111"",""Title"":""Batch Test #1"",""IsActive"":true,""Publisher@odata.bind"":""http://localhost/api/tests/Publishers(%27Publisher1%27)""}
--changeset_ee671721-3d96-462d-ac58-67530e4b530c
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 2

POST http://localhost/api/tests/Books HTTP/1.1
Content-ID: 2
Prefer: return=representation
OData-Version: 4.0
Content-Type: application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8

{""@odata.type"":""#Microsoft.Restier.Tests.Shared.Scenarios.Library.Book"",""Id"":""c6b67ec7-badc-45c6-98c7-c76b570ce694"",""Isbn"":""2222222222222"",""Title"":""Batch Test #2"",""IsActive"":true,""Publisher@odata.bind"":""http://localhost/api/tests/Publishers(%27Publisher1%27)""}
--changeset_ee671721-3d96-462d-ac58-67530e4b530c--
--batch_2e6281b5-fc5f-47c1-9692-5ad43fa6088b--
";

    private const string BatchResponse1 =
@"Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1

HTTP/1.1 201 Created
Location: http://localhost/api/tests/Books(79874b37-ce46-4f4c-aa74-8e02ce4d8b67)
Content-Type: application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8
OData-Version: 4.0

{""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",""Id"":""79874b37-ce46-4f4c-aa74-8e02ce4d8b67"",""Isbn"":""1111111111111"",""Title"":""Batch Test #1"",""PublisherId"":""Publisher1"",""IsActive"":true,""Category"":null,""PublishDate"":null}
";

    private const string BatchResponse2 =
@"Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 2

HTTP/1.1 201 Created
Location: http://localhost/api/tests/Books(c6b67ec7-badc-45c6-98c7-c76b570ce694)
Content-Type: application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8
OData-Version: 4.0

{""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",""Id"":""c6b67ec7-badc-45c6-98c7-c76b570ce694"",""Isbn"":""2222222222222"",""Title"":""Batch Test #2"",""PublisherId"":""Publisher1"",""IsActive"":true,""Category"":null,""PublishDate"":null}
";

    private const string JsonBatchRequest = @"
        {
            ""requests"": [{
                    ""id"": ""1"",
                    ""method"": ""POST"",
                    ""url"": ""http://localhost/api/tests/Books"",
                    ""headers"": {
                        ""OData-Version"": ""4.0"",
                        ""Content-Type"": ""application/json;odata.metadata=minimal"",
                        ""Accept"": ""application/json;odata.metadata=minimal""
                    },
                    ""body"": {
                        ""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",
                        ""Id"":""79874b37-ce46-4f4c-aa74-8e02ce4d8b67"",
                        ""Isbn"":""1111111111111"",
                        ""Title"":""Batch Test #1"",
                        ""IsActive"":true
                    }
                }, {
                    ""id"": ""2"",
                    ""method"": ""POST"",
                    ""url"": ""http://localhost/api/tests/Books"",
                    ""headers"": {
                        ""OData-Version"": ""4.0"",
                        ""Content-Type"": ""application/json;odata.metadata=minimal"",
                        ""Accept"": ""application/json;odata.metadata=minimal""
                    },
                    ""body"": {
                        ""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",
                        ""Id"":""c6b67ec7-badc-45c6-98c7-c76b570ce694"",
                        ""Isbn"":""2222222222222"",
                        ""Title"":""Batch Test #2"",
                        ""IsActive"":true
                    }
                }
            ]
        }";

    private const string JsonBatchResponse = @"{""responses"":[{""id"":""1"",""status"":201,""headers"":{""location"":""http://localhost/api/tests/Books(79874b37-ce46-4f4c-aa74-8e02ce4d8b67)"",""content-type"":""application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8"",""odata-version"":""4.0""}, ""body"" :{""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",""Id"":""79874b37-ce46-4f4c-aa74-8e02ce4d8b67"",""Isbn"":""1111111111111"",""Title"":""Batch Test #1"",""PublisherId"":null,""IsActive"":true,""Category"":null,""PublishDate"":null}},{""id"":""2"",""status"":201,""headers"":{""location"":""http://localhost/api/tests/Books(c6b67ec7-badc-45c6-98c7-c76b570ce694)"",""content-type"":""application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8"",""odata-version"":""4.0""}, ""body"" :{""@odata.context"":""http://localhost/api/tests/$metadata#Books/$entity"",""Id"":""c6b67ec7-badc-45c6-98c7-c76b570ce694"",""Isbn"":""2222222222222"",""Title"":""Batch Test #2"",""PublisherId"":null,""IsActive"":true,""Category"":null,""PublishDate"":null}}]}";


    private const string CollectionBindBatchRequest =
@"--batch_3f83a52d-e1bc-4dca-b1f0-c14b35cce0df
Content-Type: multipart/mixed;boundary=changeset_d7b30121-ab21-4cf6-9d2e-1f44ad57a96e

--changeset_d7b30121-ab21-4cf6-9d2e-1f44ad57a96e
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1

POST http://localhost/api/tests/Publishers HTTP/1.1
Content-ID: 1
Prefer: return=representation
OData-Version: 4.0
Content-Type: application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8

{""@odata.type"":""#Microsoft.Restier.Tests.Shared.Scenarios.Library.Publisher"",""Id"":""BatchBindPub"",""Addr"":{""Street"":""1 Test St"",""Zip"":""00001""},""Books@odata.bind"":[""http://localhost/api/tests/Books(2d760f15-974d-4556-8cdf-d610128b537e)""]}
--changeset_d7b30121-ab21-4cf6-9d2e-1f44ad57a96e--
--batch_3f83a52d-e1bc-4dca-b1f0-c14b35cce0df--
";

    private const string SelectPlusFunctionBatchRequest = @"
        {
            ""requests"": [{
                    ""id"": ""1"",
                    ""method"": ""GET"",
                    ""url"": ""http://localhost/api/tests/Publishers('Publisher1')"",
                    ""headers"": {
                        ""OData-Version"": ""4.0"",
                        ""Accept"": ""application/json;odata.metadata=minimal""
                    }
                }, {
                    ""id"": ""2"",
                    ""method"": ""GET"",
                    ""url"": ""http://localhost/api/tests/PublishBook(IsActive=true)"",
                    ""headers"": {
                        ""OData-Version"": ""4.0"",
                        ""Accept"": ""application/json;odata.metadata=minimal""
                    }
                }
            ]
        }";
}
