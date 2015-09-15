// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class InvocationContextTests
    {
        [Fact]
        public void NewInvocationContextIsConfiguredCorrectly()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();
            var domainContext = new DomainContext(configuration);
            var context = new InvocationContext(domainContext);
            Assert.Same(domainContext, context.DomainContext);
        }

        [Fact]
        public void InvocationContextGetsHookPointsCorrectly()
        {
            var hook = new HookA();
            var configuration = new DomainConfiguration().AddHookHandler<IHookA>(hook);
            configuration.EnsureCommitted();
            var domainContext = new DomainContext(configuration);
            var context = new InvocationContext(domainContext);
            Assert.Same(hook, context.GetHookHandler<IHookA>());
        }

        private interface IHookA : IHookHandler
        {
        }

        private class HookA : IHookA
        {
        }
    }
}
