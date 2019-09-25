// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core
{

    [TestClass]
    public class DataSourceStubsTests : RestierTestBase
    {
        [TestMethod]
        public void SourceOfEntityContainerElementIsNotCallable()
        {
            Action invalidOperation = () => { DataSourceStub.GetQueryableSource<object>("EntitySet"); };
            invalidOperation.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void SourceOfComposableFunctionIsNotCallable()
        {
            Action invalidOperation = () => { DataSourceStub.GetQueryableSource<object>("Namespace", "Function"); };
            invalidOperation.Should().Throw<InvalidOperationException>();
        }

        // TODO enable these when function/action is supported.
        //[TestMethod]
        //public void ResultsOfEntityContainerElementIsNotCallable()
        //{
        //    Assert.Throws<InvalidOperationException>(() => DataSourceStub.Results<object>("EntitySet"));
        //}

        //[TestMethod]
        //public void ResultOfEntityContainerElementIsNotCallable()
        //{
        //    Assert.Throws<InvalidOperationException>(() => DataSourceStub.Result<object>("Singleton"));
        //}

        //[TestMethod]
        //public void ResultsOfComposableFunctionIsNotCallable()
        //{
        //    Assert.Throws<InvalidOperationException>(() => DataSourceStub.Results<object>("Namespace", "Function"));
        //}

        //[TestMethod]
        //public void ResultOfComposableFunctionIsNotCallable()
        //{
        //    Assert.Throws<InvalidOperationException>(() => DataSourceStub.Result<object>("Namespace", "Function"));
        //}

        [TestMethod]
        public void ValueIsNotCallable()
        {
            Action invalidOperation = () => { DataSourceStub.GetPropertyValue<object>(new object(), "Property"); };
            invalidOperation.Should().Throw<InvalidOperationException>();
        }
    }
}
