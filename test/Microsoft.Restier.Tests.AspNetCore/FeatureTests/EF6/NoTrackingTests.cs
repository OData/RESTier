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
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

/// <summary>
/// End-to-end Breakdance tests that drive an HTTP GET through the
/// RESTier controller → query pipeline → sourcer and inspect the
/// composed <see cref="IQueryable"/> reaching the executor. This lets
/// us prove that the EF6 sourcer applied the configured tracking
/// transformation (or didn't, for <see cref="RestierEFTrackingBehavior.TrackAll"/>
/// and for the recursive-expand fallback).
///
/// Mirrors <c>EFCore.NoTrackingTests</c>; EF6 has no
/// <c>AsNoTrackingWithIdentityResolution</c>, so the default behavior here
/// is plain <c>AsNoTracking</c>, and the recursive-expand case falls back
/// to a bare DbSet (tracked) to preserve identity across the cycle.
/// </summary>
[Collection("LibraryApiEF6")]
public class NoTrackingTests : RestierTestBase<LibraryApi>
{
    private static Action<IServiceCollection> ConfigureWithRecorderAndDefault =>
        services =>
        {
            services.AddEntityFrameworkServices<LibraryContext>();
            services.AddSingleton<IChainedService<IQueryExecutor>, EF6RecordingQueryExecutor>();
        };

    private static Action<IServiceCollection> ConfigureWithRecorderAndTrackAll =>
        services =>
        {
            // RestierEFOptions is registered with TryAddSingleton inside
            // AddEFProviderServices, so our override must be added first.
            services.AddSingleton(new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.TrackAll });
            services.AddEntityFrameworkServices<LibraryContext>();
            services.AddSingleton<IChainedService<IQueryExecutor>, EF6RecordingQueryExecutor>();
        };

    private static Action<IServiceCollection> ConfigureWithRecorderAndNoTracking =>
        services =>
        {
            services.AddSingleton(new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.NoTracking });
            services.AddEntityFrameworkServices<LibraryContext>();
            services.AddSingleton<IChainedService<IQueryExecutor>, EF6RecordingQueryExecutor>();
        };

    [Fact]
    public async Task Get_AppliesAsNoTracking_ByDefault()
    {
        EF6RecordingQueryExecutor.Reset();
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers",
            serviceCollection: ConfigureWithRecorderAndDefault);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // EF6's AsNoTracking lowers to ObjectQuery.MergeAs(MergeOption.NoTracking)
        // in the composed expression tree, so the substring "NoTracking" is the
        // robust marker (EF6 never produces the literal "AsNoTracking" call).
        EF6RecordingQueryExecutor.AllQueryExpressionStrings
            .Should().Contain(s => s.Contains("NoTracking"));
        // Sanity: EF6 has no AsNoTrackingWithIdentityResolution
        EF6RecordingQueryExecutor.AllQueryExpressionStrings
            .Should().NotContain(s => s.Contains("AsNoTrackingWithIdentityResolution"));
    }

    [Fact]
    public async Task Get_TrackAll_DoesNotWrapDbSet()
    {
        EF6RecordingQueryExecutor.Reset();
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers",
            serviceCollection: ConfigureWithRecorderAndTrackAll);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        EF6RecordingQueryExecutor.AllQueryExpressionStrings
            .Should().NotContain(s => s.Contains("NoTracking"));
    }

    [Fact]
    public async Task Get_NoTrackingBehavior_AppliesAsNoTracking()
    {
        EF6RecordingQueryExecutor.Reset();
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers",
            serviceCollection: ConfigureWithRecorderAndNoTracking);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        EF6RecordingQueryExecutor.AllQueryExpressionStrings
            .Should().Contain(s => s.Contains("NoTracking"));
    }

    /// <summary>
    /// EF6-specific behavior: when the request shape contains a recursive
    /// (cross-type) <c>$expand</c> cycle, the sourcer falls back to a bare,
    /// tracked DbSet so identity resolution holds across the cycle. EF6 has
    /// no <c>AsNoTrackingWithIdentityResolution</c> equivalent, so plain
    /// <c>AsNoTracking</c> can't be used here.
    ///
    /// <c>/Publishers?$expand=Books($expand=Publisher)</c> forms a
    /// Publisher → Book → Publisher cross-type cycle. The
    /// <c>IExpandCycleDetector</c> sets
    /// <c>QueryRequest.HasRecursiveExpand = true</c>, and the EF6 sourcer's
    /// Default branch routes to the tracked DbSet.
    ///
    /// We root from <c>/Publishers</c> rather than <c>/Books</c> because the
    /// Library seed contains a "Sea of Rust" book with a null
    /// <c>Publisher</c> nav, which triggers an unrelated NRE deep inside the
    /// EF6 client-projection path when the second-level Publisher expand
    /// is materialized for that orphan row.
    /// </summary>
    [Fact]
    public async Task Get_WithRecursiveExpand_FallsBackToTracked()
    {
        EF6RecordingQueryExecutor.Reset();
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers?$expand=Books($expand=Publisher)",
            serviceCollection: ConfigureWithRecorderAndDefault);
        var body = await TraceListener.LogAndReturnMessageContentAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
        EF6RecordingQueryExecutor.AllQueryExpressionStrings
            .Should().NotContain(s => s.Contains("NoTracking"),
                because: "EF6 falls back to tracked when the request $expand tree contains a cycle (no AsNoTrackingWithIdentityResolution available)");
    }

    /// <summary>
    /// Chained <see cref="IQueryExecutor"/> that records the composed
    /// <see cref="IQueryable"/>'s expression string before delegating to the
    /// inner executor (the EF6 executor). The expression string preserves
    /// the method-chain that the sourcer applied, so we can assert on
    /// <c>AsNoTracking</c> / the absence of it. This is the EF6 sibling of
    /// the EFCore <c>NoTrackingTests.RecordingQueryExecutor</c>; named
    /// distinctly to avoid type-resolution conflicts when both test files
    /// compile into the same assembly.
    /// </summary>
    internal class EF6RecordingQueryExecutor : IQueryExecutor
    {
        // Static so the test method (which doesn't own the test server's
        // service provider) can observe what was recorded inside the pipeline.
        // The LibraryApiEF6 collection serializes these tests, so cross-test
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
