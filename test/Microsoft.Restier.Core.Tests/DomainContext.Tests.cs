// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Core.Tests
{
    [TestClass]
    public class DomainContextTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DomainContextOnlyAcceptsCommittedConfiguration()
        {
            var configuration = new DomainConfiguration();
            new DomainContext(configuration);
        }

        [TestMethod]
        public void NewDomainContextIsConfiguredCorrectly()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            Assert.AreSame(configuration, context.Configuration);
        }
    }
}
