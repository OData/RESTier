// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class DomainDataTests
    {
        [Fact]
        public void SourceOfEntityContainerElementIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DomainData.Source<object>("EntitySet"));
        }

        [Fact]
        public void SourceOfComposableFunctionIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DomainData.Source<object>("Namespace", "Function"));
        }

        [Fact]
        public void ResultsOfEntityContainerElementIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DomainData.Results<object>("EntitySet"));
        }

        [Fact]
        public void ResultOfEntityContainerElementIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DomainData.Result<object>("Singleton"));
        }

        [Fact]
        public void ResultsOfComposableFunctionIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DomainData.Results<object>("Namespace", "Function"));
        }

        [Fact]
        public void ResultOfComposableFunctionIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DomainData.Result<object>("Namespace", "Function"));
        }

        [Fact]
        public void ValueIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DomainData.Value<object>(new object(), "Property"));
        }
    }
}
