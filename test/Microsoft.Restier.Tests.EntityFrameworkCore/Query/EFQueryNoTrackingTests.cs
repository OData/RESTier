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
using Xunit;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Query
{
    /// <summary>
    /// Unit tests around the tracking-behavior options surface for the EFCore
    /// provider. End-to-end "GET via controller leaves the tracker empty"
    /// assertions live in the higher-level Breakdance scenario suites; these
    /// tests cover the options API.
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

        [Fact]
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
        [Fact]
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
}
