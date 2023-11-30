// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Core.Submit
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Unit tests for <see cref="SubmitResult"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SubmitResultTests
    {
        private SubmitResult testClass;
        private Exception exception;
        private ChangeSet completedChangeSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitResultTests"/> class.
        /// </summary>
        public SubmitResultTests()
        {
            exception = new Exception();
            completedChangeSet = new ChangeSet();
            testClass = new SubmitResult(exception);
        }

        /// <summary>
        /// Can construct a new Submit result.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new SubmitResult(exception);
            instance.Should().NotBeNull();
            instance = new SubmitResult(completedChangeSet);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with a null exception.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullException()
        {
            Action act = () => new SubmitResult(default(Exception));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct with a null completed changeset.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullCompletedChangeSet()
        {
            Action act = () => new SubmitResult(default(ChangeSet));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Exception is initialized correctly.
        /// </summary>
        [TestMethod]
        public void ExceptionIsInitializedCorrectly()
        {
            testClass.Exception.Should().Be(exception);
        }

        /// <summary>
        /// Can get and set Exception.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetException()
        {
            var testValue = new Exception();
            testClass.Exception = testValue;
            testClass.Exception.Should().Be(testValue);
        }

        /// <summary>
        /// Setting the exception resets the completed changeset.
        /// </summary>
        [TestMethod]
        public void ExceptionResetsCompletedChangeSet()
        {
            testClass.CompletedChangeSet = new ChangeSet();
            var testValue = new Exception();
            testClass.Exception = testValue;
            testClass.CompletedChangeSet.Should().BeNull();
        }

        /// <summary>
        /// CompletedChangeSet is initialized.
        /// </summary>
        [TestMethod]
        public void CompletedChangeSetIsInitializedCorrectly()
        {
            testClass = new SubmitResult(completedChangeSet);
            testClass.CompletedChangeSet.Should().Be(completedChangeSet);
        }

        /// <summary>
        /// Can get and set completed Changeset.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetCompletedChangeSet()
        {
            var testValue = new ChangeSet();
            testClass.CompletedChangeSet = testValue;
            testClass.CompletedChangeSet.Should().Be(testValue);
        }

        /// <summary>
        /// Setting the completed changeset resets the Exception.
        /// </summary>
        [TestMethod]
        public void CompletedChangeSetResetsException()
        {
            var testValue = new Exception();
            testClass.Exception = testValue;
            testClass.CompletedChangeSet = new ChangeSet();
            testClass.Exception.Should().BeNull();
        }
    }
}