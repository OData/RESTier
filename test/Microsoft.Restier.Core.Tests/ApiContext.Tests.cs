﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
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
        }

        [Fact]
        public void NewApiContextIsConfiguredCorrectly()
        {
            var api = new TestApi();
            var context = api.Context;
            Assert.NotNull(context.Configuration);
        }
    }
}
