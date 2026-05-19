// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

/// <summary>
/// End-to-end Breakdance tests that drive an HTTP GET through the
/// RESTier controller → query pipeline → sourcer and inspect the
/// composed <see cref="IQueryable"/> reaching the executor. This lets
/// us prove that the EF sourcer applied the configured tracking
/// transformation (or didn't, for <see cref="RestierEFTrackingBehavior.TrackAll"/>).
///
/// The pre-existing unit tests in <c>EFQueryNoTrackingTests</c> only
/// cover enum round-tripping and an isolated EF Core API sanity check —
/// they don't exercise the controller path.
/// </summary>
[Collection("LibraryApiEFCore")]
public class NoTrackingTests : RestierTestBase<LibraryApi>
{
    private static Action<IServiceCollection> ConfigureWithRecorderAndDefault =>
        services =>
        {
            services.AddEntityFrameworkServices<LibraryContext>();
            services.AddSingleton<IChainedService<IQueryExecutor>, RecordingQueryExecutor>();
        };

    private static Action<IServiceCollection> ConfigureWithRecorderAndTrackAll =>
        services =>
        {
            // RestierEFOptions is registered with TryAddSingleton inside
            // AddEFProviderServices, so our override must be added first.
            services.AddSingleton(new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.TrackAll });
            services.AddEntityFrameworkServices<LibraryContext>();
            services.AddSingleton<IChainedService<IQueryExecutor>, RecordingQueryExecutor>();
        };

    private static Action<IServiceCollection> ConfigureWithRecorderAndNoTracking =>
        services =>
        {
            services.AddSingleton(new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.NoTracking });
            services.AddEntityFrameworkServices<LibraryContext>();
            services.AddSingleton<IChainedService<IQueryExecutor>, RecordingQueryExecutor>();
        };

    [Fact]
    public async Task Get_AppliesAsNoTrackingWithIdentityResolution_ByDefault()
    {
        RecordingQueryExecutor.Reset();
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers",
            serviceCollection: ConfigureWithRecorderAndDefault);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RecordingQueryExecutor.LastQueryExpressionString
            .Should().NotBeNullOrEmpty()
            .And.Contain("AsNoTrackingWithIdentityResolution");
    }

    [Fact]
    public async Task Get_TrackAll_DoesNotWrapDbSet()
    {
        RecordingQueryExecutor.Reset();
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers",
            serviceCollection: ConfigureWithRecorderAndTrackAll);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RecordingQueryExecutor.LastQueryExpressionString
            .Should().NotBeNullOrEmpty()
            .And.NotContain("AsNoTracking");
    }

    [Fact]
    public async Task Get_NoTrackingBehavior_AppliesPlainAsNoTracking()
    {
        RecordingQueryExecutor.Reset();
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers",
            serviceCollection: ConfigureWithRecorderAndNoTracking);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RecordingQueryExecutor.LastQueryExpressionString
            .Should().NotBeNullOrEmpty()
            .And.Contain("AsNoTracking")
            .And.NotContain("AsNoTrackingWithIdentityResolution");
    }

    /// <summary>
    /// Regression test for the fix in <c>RestierController.Get</c> (OperationSegment
    /// branch): a bound function HTTP GET must opt its binding-source query into
    /// no-tracking. Before the fix, that branch's <c>QueryRequest</c> left
    /// <c>AllowNoTracking</c> at its default <c>false</c>, so the sourcer skipped
    /// the wrap and returned a tracked DbSet.
    ///
    /// <c>LibraryApi.PublishedBooks</c> is a composable bound function on
    /// <c>Publisher</c> (<c>[BoundOperation(IsComposable = true, EntitySetPath = "publisher/Books")]</c>),
    /// so <c>/Publishers('Publisher1')/PublishedBooks()</c> exercises the
    /// OperationSegment branch.
    /// </summary>
    [Fact]
    public async Task Get_BoundFunction_AppliesAsNoTrackingWithIdentityResolution_OnBindingSource()
    {
        RecordingQueryExecutor.Reset();
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers('Publisher1')/PublishedBooks()",
            serviceCollection: ConfigureWithRecorderAndDefault);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RecordingQueryExecutor.AllQueryExpressionStrings
            .Should().Contain(expr => expr.Contains("AsNoTrackingWithIdentityResolution"),
                because: "the binding-source query of a bound function GET must be opted into no-tracking");
    }

    /// <summary>
    /// Chained <see cref="IQueryExecutor"/> that records the composed
    /// <see cref="IQueryable"/>'s expression string before delegating to the
    /// inner executor (the EF executor). The expression string preserves the
    /// method-chain that the sourcer applied, so we can assert on
    /// <c>AsNoTrackingWithIdentityResolution</c> / <c>AsNoTracking</c> /
    /// the absence of either.
    /// </summary>
    internal class RecordingQueryExecutor : IQueryExecutor
    {
        // Static so the test method (which doesn't own the test server's
        // service provider) can observe what was recorded inside the pipeline.
        // The LibraryApiEFCore collection serializes these tests, so cross-test
        // bleed is not a concern, but Reset() defensively clears between runs.
        private static readonly System.Collections.Concurrent.ConcurrentQueue<string> AllExpressions
            = new();

        public static string LastQueryExpressionString { get; private set; }

        public static System.Collections.Generic.IReadOnlyCollection<string> AllQueryExpressionStrings
            => AllExpressions.ToArray();

        public static void Reset()
        {
            LastQueryExpressionString = null;
            while (AllExpressions.TryDequeue(out _)) { }
        }

        public IQueryExecutor Inner { get; set; }

        public Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            Record(query?.Expression);
            return Inner.ExecuteQueryAsync(context, query, cancellationToken);
        }

        public Task<QueryResult> ExecuteExpressionAsync<TResult>(
            QueryContext context,
            IQueryProvider queryProvider,
            Expression expression,
            CancellationToken cancellationToken)
        {
            Record(expression);
            return Inner.ExecuteExpressionAsync<TResult>(context, queryProvider, expression, cancellationToken);
        }

        private static void Record(Expression expression)
        {
            if (expression is null)
            {
                return;
            }

            var text = expression.ToString();
            LastQueryExpressionString = text;
            AllExpressions.Enqueue(text);
        }
    }
}
