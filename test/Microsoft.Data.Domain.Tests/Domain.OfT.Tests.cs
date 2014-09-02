using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Domain.Tests
{
    [TestClass]
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

        [TestMethod]
        public void DomainOfTCorrectlyWrapsAutoConstructedInnerDomain()
        {
            IExpandableDomain domain = new OuterDomain();

            var configuration = domain.Configuration;
            Assert.IsNull(configuration.GetProperty<string>("Value"));
            Assert.IsTrue(configuration.GetProperty<bool>("InnerDomain"));
            Assert.IsTrue(configuration.GetProperty<bool>("OuterDomain"));

            var context = domain.Context;
            Assert.IsTrue(context.GetProperty<bool>("InnerDomain"));
            Assert.IsTrue(context.GetProperty<bool>("OuterDomain"));

            (domain as IDisposable).Dispose();
            Assert.IsFalse(context.GetProperty<bool>("OuterDomain"));
            Assert.IsFalse(context.GetProperty<bool>("InnerDomain"));
        }

        [TestMethod]
        public void DomainOfTCorrectlyWrapsCustomConstructedInnerDomain()
        {
            IExpandableDomain domain = new OuterDomain("Test");

            var configuration = domain.Configuration;
            Assert.AreEqual("Test", configuration.GetProperty<string>("Value"));
            Assert.IsTrue(configuration.GetProperty<bool>("InnerDomain"));
            Assert.IsTrue(configuration.GetProperty<bool>("OuterDomain"));

            var context = domain.Context;
            Assert.IsTrue(context.GetProperty<bool>("InnerDomain"));
            Assert.IsTrue(context.GetProperty<bool>("OuterDomain"));

            (domain as IDisposable).Dispose();
            Assert.IsFalse(context.GetProperty<bool>("OuterDomain"));
            Assert.IsFalse(context.GetProperty<bool>("InnerDomain"));
        }
    }
}
