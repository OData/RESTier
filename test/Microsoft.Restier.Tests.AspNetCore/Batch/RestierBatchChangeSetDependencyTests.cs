// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Batch;

/// <summary>
/// Integration tests for <see cref="RestierBatchChangeSetRequestItem"/> dependency-aware execution.
/// Validates the three strategies: concurrent, pre-resolve + concurrent, and sequential fallback.
/// </summary>
[TestClass]
public class RestierBatchChangeSetDependencyTests
{
    #region Test 1: No Dependencies - Concurrent Execution

    [TestMethod]
    public async Task SendRequestAsync_NoDependencies_ExecutesConcurrently()
    {
        // Arrange
        var api = CreateMockApi();
        var context1 = CreateMockHttpContext("1", "POST", "http://localhost/api/tests/Books");
        var context2 = CreateMockHttpContext("2", "POST", "http://localhost/api/tests/Categories");

        var contexts = new List<HttpContext> { context1, context2 };
        var requestItem = new RestierBatchChangeSetRequestItem(api, contexts);

        var executionLog = new ConcurrentBag<string>();
        var barrier = new Barrier(2);

        RequestDelegate handler = async ctx =>
        {
            var contentId = ctx.Features.Get<IODataBatchFeature>()?.ContentId;

            // Both handlers must reach the barrier simultaneously — if sequential,
            // the barrier will timeout and throw.
            barrier.SignalAndWait(TimeSpan.FromSeconds(5));

            executionLog.Add(contentId);
            ctx.Response.StatusCode = StatusCodes.Status200OK;

            // Mimic controller: signal changeset completion so the batch can finish.
            var changeSet = ctx.GetChangeSet();
            if (changeSet is not null)
            {
                await changeSet.OnChangeSetCompleted().ConfigureAwait(false);
            }
        };

        // Act
        var result = await requestItem.SendRequestAsync(handler);

        // Assert
        executionLog.Should().HaveCount(2);
        executionLog.Should().Contain("1");
        executionLog.Should().Contain("2");
        result.Should().BeOfType<ChangeSetResponseItem>();
    }

    #endregion

    #region Test 2: Dependencies With Client Keys - Pre-Resolve $ContentId

    [TestMethod]
    public async Task SendRequestAsync_WithDependencies_ResolvesDollarContentId()
    {
        // Arrange
        var model = CreateEdmModel();
        var api = CreateMockApi(model);

        var context1 = CreateMockHttpContext(
            "1",
            "POST",
            "http://localhost/api/tests/Books",
            "{\"Id\":\"79874b37-ce46-4f4c-aa74-8e02ce4d8b67\",\"Title\":\"Test\"}");

        var context2 = CreateMockHttpContext(
            "2",
            "PATCH",
            "http://localhost/$1");

        var contexts = new List<HttpContext> { context1, context2 };
        var requestItem = new RestierBatchChangeSetRequestItem(api, contexts);

        string capturedUrlForRequest2 = null;

        RequestDelegate handler = async ctx =>
        {
            var contentId = ctx.Features.Get<IODataBatchFeature>()?.ContentId;

            if (contentId == "2")
            {
                capturedUrlForRequest2 = ctx.Request.GetEncodedUrl();
            }

            ctx.Response.StatusCode = StatusCodes.Status200OK;

            // Signal changeset completion for the concurrent path.
            var changeSet = ctx.GetChangeSet();
            if (changeSet is not null)
            {
                await changeSet.OnChangeSetCompleted().ConfigureAwait(false);
            }
        };

        // Act
        var result = await requestItem.SendRequestAsync(handler);

        // Assert — URL must be a well-formed absolute URL without doubled scheme://host.
        capturedUrlForRequest2.Should().NotBeNull();
        capturedUrlForRequest2.Should().Be("http://localhost/api/tests/Books(79874b37-ce46-4f4c-aa74-8e02ce4d8b67)");
        result.Should().BeOfType<ChangeSetResponseItem>();
    }

    [TestMethod]
    public async Task SendRequestAsync_WithDependencies_ResolvesDollarContentIdWithSuffix()
    {
        // Arrange — request 2 URL is "$1/Details" (with path suffix).
        var model = CreateEdmModel();
        var api = CreateMockApi(model);

        var context1 = CreateMockHttpContext(
            "1",
            "POST",
            "http://localhost/api/tests/Books",
            "{\"Id\":\"79874b37-ce46-4f4c-aa74-8e02ce4d8b67\",\"Title\":\"Test\"}");

        var context2 = CreateMockHttpContext(
            "2",
            "POST",
            "http://localhost/$1/Details");

        var contexts = new List<HttpContext> { context1, context2 };
        var requestItem = new RestierBatchChangeSetRequestItem(api, contexts);

        string capturedUrlForRequest2 = null;

        RequestDelegate handler = async ctx =>
        {
            var contentId = ctx.Features.Get<IODataBatchFeature>()?.ContentId;

            if (contentId == "2")
            {
                capturedUrlForRequest2 = ctx.Request.GetEncodedUrl();
            }

            ctx.Response.StatusCode = StatusCodes.Status200OK;

            var changeSet = ctx.GetChangeSet();
            if (changeSet is not null)
            {
                await changeSet.OnChangeSetCompleted().ConfigureAwait(false);
            }
        };

        // Act
        var result = await requestItem.SendRequestAsync(handler);

        // Assert — suffix "/Details" must be appended after the entity URL.
        capturedUrlForRequest2.Should().NotBeNull();
        capturedUrlForRequest2.Should().Be("http://localhost/api/tests/Books(79874b37-ce46-4f4c-aa74-8e02ce4d8b67)/Details");
        result.Should().BeOfType<ChangeSetResponseItem>();
    }

    #endregion

    #region Test 2b: Chained Dependencies (A→B→C)

    [TestMethod]
    public async Task SendRequestAsync_ChainedDependencies_ResolvesInOrder()
    {
        // Arrange — three requests where C references B which references A.
        var model = CreateEdmModelWithTwoEntitySets();
        var api = CreateMockApi(model);

        // A: POST to Books (no dependencies)
        var contextA = CreateMockHttpContext(
            "1",
            "POST",
            "http://localhost/api/tests/Books",
            "{\"Id\":\"aaaa1111-0000-0000-0000-000000000000\",\"Title\":\"Book A\"}");

        // B: PATCH to $1 (depends on A) — this is also referenced by C
        var contextB = CreateMockHttpContext(
            "2",
            "PATCH",
            "http://localhost/$1");

        // C: DELETE to $2 (depends on B, which depends on A)
        var contextC = CreateMockHttpContext(
            "3",
            "DELETE",
            "http://localhost/$2");

        var contexts = new List<HttpContext> { contextA, contextB, contextC };
        var requestItem = new RestierBatchChangeSetRequestItem(api, contexts);

        var capturedUrls = new ConcurrentDictionary<string, string>();

        RequestDelegate handler = async ctx =>
        {
            var contentId = ctx.Features.Get<IODataBatchFeature>()?.ContentId;
            capturedUrls[contentId] = ctx.Request.GetEncodedUrl();

            ctx.Response.StatusCode = StatusCodes.Status200OK;

            var changeSet = ctx.GetChangeSet();
            if (changeSet is not null)
            {
                await changeSet.OnChangeSetCompleted().ConfigureAwait(false);
            }
        };

        // Act
        var result = await requestItem.SendRequestAsync(handler);

        // Assert — B should resolve $1 → Books(guid), C should resolve $2 → Books(guid)
        // (B is a PATCH so its entity URL is its own resolved URL)
        capturedUrls["2"].Should().Be("http://localhost/api/tests/Books(aaaa1111-0000-0000-0000-000000000000)");
        capturedUrls["3"].Should().Be("http://localhost/api/tests/Books(aaaa1111-0000-0000-0000-000000000000)");
        result.Should().BeOfType<ChangeSetResponseItem>();
    }

    #endregion

    #region Test 3: Server-Generated Key - Sequential Fallback

    [TestMethod]
    public async Task SendRequestAsync_ServerGeneratedKey_FallsBackToSequential()
    {
        // Arrange
        var model = CreateEdmModel();
        var api = CreateMockApi(model);

        // POST with NO key in the body — pre-resolution will fail.
        var context1 = CreateMockHttpContext(
            "1",
            "POST",
            "http://localhost/api/tests/Books",
            "{\"Title\":\"Test\"}");

        var context2 = CreateMockHttpContext(
            "2",
            "PATCH",
            "http://localhost/$1");

        var contexts = new List<HttpContext> { context1, context2 };
        var requestItem = new RestierBatchChangeSetRequestItem(api, contexts);

        var executionOrder = new List<string>();

        RequestDelegate handler = ctx =>
        {
            var contentId = ctx.Features.Get<IODataBatchFeature>()?.ContentId;
            executionOrder.Add(contentId);
            ctx.Response.StatusCode = StatusCodes.Status200OK;

            // In sequential mode, set Location header on request 1 for $ContentId resolution.
            if (contentId == "1")
            {
                ctx.Response.Headers["Location"] = "http://localhost/api/tests/Books(79874b37-ce46-4f4c-aa74-8e02ce4d8b67)";
            }

            return Task.CompletedTask;
        };

        // Act
        var result = await requestItem.SendRequestAsync(handler);

        // Assert — sequential execution means requests run in order.
        executionOrder.Should().Equal("1", "2");
        result.Should().BeOfType<ChangeSetResponseItem>();
        var responseItem = (ChangeSetResponseItem)result;
        responseItem.Contexts.Should().HaveCount(2);
    }

    #endregion

    #region Test 4: Sequential Fallback - Rolls Back on Failure

    [TestMethod]
    public async Task SendRequestAsync_SequentialFallback_RollsBackOnFailure()
    {
        // Arrange
        var model = CreateEdmModel();
        var api = CreateMockApi(model);

        // POST with NO key — pre-resolution fails, falls back to sequential.
        var context1 = CreateMockHttpContext(
            "1",
            "POST",
            "http://localhost/api/tests/Books",
            "{\"Title\":\"Test\"}");

        var context2 = CreateMockHttpContext(
            "2",
            "PATCH",
            "http://localhost/$1");

        var contexts = new List<HttpContext> { context1, context2 };
        var requestItem = new RestierBatchChangeSetRequestItem(api, contexts);

        RequestDelegate handler = ctx =>
        {
            var contentId = ctx.Features.Get<IODataBatchFeature>()?.ContentId;

            if (contentId == "1")
            {
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                ctx.Response.Headers["Location"] = "http://localhost/api/tests/Books(1)";
            }
            else
            {
                // Second request fails.
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            return Task.CompletedTask;
        };

        // Act
        var result = await requestItem.SendRequestAsync(handler);

        // Assert — failure returns a single context with the error status.
        result.Should().BeOfType<ChangeSetResponseItem>();
        var responseItem = (ChangeSetResponseItem)result;
        responseItem.Contexts.Should().ContainSingle();
        responseItem.Contexts.First().Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    #endregion

    #region Test Helpers

    private static ApiBase CreateMockApi(IEdmModel model = null)
    {
        model ??= new EdmModel();
        var queryHandler = Substitute.For<IQueryHandler>();
        var submitHandler = Substitute.For<ISubmitHandler>();

        // Set up SubmitAsync to return a successful result for any context.
        submitHandler.SubmitAsync(Arg.Any<SubmitContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var ctx = callInfo.Arg<SubmitContext>();
                return Task.FromResult(new SubmitResult(ctx.ChangeSet ?? new ChangeSet()));
            });

        return Substitute.For<TestApi>(model, queryHandler, submitHandler);
    }

    private static HttpContext CreateMockHttpContext(
        string contentId, string method, string url, string body = null)
    {
        var context = new DefaultHttpContext();

        // Set OData batch feature for ContentId.
        var batchFeature = new ODataBatchFeature { ContentId = contentId };
        context.Features.Set<IODataBatchFeature>(batchFeature);

        context.Request.Method = method;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            context.Request.Scheme = uri.Scheme;
            context.Request.Host = uri.IsDefaultPort
                ? new HostString(uri.Host)
                : new HostString(uri.Host, uri.Port);
            context.Request.Path = uri.AbsolutePath;
            context.Request.QueryString = new QueryString(uri.Query);
        }
        else
        {
            // Relative URL like "$1".
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");
            context.Request.Path = "/" + url.TrimStart('/');
        }

        if (body is not null)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = bytes.Length;
        }

        return context;
    }

    private static IEdmModel CreateEdmModel()
    {
        var model = new EdmModel();
        var entityType = new EdmEntityType("Test", "Book");
        entityType.AddKeys(entityType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Guid));
        entityType.AddStructuralProperty("Title", EdmPrimitiveTypeKind.String);
        model.AddElement(entityType);

        var container = new EdmEntityContainer("Test", "Default");
        container.AddEntitySet("Books", entityType);
        model.AddElement(container);

        return model;
    }

    private static IEdmModel CreateEdmModelWithTwoEntitySets()
    {
        var model = new EdmModel();

        var bookType = new EdmEntityType("Test", "Book");
        bookType.AddKeys(bookType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Guid));
        bookType.AddStructuralProperty("Title", EdmPrimitiveTypeKind.String);
        model.AddElement(bookType);

        var detailType = new EdmEntityType("Test", "Detail");
        detailType.AddKeys(detailType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32));
        detailType.AddStructuralProperty("BookId", EdmPrimitiveTypeKind.Guid);
        model.AddElement(detailType);

        var container = new EdmEntityContainer("Test", "Default");
        container.AddEntitySet("Books", bookType);
        container.AddEntitySet("Details", detailType);
        model.AddElement(container);

        return model;
    }

    public class TestApi : ApiBase
    {
        public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }
    }

    #endregion
}
