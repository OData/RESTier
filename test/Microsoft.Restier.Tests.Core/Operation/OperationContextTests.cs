// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Operation
{
    /// <summary>
    /// Unit tests for the <see cref="OperationContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OperationContextTests
    {
        private OperationContext testClass;
        private TestApi api;
        private Func<string, (bool Present, object Value)> getParameterValueFunc;
        private string operationName;
        private bool isFunction;
        private IEnumerable bindingParameterValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationContextTests"/> class.
        /// </summary>
        public OperationContextTests()
        {
            api = new TestApi(
                Substitute.For<IEdmModel>(),
                Substitute.For<IQueryHandler>(),
                Substitute.For<ISubmitHandler>()); 
            getParameterValueFunc = name => (true, (object)this);
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
        [Fact]
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
        [Fact]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new OperationContext(
                default(ApiBase),
                default(Func<string, (bool Present, object Value)>),
                "TestValue719188563",
                true,
                Substitute.For<IEnumerable>());
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with a null getParameterValueFunc.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullGetParameterValueFunc()
        {
            Action act = () => new OperationContext(
                api,
                default(Func<string, (bool Present, object Value)>),
                "TestValue734278354",
                false,
                Substitute.For<IEnumerable>());
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with a null bindingParameterValue.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullBindingParameterValue()
        {
            Action act = () => new OperationContext(
                api,
                default(Func<string, (bool Present, object Value)>),
                "TestValue715530316",
                true,
                default(IEnumerable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct the <see cref="OperationContext"/> with an invalid OperationName.
        /// </summary>
        /// <param name="value">OperationName.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotConstructWithInvalidOperationName(string value)
        {
            Action act = () => new OperationContext(
                api,
                default(Func<string, (bool Present, object Value)>),
                value,
                false,
                Substitute.For<IEnumerable>());
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Test that the Operation name is initialized correctly.
        /// </summary>
        [Fact]
        public void OperationNameIsInitializedCorrectly()
        {
            testClass.OperationName.Should().Be(operationName);
        }

        /// <summary>
        /// Tests that the getParameterValueFunc is initialized correctly.
        /// </summary>
        [Fact]
        public void GetParameterValueFuncIsInitializedCorrectly()
        {
            testClass.GetParameterValueFunc.Should().Be(getParameterValueFunc);
        }

        /// <summary>
        /// Tests that the isFunction property is initialized correctly.
        /// </summary>
        [Fact]
        public void IsFunctionIsInitializedCorrectly()
        {
            testClass.IsFunction.Should().Be(isFunction);
        }

        /// <summary>
        /// Tests that the bindingParameterValue is initialized correctly.
        /// </summary>
        [Fact]
        public void BindingParameterValueIsInitializedCorrectly()
        {
            testClass.BindingParameterValue.Should().BeEquivalentTo(bindingParameterValue);
        }

        /// <summary>
        /// Tests that ParameterValues can be set and get.
        /// </summary>
        [Fact]
        public void CanSetAndGetParameterValues()
        {
            var testValue = new List<object>();
            testClass.ParameterValues = testValue;
            testClass.ParameterValues.Should().BeEquivalentTo(testValue);
        }

        private class TestApi : ApiBase
        {
            public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }
    }
}