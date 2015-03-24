// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class DomainBaseTests
    {
        private class TestDomain : DomainBase
        {
        }

        [Fact]
        public void DefaultDomainBaseCanBeCreatedAndDisposed()
        {
            using (var domain = new TestDomain())
            {
                domain.Dispose();
            }
        }

        [Fact]
        public void DefaultDomainBaseIsConfiguredCorrectly()
        {
            using (var domain = new TestDomain())
            {
                var expandableDomain = domain as IExpandableDomain;
                Assert.NotNull(expandableDomain.Configuration);
                Assert.False(expandableDomain.IsInitialized);
                Assert.NotNull(expandableDomain.Context);
                Assert.True(expandableDomain.IsInitialized);
                Assert.Same(expandableDomain.Configuration,
                    expandableDomain.Context.Configuration);
            }
        }

        [Fact]
        public void DisposedDomainBaseCannotAccessConfiguration()
        {
            var domain = new TestDomain();
            domain.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { var configuration = ((IExpandableDomain)domain).Configuration; });
        }

        [Fact]
        public void DisposedDomainBaseCannotAccessIsInitialized()
        {
            var domain = new TestDomain();
            domain.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { var configuration = ((IExpandableDomain)domain).IsInitialized; });
        }

        [Fact]
        public void DisposedDomainBaseCannotAccessContext()
        {
            var domain = new TestDomain();
            domain.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { var configuration = ((IExpandableDomain)domain).Context; });
        }

        [Fact]
        public void DisposedDomainBaseCannotBeInitialized()
        {
            var domain = new TestDomain();
            var expandableDomain = domain as IExpandableDomain;
            var derivedConfig = new DomainConfiguration(
                expandableDomain.Configuration);
            domain.Dispose();
            Assert.Throws<ObjectDisposedException>(() => expandableDomain.Initialize(derivedConfig));
        }

        [Fact]
        public void DomainBaseCannotBeInitializedIfAlreadyInitialized()
        {
            var domain = new TestDomain();
            var expandableDomain = domain as IExpandableDomain;
            var derivedConfig = new DomainConfiguration(
                expandableDomain.Configuration);
            var context = expandableDomain.Context;
            Assert.Throws<InvalidOperationException>(() => expandableDomain.Initialize(derivedConfig));
        }

        [Fact]
        public void DomainBaseCannotBeInitializedWithUnrelatedConfiguration()
        {
            var domain = new TestDomain();
            var expandableDomain = domain as IExpandableDomain;
            var otherConfig = new DomainConfiguration();
            Assert.Throws<ArgumentException>(() => expandableDomain.Initialize(otherConfig));
        }

        [Fact]
        public void ExpandedDomainBaseIsInitializedCorrectly()
        {
            var domain = new TestDomain();
            var expandableDomain = domain as IExpandableDomain;
            var derivedConfig = new DomainConfiguration(
                expandableDomain.Configuration);
            derivedConfig.EnsureCommitted();

            expandableDomain.Initialize(derivedConfig);
            Assert.True(expandableDomain.IsInitialized);
            Assert.Same(derivedConfig,
                expandableDomain.Context.Configuration);
        }

        [Fact]
        public void AllTestDomainsHaveSameConfigurationUntilInvalidated()
        {
            IExpandableDomain domain1 = new TestDomain();
            IExpandableDomain domain2 = new TestDomain();
            Assert.Same(domain2.Configuration, domain1.Configuration);
            DomainConfiguration.Invalidate(domain1.GetType());
            IExpandableDomain domain3 = new TestDomain();
            Assert.NotSame(domain3.Configuration, domain2.Configuration);
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
                Assert.Same(typeof(TestDomainWithParticipants), type);
                configuration.SetProperty(this.Value, true);
            }

            public override void Initialize(
                DomainContext context,
                Type type, object instance)
            {
                base.Initialize(context, type, instance);
                Assert.Same(typeof(TestDomainWithParticipants), type);
                context.SetProperty(this.Value + ".Self", instance);
                context.SetProperty(this.Value, true);
            }

            public override void Dispose(
                DomainContext context,
                Type type, object instance)
            {
                Assert.Same(typeof(TestDomainWithParticipants), type);
                context.SetProperty(this.Value, false);
                base.Dispose(context, type, instance);
            }
        }

        [TestDomainParticipant("Test1")]
        [TestDomainParticipant("Test2")]
        private class TestDomainWithParticipants : DomainBase
        {
        }

        [Fact]
        public void TestDomainAppliesDomainParticipantsCorrectly()
        {
            IExpandableDomain domain = new TestDomainWithParticipants();

            var configuration = domain.Configuration;
            Assert.True(configuration.GetProperty<bool>("Test1"));
            Assert.True(configuration.GetProperty<bool>("Test2"));

            var context = domain.Context;
            Assert.True(context.GetProperty<bool>("Test1"));
            Assert.Same(domain, context.GetProperty("Test1.Self"));
            Assert.True(context.GetProperty<bool>("Test2"));
            Assert.Same(domain, context.GetProperty("Test2.Self"));

            (domain as IDisposable).Dispose();
            Assert.False(context.GetProperty<bool>("Test2"));
            Assert.False(context.GetProperty<bool>("Test1"));
        }
    }
}
