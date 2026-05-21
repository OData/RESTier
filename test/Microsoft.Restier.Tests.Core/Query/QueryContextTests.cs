// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Unit tests for the <see cref="QueryContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]

    [TestClass]
    public class QueryContextTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryContextTests"/> class.
        /// </summary>
        public QueryContextTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
        }

        /// <summary>
        /// Can construct a new QueryContext.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var queryableSource = new QueryableSource<Test>(Expression.Constant(Substitute.For<IQueryable>()));
            var request = new QueryRequest(queryableSource);
            var instance = new QueryContext(api, request);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with a null api.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullApi()
        {
            var queryableSource = new QueryableSource<Test>(Expression.Constant(Substitute.For<IQueryable>()));
            var request = new QueryRequest(queryableSource);
            Action act = () => new QueryContext(
                default(ApiBase),
                request);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct with a null request.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullRequest()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            Action act = () => new QueryContext(api, default(QueryRequest));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can get and set the model.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetModel()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var queryableSource = new QueryableSource<Test>(Expression.Constant(Substitute.For<IQueryable>()));
            var request = new QueryRequest(queryableSource);
            var instance = new QueryContext(api, request);

            var testValue = Substitute.For<IEdmModel>();
            instance.Model = testValue;
            instance.Model.Should().Be(testValue);
        }

        /// <summary>
        /// Request is initialized correctly.
        /// </summary>
        [TestMethod]
        public void RequestIsInitializedCorrectly()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var queryableSource = new QueryableSource<Test>(Expression.Constant(Substitute.For<IQueryable>()));
            var request = new QueryRequest(queryableSource);
            var instance = new QueryContext(api, request);

            instance.Request.Should().Be(request);
        }

        private class TestApi : ApiBase
        {
            public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class Test
        {
            public string Name { get; set; }
        }
    }
}