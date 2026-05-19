// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Restier.EntityFramework;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFramework.Query
{
    /// <summary>
    /// Unit tests around the tracking-behavior options surface for the EF6
    /// provider. End-to-end cycle-aware fallback behavior is exercised by the
    /// higher-level Breakdance scenario suites that run against real SQL Server.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class EFQueryNoTrackingTests
    {
        [Fact]
        public void Default_TrackingBehavior_IsDefault()
        {
            var options = new RestierEFOptions();
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.Default);
        }

        [Fact]
        public void TrackingBehavior_RoundTrips_TrackAll()
        {
            var options = new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.TrackAll };
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.TrackAll);
        }

        [Fact]
        public void TrackingBehavior_RoundTrips_NoTracking()
        {
            var options = new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.NoTracking };
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.NoTracking);
        }

        /// <summary>
        /// On EF6 the <see cref="RestierEFTrackingBehavior.NoTrackingWithIdentityResolution"/>
        /// value is accepted, but at runtime the sourcer falls back to plain
        /// <c>AsNoTracking</c> because EF6 has no equivalent API. This test only
        /// verifies the enum value round-trips through the options surface.
        /// </summary>
        [Fact]
        public void TrackingBehavior_RoundTrips_NoTrackingWithIdentityResolution()
        {
            var options = new RestierEFOptions
            {
                TrackingBehavior = RestierEFTrackingBehavior.NoTrackingWithIdentityResolution,
            };
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.NoTrackingWithIdentityResolution);
        }
    }

    /// <summary>
    /// Minimal test entity used by <see cref="TrackingTestContext"/>. Kept
    /// private to the test file so it doesn't pollute the LibraryContext
    /// scenario.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class TrackingTestEntity
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Minimal EF6 DbContext used purely to exercise
    /// <see cref="EFQueryExpressionSourcer.ApplyTracking"/> without touching
    /// SQL Server.
    /// </summary>
    /// <remarks>
    /// We can't reuse the real <c>LibraryContext</c> here: it registers a
    /// <c>DropCreateDatabaseIfModelChanges</c> initializer, and EF6's
    /// <c>AsNoTracking</c> call path goes through <c>InternalSet.Initialize</c>
    /// which runs the database initializer — that requires a live SQL Server
    /// connection. The constructor of this minimal context disables the
    /// initializer via <c>Database.SetInitializer&lt;TrackingTestContext&gt;(null)</c>,
    /// so model build runs entirely against the CLR types and no connection is
    /// ever attempted.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public class TrackingTestContext : DbContext
    {
        static TrackingTestContext()
        {
            // No database initializer — model build proceeds against CLR types,
            // no SQL Server connection is ever made.
            Database.SetInitializer<TrackingTestContext>(null);
        }

        public TrackingTestContext()
            : base("Server=(local);Database=Restier_TrackingTests_NeverConnected;Integrated Security=true;")
        {
        }

        public DbSet<TrackingTestEntity> Entities { get; set; }
    }

    /// <summary>
    /// Direct unit tests for <see cref="EFQueryExpressionSourcer.ApplyTracking"/>
    /// on the EF6 compilation. These bypass Breakdance (which is blocked for EF6
    /// by a pre-existing SQL Server fixture flake) and verify the EF6 decision
    /// matrix — including the recursive-expand fallback — using reference
    /// identity on the returned IQueryable.
    /// </summary>
    /// <remarks>
    /// We deliberately do NOT inspect <c>result.Expression</c> on EF6: accessing
    /// <c>DbQuery&lt;T&gt;.Expression</c> triggers lazy model build inside
    /// <c>InternalSet.Initialize</c>. EF6's <c>AsNoTracking</c> also routes
    /// through <c>InternalSet.Initialize</c>, but with the database initializer
    /// disabled on the minimal <see cref="TrackingTestContext"/> the
    /// initialization path completes without any SQL Server connection — the
    /// returned IQueryable is a fresh <c>DbQuery</c> wrapper distinct from the
    /// source DbSet, which is what we assert on.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public class EFQuerySourcerTrackingTests
    {
        private readonly TrackingTestContext context = new TrackingTestContext();

        /// <summary>
        /// EF6 + Default + no cycle → wraps DbSet with AsNoTracking (returns a
        /// different IQueryable than the bare DbSet).
        /// </summary>
        [Fact]
        public void Default_NoRecursiveExpand_AppliesAsNoTracking()
        {
            var set = context.Entities;
            var result = EFQueryExpressionSourcer.ApplyTracking(
                set,
                RestierEFTrackingBehavior.Default,
                hasRecursiveExpand: false);

            result.Should().NotBeSameAs(set);
        }

        /// <summary>
        /// EF6 + Default + recursive expand → bare DbSet (tracked).
        /// This is the recursive-expand fallback the cycle detector enables —
        /// EF6 has no AsNoTrackingWithIdentityResolution, so a cycle in $expand
        /// forces a tracked query to preserve identity.
        /// </summary>
        [Fact]
        public void Default_HasRecursiveExpand_FallsBackToTracked()
        {
            var set = context.Entities;
            var result = EFQueryExpressionSourcer.ApplyTracking(
                set,
                RestierEFTrackingBehavior.Default,
                hasRecursiveExpand: true);

            result.Should().BeSameAs(set);
        }

        /// <summary>
        /// EF6 + TrackAll → bare DbSet regardless of recursive-expand.
        /// </summary>
        [Fact]
        public void TrackAll_AlwaysTracked()
        {
            var set = context.Entities;
            var noCycle = EFQueryExpressionSourcer.ApplyTracking(
                set, RestierEFTrackingBehavior.TrackAll, hasRecursiveExpand: false);
            var withCycle = EFQueryExpressionSourcer.ApplyTracking(
                set, RestierEFTrackingBehavior.TrackAll, hasRecursiveExpand: true);

            noCycle.Should().BeSameAs(set);
            withCycle.Should().BeSameAs(set);
        }

        /// <summary>
        /// EF6 + NoTracking → always wraps with AsNoTracking, overriding the
        /// recursive-expand hint.
        /// </summary>
        [Fact]
        public void NoTracking_OverridesRecursiveExpandHint()
        {
            var set = context.Entities;
            var noCycle = EFQueryExpressionSourcer.ApplyTracking(
                set, RestierEFTrackingBehavior.NoTracking, hasRecursiveExpand: false);
            var withCycle = EFQueryExpressionSourcer.ApplyTracking(
                set, RestierEFTrackingBehavior.NoTracking, hasRecursiveExpand: true);

            noCycle.Should().NotBeSameAs(set);
            withCycle.Should().NotBeSameAs(set);
        }

        /// <summary>
        /// EF6 + NoTrackingWithIdentityResolution → falls back to plain
        /// AsNoTracking (EF6 has no identity-resolution-aware equivalent).
        /// The returned IQueryable is wrapped (not the same instance as the
        /// input DbSet).
        /// </summary>
        [Fact]
        public void NoTrackingWithIdentityResolution_FallsBackToNoTracking_OnEF6()
        {
            var set = context.Entities;
            var result = EFQueryExpressionSourcer.ApplyTracking(
                set,
                RestierEFTrackingBehavior.NoTrackingWithIdentityResolution,
                hasRecursiveExpand: false);

            result.Should().NotBeSameAs(set);
        }
    }
}
