// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ApiContextTests
    {
        [Fact]
        public void NewApiContextIsConfiguredCorrectly()
        {
            var configuration = new ServiceCollection().BuildApiConfiguration();
            var context = new ApiContext(configuration);
            Assert.Same(configuration, context.Configuration);
        }
    }
}
