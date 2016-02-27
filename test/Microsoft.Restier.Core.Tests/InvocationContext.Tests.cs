// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class InvocationContextTests
    {
        [Fact]
        public void NewInvocationContextIsConfiguredCorrectly()
        {
            var configuration = new ApiBuilder().Build();
            var apiContext = new ApiContext(configuration);
            var context = new InvocationContext(apiContext);
            Assert.Same(apiContext, context.ApiContext);
        }

        [Fact]
        public void InvocationContextGetsHookPointsCorrectly()
        {
            var hook = new HookA();
            var configuration = new ApiBuilder().CutoffPrevious<IHookA>(hook).Build();
            var apiContext = new ApiContext(configuration);
            var context = new InvocationContext(apiContext);
            Assert.Same(hook, context.GetApiService<IHookA>());
        }

        private interface IHookA
        {
        }

        private class HookA : IHookA
        {
        }
    }
}
