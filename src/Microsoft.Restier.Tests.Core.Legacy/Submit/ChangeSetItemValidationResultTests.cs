// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using FluentAssertions;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core.Submit
{

    /// <summary>
    /// Unit tests for the <see cref="ChangeSetItemValidationResult"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ChangeSetItemValidationResultTests
    {
        private ChangeSetItemValidationResult testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetItemValidationResultTests"/> class.
        /// </summary>
        public ChangeSetItemValidationResultTests()
        {
            testClass = new ChangeSetItemValidationResult();
        }

        /// <summary>
        /// Can construct an instance.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new ChangeSetItemValidationResult();
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can call the ToString() method.
        /// </summary>
        [TestMethod]
        public void CanCallToString()
        {
            testClass.Message = "Lorem ipsum";
            var result = testClass.ToString();
            result.Should().Be(testClass.Message);
        }

        /// <summary>
        /// Can get and set the Validator type.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetValidatorType()
        {
            var testValue = "TestValue1505985619";
            testClass.ValidatorType = testValue;
            testClass.ValidatorType.Should().Be(testValue);
        }

        /// <summary>
        /// Can get and set the target.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetTarget()
        {
            var testValue = new object();
            testClass.Target = testValue;
            testClass.Target.Should().Be(testValue);
        }

        /// <summary>
        /// Can get and set the property name.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetPropertyName()
        {
            var testValue = "TestValue595224707";
            testClass.PropertyName = testValue;
            testClass.PropertyName.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get the severity.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetSeverity()
        {
            var testValue = EventLevel.Informational;
            testClass.Severity = testValue;
            testClass.Severity.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get the message.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetMessage()
        {
            var testValue = "TestValue2070305587";
            testClass.Message = testValue;
            testClass.Message.Should().Be(testValue);
        }
    }
}