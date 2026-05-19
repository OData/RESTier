// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

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
}
