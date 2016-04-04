// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class InvocationContextTests
    {
        [Fact]
        public void NewInvocationContextIsConfiguredCorrectly()
        {
            var configuration = new ServiceCollection()
                .BuildApiConfiguration();
            var apiContext = new ApiContext(configuration);
            var context = new InvocationContext(apiContext);
            Assert.Same(apiContext, context.ApiContext);
        }

        [Fact]
        public void InvocationContextGetsHookPointsCorrectly()
        {
            var hook = new HookA();
            var configuration = new ServiceCollection()
                .CutoffPrevious<IHookA>(hook)
                .BuildApiConfiguration();
            var apiContext = new ApiContext(configuration);
            var context = new InvocationContext(apiContext);
            Assert.Same(hook, context.GetApiContextService<IHookA>());
        }

        private interface IHookA
        {
        }

        private class HookA : IHookA
        {
        }
    }
}
