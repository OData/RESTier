// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.Restier.AspNetCore.Versioning.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Internal
{

    [TestClass]
    public class ApiVersionAttributeReaderTests
    {

        [TestMethod]
        public void Read_SingleAttribute_ReturnsOneEntry()
        {
            var entries = ApiVersionAttributeReader.Read(typeof(SingleVersion)).ToArray();

            entries.Should().HaveCount(1);
            entries[0].ApiVersion.Should().Be(new ApiVersion(1, 0));
            entries[0].IsDeprecated.Should().BeFalse();
        }

        [TestMethod]
        public void Read_MultipleAttributes_ReturnsAllEntriesInDeclarationOrder()
        {
            var entries = ApiVersionAttributeReader.Read(typeof(TwoVersions)).ToArray();

            entries.Should().HaveCount(2);
            entries.Should().ContainSingle(e => e.ApiVersion == new ApiVersion(1, 0) && e.IsDeprecated);
            entries.Should().ContainSingle(e => e.ApiVersion == new ApiVersion(2, 0) && !e.IsDeprecated);
        }

        [TestMethod]
        public void Read_NoAttribute_ThrowsInvalidOperation()
        {
            Action act = () => ApiVersionAttributeReader.Read(typeof(NoAttribute)).ToArray();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{typeof(NoAttribute).FullName}*[ApiVersion]*imperative overload*");
        }

        [ApiVersion("1.0")]
        private class SingleVersion { }

        [ApiVersion("1.0", Deprecated = true)]
        [ApiVersion("2.0")]
        private class TwoVersions { }

        private class NoAttribute { }

    }

}
