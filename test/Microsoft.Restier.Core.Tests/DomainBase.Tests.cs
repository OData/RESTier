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
        public void AllTestDomainsHaveSameConfigurationUntilInvalidated()
        {
            IDomain domain1 = new TestDomain();
            IDomain domain2 = new TestDomain();
            Assert.Same(domain2.Context.Configuration, domain1.Context.Configuration);
            DomainConfiguration.Invalidate(domain1.GetType());
            IDomain domain3 = new TestDomain();
            Assert.NotSame(domain3.Context.Configuration, domain2.Context.Configuration);
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
            IDomain domain = new TestDomainWithParticipants();

            var configuration = domain.Context.Configuration;
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
