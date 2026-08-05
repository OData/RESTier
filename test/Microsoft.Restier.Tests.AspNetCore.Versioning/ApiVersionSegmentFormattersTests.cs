// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Asp.Versioning;
using FluentAssertions;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning
{

    [TestClass]
    public class ApiVersionSegmentFormattersTests
    {

        [TestMethod]
        public void Major_FormatsAsVPrefixedMajorOnly()
        {
            ApiVersionSegmentFormatters.Major(new ApiVersion(1, 0)).Should().Be("v1");
            ApiVersionSegmentFormatters.Major(new ApiVersion(2, 7)).Should().Be("v2");
        }

        [TestMethod]
        public void MajorMinor_FormatsAsVPrefixedMajorAndMinor()
        {
            ApiVersionSegmentFormatters.MajorMinor(new ApiVersion(1, 0)).Should().Be("v1.0");
            ApiVersionSegmentFormatters.MajorMinor(new ApiVersion(2, 7)).Should().Be("v2.7");
        }

    }

}
