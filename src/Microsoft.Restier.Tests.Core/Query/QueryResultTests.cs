// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Unit tests for the <see cref="QueryResult"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class QueryResultTests
    {
        private QueryResult testClass;
        private Exception exception;
        private IEnumerable results;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryResultTests"/> class.
        /// </summary>
        public QueryResultTests()
        {
            exception = new Exception();
            results = new Mock<IEnumerable>().Object;
            testClass = new QueryResult(results);
        }

        /// <summary>
        /// Can construct the instance.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new QueryResult(exception);
            instance.Should().NotBeNull();
            instance = new QueryResult(results);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with a null exception argument.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullException()
        {
            Action act = () => new QueryResult(default(Exception));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct with a null results argument.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullResults()
        {
            Action act = () => new QueryResult(default(IEnumerable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Exception argument is initialized correctly.
        /// </summary>
        [TestMethod]
        public void ExceptionIsInitializedCorrectly()
        {
            var instance = new QueryResult(exception);
            instance.Exception.Should().Be(exception);
        }

        /// <summary>
        /// Can get and set the exception.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetException()
        {
            var testValue = new Exception();
            testClass.Exception = testValue;
            testClass.Exception.Should().Be(testValue);
        }

        /// <summary>
        /// Can get and set the results source.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetResultsSource()
        {
            var testValue = new Mock<IEdmEntitySet>().Object;
            testClass.ResultsSource = testValue;
            testClass.ResultsSource.Should().Be(testValue);
        }

        /// <summary>
        /// Results is initialized correctly.
        /// </summary>
        [TestMethod]
        public void ResultsIsInitializedCorrectly()
        {
            testClass = new QueryResult(results);
            testClass.Results.Should().BeSameAs(results);
        }

        /// <summary>
        /// Can set and get results.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetResults()
        {
            var testValue = new Mock<IEnumerable>().Object;
            testClass.Results = testValue;
            testClass.Results.Should().BeSameAs(testValue);
        }
    }
}