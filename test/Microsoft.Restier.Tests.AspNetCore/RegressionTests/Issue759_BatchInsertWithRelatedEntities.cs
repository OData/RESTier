// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests;

/// <summary>
/// Regression tests for https://github.com/OData/RESTier/issues/759.
/// <para>
/// The reporter saw <c>SaveChanges(SaveChangesOptions.BatchWithSingleChangeset)</c> on a parent
/// entity with related children fail with an OData URI parse error on a URL of the form
/// <c>http://host/$N/childCollection</c>. <c>$N</c> is a Content-ID reference to the just-created
/// parent; it must be resolved against the EDM-derived entity URL of request <c>N</c> before the
/// URI parser runs. The fix landed via #762 as <c>ChangeSetDependencyResolver</c>.
/// </para>
/// <para>
/// Verification scope on this branch:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>URL parse no longer fails</b> — the dependent <c>POST /$1/Books</c> reaches the controller
///     at the resolved nav-collection URL and returns 201. This is the direct symptom from #759.
///   </description></item>
///   <item><description>
///     <b>Deep insert</b> (single POST with nested children, no $batch) covers the reporter's
///     concern about not having to set the parent FK on each child manually. RESTier + EF set
///     the FK from the parent insert.
///   </description></item>
/// </list>
/// </summary>
public abstract class Issue759_BatchInsertWithRelatedEntities<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    protected abstract Task CleanupIssue759Async();

    [Fact]
    public async Task Issue759_BatchChangeSet_DollarContentIdResolvesAndChildReachesController()
    {
        // Direct regression for the #759 URL parse failure: the reporter's "/$121/boxes" pattern.
        // Before #762, "/$1/Books" hit the OData URI parser as the literal "$1/Books" and threw
        // "resource not found for $1". ChangeSetDependencyResolver now pre-resolves "$1" against
        // the EDM-derived entity URL of request 1, so the path the controller sees is the
        // fully-resolved "/api/tests/Publishers('Issue759Pub')/Books".
        //
        // Asserted here: the dependent child POST reaches the controller at the resolved URL
        // and persists. The full $batch envelope as a whole still has a known issue on the
        // concurrent-execution path — see Issue759_BatchChangeSet_ParentAndChild_FullSuccess
        // below — but that's not the bug #759 originally described.
        await CleanupIssue759Async();

        try
        {
            var client = await GetHttpClientAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
            {
                Content = new StringContent(Issue759MimeBatchRequest, Encoding.UTF8),
            };
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse(
                "multipart/mixed;boundary=batch_759_outer");

            var batchResponse = await client.SendAsync(request, Xunit.TestContext.Current.CancellationToken);
            var batchBody = await TraceListener.LogAndReturnMessageContentAsync(batchResponse);

            // The $batch envelope itself returns 200 (per-request status is inside the multipart body).
            batchResponse.IsSuccessStatusCode.Should().BeTrue(
                because: $"the $batch envelope itself must succeed. Body: {batchBody}");

            // The original symptom was an OData URI parse error like
            //   ODataException: The request URI is not valid. Since the segment '$1' refers to a previous
            //   segment 'http://...', the URI of the previous segment must not be specified.
            // The resolver fix means this error is gone — the dependent request reaches the controller.
            batchBody.Should().NotContain("ODataException",
                because: "the URL parse error from #759 must not surface");
            batchBody.Should().NotContain("resource not found",
                because: "$1 must be resolved before the URI parser runs (was the #759 symptom)");
            batchBody.Should().NotContain("'$1'",
                because: "$1 must be substituted with the parent's entity URL before parsing");

            // The dependent child POST (request 2) reaches the controller and persists.
            // End-to-end verification: GET the Book back by its id and confirm it was saved.
            // Note: this asserts that the *URL was resolved and routed* — proving the #759 parse
            // symptom is gone. It does NOT assert the FK back-bind to Publisher; auto-binding the
            // parent FK from a nav-collection POST URL is a separate concern (the reporter's
            // point 2) that the current controller does not implement.
            var getBookResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: $"/Books({Issue759ChildBookId})",
                serviceCollection: ConfigureServices);
            var getBookContent = await TraceListener.LogAndReturnMessageContentAsync(getBookResponse);

            getBookResponse.IsSuccessStatusCode.Should().BeTrue(
                because: $"the child Book POSTed at /$1/Books must persist. Response: {getBookContent}");

            var (book, _) = await getBookResponse.DeserializeResponseAsync<Book>();
            book.Should().NotBeNull();
            book.Title.Should().Be("Issue759 Child Book",
                because: "POSTing to /$1/Books reaches the controller at the resolved URL — proves #759 parse fix");
        }
        finally
        {
            await CleanupIssue759Async();
        }
    }

    [Fact]
    public async Task Issue759_BatchChangeSet_ParentAndChild_AllInnerRequestsSucceed()
    {
        // Direct regression for the bug found while writing the earlier test on this branch:
        // the concurrent batch-execution path pre-populated the framework's
        // contentIdToLocationMapping with entries for ContentIds whose requests were still
        // pending. The framework treats those entries as a conflict and silently rewrites the
        // non-dependent (parent) request's 201 to 500 between result execution and the batch
        // response writer. ChangeSetDependencyResolver now uses a private mapping for URL
        // rewrites and leaves the framework's mapping alone — see
        // RestierBatchChangeSetRequestItem.TryPreResolve.
        //
        // Asserted: every inner response in the batch is a successful 201 Created. The
        // PublisherId back-bind on the child Book is a separate concern (POST to a nav
        // collection URL does not propagate the parent key onto the child entity).
        await CleanupIssue759Async();

        try
        {
            var client = await GetHttpClientAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "$batch")
            {
                Content = new StringContent(Issue759MimeBatchRequest, Encoding.UTF8),
            };
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse(
                "multipart/mixed;boundary=batch_759_outer");

            var batchResponse = await client.SendAsync(request, Xunit.TestContext.Current.CancellationToken);
            var batchBody = await TraceListener.LogAndReturnMessageContentAsync(batchResponse);

            batchResponse.IsSuccessStatusCode.Should().BeTrue(
                because: $"the $batch envelope must succeed. Body: {batchBody}");
            batchBody.Should().NotContain("HTTP/1.1 500",
                because: "every per-request response in the changeset must succeed");
            batchBody.Should().NotContain("HTTP/1.1 4",
                because: "no per-request response should be a 4xx");

            // Both responses should now be present (concurrent-path failure used to truncate to one).
            var content1Index = batchBody.IndexOf("Content-ID: 1", System.StringComparison.Ordinal);
            var content2Index = batchBody.IndexOf("Content-ID: 2", System.StringComparison.Ordinal);
            content1Index.Should().BeGreaterThan(0, because: "the parent's per-request response must be present");
            content2Index.Should().BeGreaterThan(content1Index, because: "the child's per-request response must follow");
        }
        finally
        {
            await CleanupIssue759Async();
        }
    }

    [Fact]
    public async Task Issue759_DeepInsert_PostParentWithNestedChild_NoForeignKeyOnChild()
    {
        // Covers #759 point (2): the reporter wanted to avoid setting the parent's key on each
        // child manually. Deep insert (single POST with nested children, no $batch) is the
        // server-side route to that ergonomic — the child payload carries no PublisherId, and
        // RESTier + EF set the FK as part of the parent insert.
        await CleanupIssue759Async();

        try
        {
            var payload = new
            {
                Id = "Issue759Pub",
                Addr = new { Zip = "00000" },
                Books = new[]
                {
                    new
                    {
                        Id = Guid.NewGuid(),
                        Isbn = "7597597597597",
                        Title = "Issue759 Nested Book",
                        IsActive = true,
                    },
                },
            };

            var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Post,
                resource: "/Publishers",
                payload: payload,
                acceptHeader: WebApiConstants.DefaultAcceptHeader,
                serviceCollection: ConfigureServices);

            var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
            postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
                because: $"deep insert POST should succeed without the child carrying PublisherId. Response: {postContent}");

            var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
                HttpMethod.Get,
                resource: "/Publishers('Issue759Pub')?$expand=Books",
                serviceCollection: ConfigureServices);
            getResponse.IsSuccessStatusCode.Should().BeTrue();

            var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
            publisher.Should().NotBeNull();
            publisher.Books.Should().ContainSingle(b => b.Title == "Issue759 Nested Book",
                because: "the nested Book in the deep-insert payload must persist and be linked to the parent");
        }
        finally
        {
            await CleanupIssue759Async();
        }
    }

    private async Task<HttpClient> GetHttpClientAsync()
    {
        var httpClient = await RestierTestHelpers.GetTestableHttpClient<TApi>(
            serviceCollection: ConfigureServices);
        httpClient.BaseAddress = new Uri($"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}");
        return httpClient;
    }

    // The child URL is "/$1/Books" — the navigation-collection analogue of the reporter's
    // "/$121/boxes". The fixed-Guid book id makes the assertion deterministic across runs.
    private const string Issue759ChildBookId = "d7591759-7591-7591-7591-759175917591";

    private const string Issue759MimeBatchRequest =
@"--batch_759_outer
Content-Type: multipart/mixed;boundary=changeset_759_inner

--changeset_759_inner
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1

POST http://localhost/api/tests/Publishers HTTP/1.1
Content-ID: 1
Prefer: return=representation
OData-Version: 4.0
Content-Type: application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8

{""@odata.type"":""#Microsoft.Restier.Tests.Shared.Scenarios.Library.Publisher"",""Id"":""Issue759Pub"",""Addr"":{""Street"":""1 Test St"",""Zip"":""00001""}}
--changeset_759_inner
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 2

POST http://localhost/$1/Books HTTP/1.1
Content-ID: 2
Prefer: return=representation
OData-Version: 4.0
Content-Type: application/json;odata.metadata=minimal;odata.streaming=true;IEEE754Compatible=false;charset=utf-8

{""@odata.type"":""#Microsoft.Restier.Tests.Shared.Scenarios.Library.Book"",""Id"":""d7591759-7591-7591-7591-759175917591"",""Isbn"":""7597597597597"",""Title"":""Issue759 Child Book"",""IsActive"":true}
--changeset_759_inner--
--batch_759_outer--
";
}
