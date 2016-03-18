// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class DataSourceStubsTests
    {
        [Fact]
        public void SourceOfEntityContainerElementIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DataSourceStubs.Source<object>("EntitySet"));
        }

        [Fact]
        public void SourceOfComposableFunctionIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DataSourceStubs.Source<object>("Namespace", "Function"));
        }

        [Fact]
        public void ResultsOfEntityContainerElementIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DataSourceStubs.Results<object>("EntitySet"));
        }

        [Fact]
        public void ResultOfEntityContainerElementIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DataSourceStubs.Result<object>("Singleton"));
        }

        [Fact]
        public void ResultsOfComposableFunctionIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DataSourceStubs.Results<object>("Namespace", "Function"));
        }

        [Fact]
        public void ResultOfComposableFunctionIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DataSourceStubs.Result<object>("Namespace", "Function"));
        }

        [Fact]
        public void ValueIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DataSourceStubs.Value<object>(new object(), "Property"));
        }
    }
}
