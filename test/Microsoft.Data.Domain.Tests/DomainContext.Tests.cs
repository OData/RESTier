using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Domain.Tests
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
