// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Query
{
    /// <summary>
    /// Unit tests around the tracking-behavior options surface for the EFCore
    /// provider. End-to-end "GET via controller leaves the tracker empty"
    /// assertions live in the higher-level Breakdance scenario suites; these
    /// tests cover the options API.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class EFQueryNoTrackingTests
    {
        [TestMethod]
        public void Default_TrackingBehavior_IsDefault()
        {
            var options = new RestierEFOptions();
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.Default);
        }

        [TestMethod]
        public void TrackingBehavior_RoundTrips_TrackAll()
        {
            var options = new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.TrackAll };
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.TrackAll);
        }

        [TestMethod]
        public void TrackingBehavior_RoundTrips_NoTracking()
        {
            var options = new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.NoTracking };
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.NoTracking);
        }

        [TestMethod]
        public void TrackingBehavior_RoundTrips_NoTrackingWithIdentityResolution()
        {
            var options = new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.NoTrackingWithIdentityResolution };
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.NoTrackingWithIdentityResolution);
        }

        /// <summary>
        /// Sanity check: confirms that AsNoTrackingWithIdentityResolution on a
        /// real EF Core query actually leaves the change tracker empty. Acts as
        /// a guard against future EF Core API changes.
        /// </summary>
        [TestMethod]
        public void AsNoTrackingWithIdentityResolution_LeavesChangeTrackerEmpty()
        {
            var options = new DbContextOptionsBuilder<LibraryContext>()
                .UseInMemoryDatabase($"notracking-{Guid.NewGuid()}")
                .Options;

            using var context = new LibraryContext(options);
            context.Publishers.Add(new Publisher
            {
                Id = "P1",
            });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var publishers = context.Publishers.AsNoTrackingWithIdentityResolution().ToList();

            publishers.Should().HaveCount(1);
            context.ChangeTracker.Entries().Should().BeEmpty();
        }
    }

    /// <summary>
    /// Direct unit tests for <see cref="EFQueryExpressionSourcer.ApplyTracking"/>
    /// on the EFCore compilation. These cover the full
    /// <see cref="RestierEFTrackingBehavior"/> × <c>HasRecursiveExpand</c>
    /// decision matrix by inspecting the IQueryable expression tree returned
    /// for each combination. The EFCore path ignores the recursive-expand hint
    /// — identity resolution covers cycles natively via
    /// <c>AsNoTrackingWithIdentityResolution</c>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class EFQuerySourcerTrackingTests
    {
        private readonly LibraryContext context;

        public EFQuerySourcerTrackingTests()
        {
            var options = new DbContextOptionsBuilder<LibraryContext>()
                .UseInMemoryDatabase($"sourcer-{Guid.NewGuid()}")
                .Options;
            context = new LibraryContext(options);
        }

        [TestCleanup]
        public void Cleanup() => context?.Dispose();

        /// <summary>
        /// EFCore + Default → identity-resolved no-tracking. Assert with the
        /// more specific method name so we don't get a false positive from the
        /// substring "AsNoTracking".
        /// </summary>
        [TestMethod]
        public void Default_NoRecursiveExpand_AppliesAsNoTrackingWithIdentityResolution()
        {
            var result = EFQueryExpressionSourcer.ApplyTracking(
                context.Books,
                RestierEFTrackingBehavior.Default,
                hasRecursiveExpand: false);

            result.Expression.ToString().Should().Contain("AsNoTrackingWithIdentityResolution");
        }

        /// <summary>
        /// EFCore + Default + recursive-expand=true → still identity-resolved
        /// no-tracking. The hint is irrelevant on EFCore because
        /// AsNoTrackingWithIdentityResolution preserves identity across cycles.
        /// </summary>
        [TestMethod]
        public void Default_HasRecursiveExpand_StillAppliesAsNoTrackingWithIdentityResolution()
        {
            var result = EFQueryExpressionSourcer.ApplyTracking(
                context.Books,
                RestierEFTrackingBehavior.Default,
                hasRecursiveExpand: true);

            result.Expression.ToString().Should().Contain("AsNoTrackingWithIdentityResolution");
        }

        /// <summary>
        /// EFCore + TrackAll → bare DbSet regardless of recursive-expand.
        /// </summary>
        [TestMethod]
        public void TrackAll_AlwaysTracked()
        {
            var noCycle = EFQueryExpressionSourcer.ApplyTracking(
                context.Books, RestierEFTrackingBehavior.TrackAll, hasRecursiveExpand: false);
            var withCycle = EFQueryExpressionSourcer.ApplyTracking(
                context.Books, RestierEFTrackingBehavior.TrackAll, hasRecursiveExpand: true);

            noCycle.Expression.ToString().Should().NotContain("AsNoTracking");
            withCycle.Expression.ToString().Should().NotContain("AsNoTracking");
        }

        /// <summary>
        /// EFCore + NoTracking → plain AsNoTracking, NOT the identity-resolution
        /// variant. We assert both: the substring "AsNoTracking" appears, and
        /// the more specific "AsNoTrackingWithIdentityResolution" does not.
        /// </summary>
        [TestMethod]
        public void NoTracking_AppliesAsNoTrackingOnly()
        {
            var result = EFQueryExpressionSourcer.ApplyTracking(
                context.Books,
                RestierEFTrackingBehavior.NoTracking,
                hasRecursiveExpand: false);

            var expr = result.Expression.ToString();
            expr.Should().Contain("AsNoTracking");
            expr.Should().NotContain("AsNoTrackingWithIdentityResolution");
        }

        /// <summary>
        /// EFCore + NoTrackingWithIdentityResolution → identity-resolved
        /// no-tracking.
        /// </summary>
        [TestMethod]
        public void NoTrackingWithIdentityResolution_AppliesAsNoTrackingWithIdentityResolution()
        {
            var result = EFQueryExpressionSourcer.ApplyTracking(
                context.Books,
                RestierEFTrackingBehavior.NoTrackingWithIdentityResolution,
                hasRecursiveExpand: false);

            result.Expression.ToString().Should().Contain("AsNoTrackingWithIdentityResolution");
        }
    }
}
