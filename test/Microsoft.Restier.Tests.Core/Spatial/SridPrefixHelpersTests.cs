// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.Restier.Core.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core.Spatial
{
    [TestClass]
    public class SridPrefixHelpersTests
    {
        [TestMethod]
        public void Format_emits_canonical_SRID_prefix()
        {
            var text = SridPrefixHelpers.FormatWithSridPrefix(4326, "POINT(1 2)");
            text.Should().Be("SRID=4326;POINT(1 2)");
        }

        [TestMethod]
        public void Parse_returns_srid_and_body_for_prefixed_input()
        {
            var (srid, body) = SridPrefixHelpers.ParseSridPrefix("SRID=4269;POINT(1 2)");
            srid.Should().Be(4269);
            body.Should().Be("POINT(1 2)");
        }

        [TestMethod]
        public void Parse_returns_null_srid_for_input_without_prefix()
        {
            var (srid, body) = SridPrefixHelpers.ParseSridPrefix("POINT(1 2)");
            srid.Should().BeNull();
            body.Should().Be("POINT(1 2)");
        }

        [TestMethod]
        [DataRow("SRID=POINT(1 2)")]                    // no semicolon
        [DataRow("SRID=;POINT(1 2)")]                   // empty SRID
        [DataRow("SRID=abc;POINT(1 2)")]                // non-integer SRID
        public void Parse_throws_for_malformed_prefix(string input)
        {
            var act = () => SridPrefixHelpers.ParseSridPrefix(input);
            act.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void Round_trip_is_lossless()
        {
            var formatted = SridPrefixHelpers.FormatWithSridPrefix(3857, "LINESTRING(0 0, 1 1)");
            var (srid, body) = SridPrefixHelpers.ParseSridPrefix(formatted);
            srid.Should().Be(3857);
            body.Should().Be("LINESTRING(0 0, 1 1)");
        }
    }
}
