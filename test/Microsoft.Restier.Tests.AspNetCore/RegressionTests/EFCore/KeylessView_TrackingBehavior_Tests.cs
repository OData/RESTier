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
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EFCore;

/// <summary>
/// End-to-end assertion for Follow-up B: a real HTTP GET against a keyless view
/// must leave the <see cref="Microsoft.EntityFrameworkCore.DbContext.ChangeTracker"/>
/// empty when <see cref="RestierEFOptions.TrackingBehavior"/> is set to
/// <see cref="RestierEFTrackingBehavior.NoTracking"/>. Pins the user-visible
/// contract that the EF options bag propagates through
/// <see cref="KeylessViewQueryExpressionSourcer"/>'s
/// <c>EFQueryExpressionSourcer.ApplyTracking(...)</c> call into the
/// underlying EF Core provider.
///
/// The per-behaviour decision matrix (NoTracking / Default /
/// NoTrackingWithIdentityResolution / TrackAll) is already covered by the
/// unit tests in <c>KeylessViewQueryExpressionSourcerTests</c>; this file
/// is the single end-to-end pin that motivated Follow-up B plus one
/// <c>TrackAll</c> contrast row for cheap confidence.
/// </summary>
[TestClass]
public class KeylessView_TrackingBehavior_Tests
{
    /// <summary>
    /// Configures EF services with a caller-supplied
    /// <see cref="RestierEFOptions"/> registered up-front. The shared
    /// <c>AddEFProviderServices</c> uses <c>TryAddSingleton</c> for the
    /// default options instance, so a pre-registered singleton wins.
    /// Adds a chained <see cref="IQueryExecutor"/> that captures the
    /// <c>ChangeTracker.Entries().Count()</c> after inner execution
    /// completes — the post-materialisation snapshot the assertion
    /// needs (taken while the request scope is still alive, so the
    /// <c>DbContext</c> instance is the same one the sourcer wrapped).
    /// </summary>
    private static Action<IServiceCollection> ConfigureWith(RestierEFTrackingBehavior behavior) =>
        services =>
        {
            services.AddSingleton(new RestierEFOptions { TrackingBehavior = behavior });
            services.AddEntityFrameworkServices<LibraryContext>();
            services.AddSingleton<IChainedService<IQueryExecutor>, ChangeTrackerProbeExecutor>();
        };

    [TestMethod]
    public async Task Get_KeylessView_WithNoTrackingOption_LeavesChangeTrackerEmpty()
    {
        ChangeTrackerProbeExecutor.Reset();

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()",
            serviceCollection: ConfigureWith(RestierEFTrackingBehavior.NoTracking));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ChangeTrackerProbeExecutor.PostExecutionEntryCount.Should().NotBeNull(
            because: "the keyless-view GET should reach the EF query executor");
        ChangeTrackerProbeExecutor.PostExecutionEntryCount.Should().Be(0,
            because: "RestierEFOptions.TrackingBehavior = NoTracking should keep the ChangeTracker empty for keyless-view GET requests");
    }

    [TestMethod]
    public async Task Get_KeylessView_WithTrackAllOption_LeavesChangeTrackerEmpty()
    {
        // Keyless types are unconditionally untrackable in EF Core regardless of
        // the wrapper applied to the source DbSet — HasNoKey() entities never
        // enter the ChangeTracker. We pin this here as a contrast row to the
        // NoTracking assertion above so that a future regression in
        // KeylessViewQueryExpressionSourcer's tracking-passthrough still surfaces
        // a clear story: even when the user opts into TrackAll, keyless-view
        // rows do not pollute the ChangeTracker.
        ChangeTrackerProbeExecutor.Reset();

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()",
            serviceCollection: ConfigureWith(RestierEFTrackingBehavior.TrackAll));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ChangeTrackerProbeExecutor.PostExecutionEntryCount.Should().NotBeNull(
            because: "the keyless-view GET should reach the EF query executor");
        ChangeTrackerProbeExecutor.PostExecutionEntryCount.Should().Be(0,
            because: "keyless entity types are untrackable in EF Core, so the ChangeTracker stays empty even under RestierEFTrackingBehavior.TrackAll");
    }

    /// <summary>
    /// Chained <see cref="IQueryExecutor"/> that captures
    /// <c>((IEntityFrameworkApi)context.Api).DbContext.ChangeTracker.Entries().Count()</c>
    /// after the inner executor materialises the query. Captures while the
    /// request scope is still alive, so the inspected <c>DbContext</c> is the
    /// same instance the sourcer wrapped.
    /// </summary>
    internal sealed class ChangeTrackerProbeExecutor : IQueryExecutor
    {
        // Static because the test method does not own the test server's
        // service provider. Tests in this class are not parallelised at
        // the xUnit collection level — each is a fresh test server with
        // its own configured pipeline — but Reset() defensively clears
        // between runs.
        public static int? PostExecutionEntryCount { get; private set; }

        public static void Reset() => PostExecutionEntryCount = null;

        public IQueryExecutor Inner { get; set; }

        public async Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            var result = await Inner.ExecuteQueryAsync(context, query, cancellationToken).ConfigureAwait(false);
            Capture(context);
            return result;
        }

        public async Task<QueryResult> ExecuteExpressionAsync<TResult>(
            QueryContext context,
            IQueryProvider queryProvider,
            Expression expression,
            CancellationToken cancellationToken)
        {
            var result = await Inner.ExecuteExpressionAsync<TResult>(context, queryProvider, expression, cancellationToken).ConfigureAwait(false);
            Capture(context);
            return result;
        }

        private static void Capture(QueryContext context)
        {
            if (context?.Api is IEntityFrameworkApi efApi)
            {
                PostExecutionEntryCount = efApi.DbContext.ChangeTracker.Entries().Count();
            }
        }
    }
}
