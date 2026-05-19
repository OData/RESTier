// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using NSubstitute;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Unit tests for the <see cref="QueryRequest"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class QueryRequestTests
    {
        private QueryRequest testClass;
        private IQueryable query = Substitute.For<IQueryable>();

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryRequestTests"/> class.
        /// </summary>
        public QueryRequestTests()
        {
            var query = new QueryableSource<object>(Expression.Constant(this.query));
            testClass = new QueryRequest(query);
        }

        /// <summary>
        /// Can construct.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            testClass.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with null query.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullQuery()
        {
            Action act = () => new QueryRequest(default(IQueryable));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// <summary>
        /// Can set and get the IQueryable.
        /// </summary>
        [Fact]
        public void CanSetAndGetIQuerable()
        {
            var testValue = Substitute.For<IQueryable>();
            testClass.Query = testValue;
            testClass.Query.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get ShouldReturnCount.
        /// </summary>
        [Fact]
        public void CanSetAndGetShouldReturnCount()
        {
            var testValue = true;
            testClass.ShouldReturnCount = testValue;
            testClass.ShouldReturnCount.Should().Be(testValue);
        }

        /// <summary>
        /// HasRecursiveExpand defaults to false.
        /// </summary>
        [Fact]
        public void HasRecursiveExpand_DefaultsToFalse()
        {
            testClass.HasRecursiveExpand.Should().BeFalse();
        }

        /// <summary>
        /// HasRecursiveExpand can be set by internal code (e.g. the controller layer).
        /// </summary>
        [Fact]
        public void HasRecursiveExpand_CanBeSet()
        {
            typeof(QueryRequest)
                .GetProperty(nameof(QueryRequest.HasRecursiveExpand))!
                .SetValue(testClass, true);
            testClass.HasRecursiveExpand.Should().BeTrue();
        }

        /// <summary>
        /// AllowNoTracking defaults to false so the submit pipeline and any
        /// direct (non-controller) QueryAsync call preserves tracked behavior.
        /// </summary>
        [Fact]
        public void AllowNoTracking_DefaultsToFalse()
        {
            testClass.AllowNoTracking.Should().BeFalse();
        }

        /// <summary>
        /// AllowNoTracking can be set by internal code (the AspNetCore controller).
        /// </summary>
        [Fact]
        public void AllowNoTracking_CanBeSet()
        {
            typeof(QueryRequest)
                .GetProperty(nameof(QueryRequest.AllowNoTracking))!
                .SetValue(testClass, true);
            testClass.AllowNoTracking.Should().BeTrue();
        }
    }
}