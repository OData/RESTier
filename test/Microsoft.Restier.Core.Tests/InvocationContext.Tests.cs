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
            var configuration = new ApiConfiguration();
            configuration.EnsureCommitted();
            var apiContext = new ApiContext(configuration);
            var context = new InvocationContext(apiContext);
            Assert.Same(apiContext, context.ApiContext);
        }

        [Fact]
        public void InvocationContextGetsHookPointsCorrectly()
        {
            var hook = new HookA();
            var configuration = new ApiConfiguration().AddHookHandler<IHookA>(hook);
            configuration.EnsureCommitted();
            var apiContext = new ApiContext(configuration);
            var context = new InvocationContext(apiContext);
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
