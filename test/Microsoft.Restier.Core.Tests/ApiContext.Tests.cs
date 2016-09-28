// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ApiContextTests
    {
        private class TestApi : ApiBase
        {
            public TestApi(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        [Fact]
        public void NewApiContextIsConfiguredCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            Assert.NotNull(api);
        }
    }
}
