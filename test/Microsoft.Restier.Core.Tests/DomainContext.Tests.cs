// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class DomainContextTests
    {
        [Fact]
        public void DomainContextOnlyAcceptsCommittedConfiguration()
        {
            var configuration = new DomainConfiguration();
            Assert.Throws<ArgumentException>(() => new DomainContext(configuration));
        }

        [Fact]
        public void NewDomainContextIsConfiguredCorrectly()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            Assert.Same(configuration, context.Configuration);
        }
    }
}
