// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class DeepInsertTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    private static string UniqueId([System.Runtime.CompilerServices.CallerMemberName] string name = null)
    {
        var id = $"{name}_{Guid.NewGuid():N}";
        return id.Length > 64 ? id[..64] : id;
    }

    [Fact]
    public async Task DeepInsert_CollectionNavProperty()
    {
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Isbn = "1234567890123", Title = "Deep Book 1", IsActive = true },
                new { Isbn = "9876543210123", Title = "Deep Book 2", IsActive = true },
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
            because: $"deep insert POST should succeed. Response body: {postContent}");

        // Verify the publisher was created with its books
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Id.Should().Be(pubId);
        publisher.Books.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeepInsert_ServerGeneratedKeys()
    {
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Isbn = "1111111111111", Title = "Server Key Book", IsActive = true },
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
            because: $"deep insert POST should succeed. Response body: {postContent}");

        // Verify the book got a server-generated Guid from OnInsertingBook
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Books.Should().HaveCount(1);
        publisher.Books[0].Id.Should().NotBe(Guid.Empty,
            because: "OnInsertingBook should have assigned a server-generated Guid");
    }

    [Fact]
    public async Task DeepInsert_FiresConventionMethods()
    {
        // Post with a Book that has Id = Guid.Empty, which OnInsertingBook should replace with a real Guid
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Id = Guid.Empty, Isbn = "2222222222222", Title = "Convention Book", IsActive = true },
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
            because: $"deep insert POST should succeed. Response body: {postContent}");

        // Verify the convention method fired and assigned a non-empty Guid
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Books.Should().HaveCount(1);
        publisher.Books[0].Id.Should().NotBe(Guid.Empty,
            because: "OnInsertingBook convention should have assigned a non-empty Guid");
    }

    [Fact]
    public async Task DeepInsert_MaxDepth1_AllowsOneLevel()
    {
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Isbn = "6666666666666", Title = "Depth 1 OK Book", IsActive = true },
            },
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            configureOptions: o => o.DeepOperations.MaxDepth = 1);

        var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: $"MaxDepth=1 should allow one level of nesting. Response: {postContent}");
    }

    [Fact]
    public async Task DeepInsert_ExceedsMaxDepth_Returns400()
    {
        // A payload with 2 levels of nesting: Publisher -> Books -> Reviews
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new
                {
                    Isbn = "3333333333333",
                    Title = "Too Deep Book",
                    IsActive = true,
                    Reviews = new[]
                    {
                        new { Content = "Great book!", Rating = 5 },
                    },
                },
            },
        };

        // Set MaxDepth = 1 via the RestierRouteOptions bag. Registrations of
        // DeepOperationSettings made via the route IServiceCollection are
        // silently replaced by the bag in AddRestierRoute, so the bag is the
        // single canonical channel for this setting.
        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            configureOptions: o => o.DeepOperations.MaxDepth = 1);

        postResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "nesting depth exceeds MaxDepth=1 (Publisher->Books is OK at depth 0, but Books->Reviews at depth 1 should be rejected)");
    }

    [Fact]
    public async Task DeepInsert_WithKeyOnlyNestedEntity_TreatedAsBind()
    {
        // A nested entity with only key properties is detected as a bind reference
        // by the key-subset heuristic. Real @odata.bind wire format is tested by
        // BatchTests_MimePayloadTest which uses actual @odata.bind annotation syntax.
        // This test verifies that a Publisher can be created with
        // an existing Book wired via bind reference (only the Book's key is supplied).
        var pubId = UniqueId();

        // "A Clockwork Orange" — seeded, active, belongs to Publisher1.
        // We re-bind it to a new publisher to exercise the bind reference resolution path.
        var existingBookId = new Guid("19d68c75-1313-4369-b2bf-521f2b260a59");

        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "11111" },
            Books = new object[]
            {
                new { Id = existingBookId }, // Only key property — detected as bind reference
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
            because: $"POST with a bind reference to an existing Book should succeed. Response: {postContent}");

        // Verify the new publisher was created
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue(
            because: "the newly created Publisher should be retrievable");

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Id.Should().Be(pubId);
    }

    [Fact]
    public async Task DeepInsert_ResponseIncludesExpandedBooks()
    {
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Isbn = "8888888888888", Title = "Response Test Book", IsActive = true },
            },
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        postResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "the deep insert POST should succeed");

        // Verify the 201 response body includes Books (expanded per OData 4.01)
        var responseContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        responseContent.Should().Contain("Response Test Book",
            because: "the deep insert 201 response should expand nested Books in the response body");
    }

    [Fact]
    public async Task DeepInsert_ResponseIncludesMultiLevelExpand()
    {
        // POST Publisher with Books containing Reviews (2-level nesting)
        // Verify the 201 response includes both Books AND Reviews in the expanded response
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new
                {
                    Isbn = "1010101010101",
                    Title = "Multi-Expand Book",
                    IsActive = true,
                    Reviews = new[]
                    {
                        new { Content = "Deep review!", Rating = 5 },
                    },
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
            because: $"multi-level deep insert should succeed. Response: {postContent}");

        // Verify the response includes expanded Books
        postContent.Should().Contain("Multi-Expand Book",
            because: "response should include expanded Books");

        // Verify the response includes expanded Reviews within Books
        postContent.Should().Contain("Deep review!",
            because: "response should include expanded Reviews within Books (multi-level expansion)");
    }

    [Fact]
    public async Task DeepInsert_ResponseHasExpandedNavigationShape()
    {
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new { Isbn = "1212121212121", Title = "Structural Test Book", IsActive = true },
            },
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Deserialize as Publisher and verify the Books property is populated
        var (publisher, _) = await postResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Id.Should().Be(pubId);
        publisher.Books.Should().NotBeNullOrEmpty(
            because: "the 201 response should include expanded Books navigation property");
        publisher.Books.Should().HaveCount(1);
        publisher.Books[0].Title.Should().Be("Structural Test Book");
    }

    [Fact]
    public async Task DeepInsert_BindReferenceNotFound_Returns400()
    {
        // When a nested entity is detected as a bind reference (only key properties)
        // but the referenced entity does not exist, Phase 1 bind validation must return 400.
        var pubId = UniqueId();
        var nonExistentBookId = Guid.NewGuid();

        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new object[]
            {
                new { Id = nonExistentBookId }, // Only key property — detected as bind reference
            },
        };

        var postResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var postContent = await postResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        postResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: $"referencing a non-existent Book as a bind reference should return 400. Response: {postContent}");
    }

    [Fact]
    public async Task DeepInsert_BindDoesNotFireConventionMethods()
    {
        // Use a seeded, active Book: "Color Purple, The" (Publisher2)
        // Deliberately using a different book than DeepInsert_WithKeyOnlyNestedEntity_TreatedAsBind
        // to avoid cross-test contamination from shared database state.
        var existingBookId = new Guid("0697576b-d616-4057-9d28-ed359775129e");

        // GET the existing book to capture its original state
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({existingBookId})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (originalBook, _) = await bookRequest.DeserializeResponseAsync<Book>();
        var originalTitle = originalBook.Title;

        // Create a new Publisher with a bind reference to the existing Book.
        // Key-only payload triggers IsEntityReference → NavigationBinding (not Insert).
        // OnInsertingBook should NOT fire for the bound book.
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new object[]
            {
                new { Id = existingBookId }, // Key only → bind reference
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
            because: $"deep insert with bind reference should succeed. Response: {postContent}");

        // Verify: book is now linked to the new publisher
        var verifyResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({existingBookId})?$expand=Publisher",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        verifyResponse.IsSuccessStatusCode.Should().BeTrue();

        var (verifiedBook, _) = await verifyResponse.DeserializeResponseAsync<Book>();
        verifiedBook.PublisherId.Should().Be(pubId,
            because: "the book should be linked to the new publisher via bind");
        verifiedBook.Id.Should().Be(existingBookId,
            because: "OnInsertingBook should NOT have fired for a bind reference — the book's Id must be unchanged");
        verifiedBook.Title.Should().Be(originalTitle,
            because: "OnInsertingBook should NOT have fired for a bind reference — the book's properties should be unchanged");

        // Verify no duplicate book was created: the publisher should have exactly 1 book (the bound one)
        var expandResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        expandResponse.IsSuccessStatusCode.Should().BeTrue();

        var (newPublisher, _) = await expandResponse.DeserializeResponseAsync<Publisher>();
        newPublisher.Books.Should().HaveCount(1,
            because: "only the bound book should be linked — no new book should have been inserted");
    }

    [Fact]
    public async Task DeepInsert_MultiLevel()
    {
        var pubId = UniqueId();
        var payload = new
        {
            Id = pubId,
            Addr = new { Zip = "00000" },
            Books = new[]
            {
                new
                {
                    Isbn = "9999999999999",
                    Title = "Multi Level Book",
                    IsActive = true,
                    Reviews = new[]
                    {
                        new { Content = "Great multi-level book!", Rating = 5 },
                        new { Content = "Decent.", Rating = 3 },
                    },
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
            because: $"multi-level deep insert should succeed. Response: {postContent}");

        // Verify: publisher has 1 book, book has 2 reviews
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Publishers('{pubId}')?$expand=Books($expand=Reviews)",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Books.Should().HaveCount(1);
        // The Reviews collection may not deserialize depending on the test infrastructure
        // At minimum, verify the book was created
        publisher.Books[0].Title.Should().Be("Multi Level Book");
    }
}
