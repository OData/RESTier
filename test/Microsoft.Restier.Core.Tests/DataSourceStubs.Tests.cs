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
            Assert.Throws<InvalidOperationException>(() => DataSourceStub.GetQueryableSource<object>("EntitySet"));
        }

        [Fact]
        public void SourceOfComposableFunctionIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DataSourceStub.GetQueryableSource<object>("Namespace", "Function"));
        }

        // TODO enable these when function/action is supported.
        //[Fact]
        //public void ResultsOfEntityContainerElementIsNotCallable()
        //{
        //    Assert.Throws<InvalidOperationException>(() => DataSourceStub.Results<object>("EntitySet"));
        //}

        //[Fact]
        //public void ResultOfEntityContainerElementIsNotCallable()
        //{
        //    Assert.Throws<InvalidOperationException>(() => DataSourceStub.Result<object>("Singleton"));
        //}

        //[Fact]
        //public void ResultsOfComposableFunctionIsNotCallable()
        //{
        //    Assert.Throws<InvalidOperationException>(() => DataSourceStub.Results<object>("Namespace", "Function"));
        //}

        //[Fact]
        //public void ResultOfComposableFunctionIsNotCallable()
        //{
        //    Assert.Throws<InvalidOperationException>(() => DataSourceStub.Result<object>("Namespace", "Function"));
        //}

        [Fact]
        public void ValueIsNotCallable()
        {
            Assert.Throws<InvalidOperationException>(() => DataSourceStub.GetPropertyValue<object>(new object(), "Property"));
        }
    }
}
