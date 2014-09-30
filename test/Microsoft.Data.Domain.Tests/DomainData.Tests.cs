// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
