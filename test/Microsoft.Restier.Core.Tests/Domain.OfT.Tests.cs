// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class DomainOfTTests
    {
        private class InnerDomain : DomainBase
        {
            public InnerDomain()
            {
            }

            public InnerDomain(string value)
            {
                this.Value = value;
            }

            public string Value { get; private set; }

            protected override DomainConfiguration CreateDomainConfiguration()
            {
                var config = base.CreateDomainConfiguration();
                config.SetProperty("Value", this.Value);
                config.SetProperty("InnerDomain", true);
                return config;
            }

            protected override DomainContext CreateDomainContext(
                DomainConfiguration configuration)
            {
                var context = base.CreateDomainContext(configuration);
                context.SetProperty("InnerDomain", true);
                return context;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.DomainContext.SetProperty("InnerDomain", false);
                    DomainConfiguration.Invalidate(this.DomainConfigurationKey);
                }
                base.Dispose(disposing);
            }
        }

        private class OuterDomain : Domain<InnerDomain>
        {
            public OuterDomain(string value = null)
            {
                this.Value = value;
            }

            public string Value { get; private set; }

            protected override InnerDomain CreateExpandableDomain()
            {
                if (this.Value == null)
                {
                    return base.CreateExpandableDomain();
                }
                else
                {
                    return new InnerDomain(this.Value);
                }
            }

            protected override DomainConfiguration CreateDomainConfiguration()
            {
                var config = base.CreateDomainConfiguration();
                config.SetProperty("OuterDomain", true);
                return config;
            }

            protected override DomainContext CreateDomainContext(
                DomainConfiguration configuration)
            {
                var context = base.CreateDomainContext(configuration);
                context.SetProperty("OuterDomain", true);
                return context;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.DomainContext.SetProperty("OuterDomain", false);
                    DomainConfiguration.Invalidate(this.DomainConfigurationKey);
                }
                base.Dispose(disposing);
            }
        }

        [Fact]
        public void DomainOfTCorrectlyWrapsAutoConstructedInnerDomain()
        {
            IExpandableDomain domain = new OuterDomain();

            var configuration = domain.Configuration;
            Assert.Null(configuration.GetProperty<string>("Value"));
            Assert.True(configuration.GetProperty<bool>("InnerDomain"));
            Assert.True(configuration.GetProperty<bool>("OuterDomain"));

            var context = domain.Context;
            Assert.True(context.GetProperty<bool>("InnerDomain"));
            Assert.True(context.GetProperty<bool>("OuterDomain"));

            (domain as IDisposable).Dispose();
            Assert.False(context.GetProperty<bool>("OuterDomain"));
            Assert.False(context.GetProperty<bool>("InnerDomain"));
        }

        [Fact]
        public void DomainOfTCorrectlyWrapsCustomConstructedInnerDomain()
        {
            IExpandableDomain domain = new OuterDomain("Test");

            var configuration = domain.Configuration;
            Assert.Equal("Test", configuration.GetProperty<string>("Value"));
            Assert.True(configuration.GetProperty<bool>("InnerDomain"));
            Assert.True(configuration.GetProperty<bool>("OuterDomain"));

            var context = domain.Context;
            Assert.True(context.GetProperty<bool>("InnerDomain"));
            Assert.True(context.GetProperty<bool>("OuterDomain"));

            (domain as IDisposable).Dispose();
            Assert.False(context.GetProperty<bool>("OuterDomain"));
            Assert.False(context.GetProperty<bool>("InnerDomain"));
        }
    }
}
