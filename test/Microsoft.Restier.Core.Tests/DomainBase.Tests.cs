// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Core.Tests
{
    [TestClass]
    public class DomainBaseTests
    {
        private class TestDomain : DomainBase
        {
        }

        [TestMethod]
        public void DefaultDomainBaseCanBeCreatedAndDisposed()
        {
            using (var domain = new TestDomain())
            {
                domain.Dispose();
            }
        }

        [TestMethod]
        public void DefaultDomainBaseIsConfiguredCorrectly()
        {
            using (var domain = new TestDomain())
            {
                var expandableDomain = domain as IExpandableDomain;
                Assert.IsNotNull(expandableDomain.Configuration);
                Assert.IsFalse(expandableDomain.IsInitialized);
                Assert.IsNotNull(expandableDomain.Context);
                Assert.IsTrue(expandableDomain.IsInitialized);
                Assert.AreSame(expandableDomain.Configuration,
                    expandableDomain.Context.Configuration);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void DisposedDomainBaseCannotAccessConfiguration()
        {
            var domain = new TestDomain();
            domain.Dispose();
            var configuration = ((IExpandableDomain)domain).Configuration;
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void DisposedDomainBaseCannotAccessIsInitialized()
        {
            var domain = new TestDomain();
            domain.Dispose();
            var configuration = ((IExpandableDomain)domain).IsInitialized;
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void DisposedDomainBaseCannotAccessContext()
        {
            var domain = new TestDomain();
            domain.Dispose();
            var configuration = ((IExpandableDomain)domain).Context;
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void DisposedDomainBaseCannotBeInitialized()
        {
            var domain = new TestDomain();
            var expandableDomain = domain as IExpandableDomain;
            var derivedConfig = new DomainConfiguration(
                expandableDomain.Configuration);
            domain.Dispose();
            expandableDomain.Initialize(derivedConfig);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DomainBaseCannotBeInitializedIfAlreadyInitialized()
        {
            var domain = new TestDomain();
            var expandableDomain = domain as IExpandableDomain;
            var derivedConfig = new DomainConfiguration(
                expandableDomain.Configuration);
            var context = expandableDomain.Context;
            expandableDomain.Initialize(derivedConfig);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DomainBaseCannotBeInitializedWithUnrelatedConfiguration()
        {
            var domain = new TestDomain();
            var expandableDomain = domain as IExpandableDomain;
            var otherConfig = new DomainConfiguration();
            expandableDomain.Initialize(otherConfig);
        }

        [TestMethod]
        public void ExpandedDomainBaseIsInitializedCorrectly()
        {
            var domain = new TestDomain();
            var expandableDomain = domain as IExpandableDomain;
            var derivedConfig = new DomainConfiguration(
                expandableDomain.Configuration);
            derivedConfig.EnsureCommitted();

            expandableDomain.Initialize(derivedConfig);
            Assert.IsTrue(expandableDomain.IsInitialized);
            Assert.AreSame(derivedConfig,
                expandableDomain.Context.Configuration);
        }

        [TestMethod]
        public void AllTestDomainsHaveSameConfigurationUntilInvalidated()
        {
            IExpandableDomain domain1 = new TestDomain();
            IExpandableDomain domain2 = new TestDomain();
            Assert.AreSame(domain2.Configuration, domain1.Configuration);
            DomainConfiguration.Invalidate(domain1.GetType());
            IExpandableDomain domain3 = new TestDomain();
            Assert.AreNotSame(domain3.Configuration, domain2.Configuration);
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        private class TestDomainParticipantAttribute :
            DomainParticipantAttribute
        {
            public TestDomainParticipantAttribute(string value)
            {
                this.Value = value;
            }

            public string Value { get; private set; }

            public override void Configure(
                DomainConfiguration configuration,
                Type type)
            {
                base.Configure(configuration, type);
                Assert.AreSame(typeof(TestDomainWithParticipants), type);
                configuration.SetProperty(this.Value, true);
            }

            public override void Initialize(
                DomainContext context,
                Type type, object instance)
            {
                base.Initialize(context, type, instance);
                Assert.AreSame(typeof(TestDomainWithParticipants), type);
                context.SetProperty(this.Value + ".Self", instance);
                context.SetProperty(this.Value, true);
            }

            public override void Dispose(
                DomainContext context,
                Type type, object instance)
            {
                Assert.AreSame(typeof(TestDomainWithParticipants), type);
                context.SetProperty(this.Value, false);
                base.Dispose(context, type, instance);
            }
        }

        [TestDomainParticipant("Test1")]
        [TestDomainParticipant("Test2")]
        private class TestDomainWithParticipants : DomainBase
        {
        }

        [TestMethod]
        public void TestDomainAppliesDomainParticipantsCorrectly()
        {
            IExpandableDomain domain = new TestDomainWithParticipants();

            var configuration = domain.Configuration;
            Assert.IsTrue(configuration.GetProperty<bool>("Test1"));
            Assert.IsTrue(configuration.GetProperty<bool>("Test2"));

            var context = domain.Context;
            Assert.IsTrue(context.GetProperty<bool>("Test1"));
            Assert.AreSame(domain, context.GetProperty("Test1.Self"));
            Assert.IsTrue(context.GetProperty<bool>("Test2"));
            Assert.AreSame(domain, context.GetProperty("Test2.Self"));

            (domain as IDisposable).Dispose();
            Assert.IsFalse(context.GetProperty<bool>("Test2"));
            Assert.IsFalse(context.GetProperty<bool>("Test1"));
        }
    }
}
