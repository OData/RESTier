// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Domain.Tests
{
    [TestClass]
    public class DomainDataTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SourceOfEntityContainerElementIsNotCallable()
        {
            DomainData.Source<object>("EntitySet");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SourceOfComposableFunctionIsNotCallable()
        {
            DomainData.Source<object>("Namespace", "Function");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ResultsOfEntityContainerElementIsNotCallable()
        {
            DomainData.Results<object>("EntitySet");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ResultOfEntityContainerElementIsNotCallable()
        {
            DomainData.Result<object>("Singleton");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ResultsOfComposableFunctionIsNotCallable()
        {
            DomainData.Results<object>("Namespace", "Function");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ResultOfComposableFunctionIsNotCallable()
        {
            DomainData.Result<object>("Namespace", "Function");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ValueIsNotCallable()
        {
            DomainData.Value<object>(new object(), "Property");
        }
    }
}
