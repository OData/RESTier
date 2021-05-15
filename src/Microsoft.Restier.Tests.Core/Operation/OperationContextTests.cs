// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core.Operation
{
    /// <summary>
    /// Unit tests for the <see cref="OperationContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OperationContextTests
    {
        private OperationContext testClass;
        private ApiBase api;
        private Func<string, object> getParameterValueFunc;
        private string operationName;
        private bool isFunction;
        private IEnumerable bindingParameterValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationContextTests"/> class.
        /// </summary>
        public OperationContextTests()
        {
            api = new TestApi(new ServiceProviderMock().ServiceProvider.Object);
            getParameterValueFunc = name => this;
            operationName = "Insert";
            isFunction = true;
            bindingParameterValue = new List<object>();
            testClass = new OperationContext(
                api,
                getParameterValueFunc,
                operationName,
                isFunction,
                bindingParameterValue);
        }

        /// <summary>
        /// Can construct a new <see cref="OperationContext"/>.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new OperationContext(
                api,
                getParameterValueFunc,
                operationName,
                isFunction,
                bindingParameterValue);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with a null Api.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new OperationContext(
                default(ApiBase),
                default(Func<string, object>),
                "TestValue719188563",
                true,
                new Mock<IEnumerable>().Object);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with a null getParameterValueFunc.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullGetParameterValueFunc()
        {
            Action act = () => new OperationContext(
                api,
                default(Func<string, object>),
                "TestValue734278354",
                false,
                new Mock<IEnumerable>().Object);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with a null bindingParameterValue.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullBindingParameterValue()
        {
            Action act = () => new OperationContext(
                api,
                default(Func<string, object>),
                "TestValue715530316",
                true,
                default(IEnumerable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with an invalid OperationName.
        /// </summary>
        /// <param name="value">OperationName.</param>
        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotConstructWithInvalidOperationName(string value)
        {
            Action act = () => new OperationContext(
                api,
                default(Func<string, object>),
                value,
                false,
                new Mock<IEnumerable>().Object);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Test that the Operation name is initialized correctly.
        /// </summary>
        [TestMethod]
        public void OperationNameIsInitializedCorrectly()
        {
            testClass.OperationName.Should().Be(operationName);
        }

        /// <summary>
        /// Tests that the getParameterValueFunc is initialized correctly.
        /// </summary>
        [TestMethod]
        public void GetParameterValueFuncIsInitializedCorrectly()
        {
            testClass.GetParameterValueFunc.Should().Be(getParameterValueFunc);
        }

        /// <summary>
        /// Tests that the isFunction property is initialized correctly.
        /// </summary>
        [TestMethod]
        public void IsFunctionIsInitializedCorrectly()
        {
            testClass.IsFunction.Should().Be(isFunction);
        }

        /// <summary>
        /// Tests that the bindingParameterValue is initialized correctly.
        /// </summary>
        [TestMethod]
        public void BindingParameterValueIsInitializedCorrectly()
        {
            testClass.BindingParameterValue.Should().BeEquivalentTo(bindingParameterValue);
        }

        /// <summary>
        /// Tests that ParameterValues can be set and get.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetParameterValues()
        {
            var testValue = new List<object>();
            testClass.ParameterValues = testValue;
            testClass.ParameterValues.Should().BeEquivalentTo(testValue);
        }

        private class TestApi : ApiBase
        {
            public TestApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }
    }
}