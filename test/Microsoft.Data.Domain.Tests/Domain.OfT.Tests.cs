// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
