// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ApiDataTests
    {
        [Fact]
        public void SourceOfEntityContainerElementIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => ApiData.Source<object>("EntitySet"));
        }

        [Fact]
        public void SourceOfComposableFunctionIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => ApiData.Source<object>("Namespace", "Function"));
        }

        [Fact]
        public void ResultsOfEntityContainerElementIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => ApiData.Results<object>("EntitySet"));
        }

        [Fact]
        public void ResultOfEntityContainerElementIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => ApiData.Result<object>("Singleton"));
        }

        [Fact]
        public void ResultsOfComposableFunctionIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => ApiData.Results<object>("Namespace", "Function"));
        }

        [Fact]
        public void ResultOfComposableFunctionIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => ApiData.Result<object>("Namespace", "Function"));
        }

        [Fact]
        public void ValueIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => ApiData.Value<object>(new object(), "Property"));
        }
    }
}
