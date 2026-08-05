// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Core.Query
{
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
    using System.Reflection;

    /// <summary>
    /// Query expression context tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class QueryExpressionContextTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;

        private readonly QueryExpressionContext testClass;
        private readonly QueryContext queryContext;
        private readonly MethodInfo testGetQuerableSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryExpressionContextTests"/> class.
        /// </summary>
        public QueryExpressionContextTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();

            var api = new TestApi(model, queryHandler, submitHandler);
            var queryableSource = new QueryableSource<Test>(Expression.Constant(Substitute.For<IQueryable>()));
            var request = new QueryRequest(queryableSource);
            queryContext = new QueryContext(api, request);
            testClass = new QueryExpressionContext(queryContext);
            var type = typeof(DataSourceStub);
            var methodInfo = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.Name == "GetQueryableSource");
            testGetQuerableSource = methodInfo.First().MakeGenericMethod(new Type[] { typeof(Test) });
        }

        /// <summary>
        /// Can construct an instance of the <see cref="QueryExpressionContext"/> class.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new QueryExpressionContext(queryContext);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with a null query context.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullQueryContext()
        {
            Action act = () => new QueryExpressionContext(default(QueryContext));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call PushVisitedNode.
        /// </summary>
        [TestMethod]
        public void CanCallPushVisitedNode()
        {
            var visitedNode = Expression.Constant(Substitute.For<IQueryable>());
            testClass.PushVisitedNode(visitedNode);
            testClass.VisitedNode.Should().Be(visitedNode);
        }

        /// <summary>
        /// Can call PushVisitedNode and update the model reference.
        /// </summary>
        [TestMethod]
        public void CanCallPushVisitedNodeAndUpdateModelReference()
        {
            var visitedNode = Expression.Call(testGetQuerableSource, new Expression[] { Expression.Constant("Test"), Expression.Constant(new object[0]) });
            testClass.PushVisitedNode(visitedNode);
            testClass.ModelReference.Should().NotBeNull();
        }

        // TODO: More tests.

        /*



                [TestMethod]
                public void CanCallReplaceVisitedNode()
                {
                    var visitedNode = new BinaryExpression();
                    testClass.ReplaceVisitedNode(visitedNode);
                    false, "Create or modify test".Should().BeTrue();
                }

                [TestMethod]
                public void CannotCallReplaceVisitedNodeWithNullVisitedNode()
                {
                    Action act = () => testClass.ReplaceVisitedNode(default(Expression)); act.Should().Throw<ArgumentNullException>();
                }

                [TestMethod]
                public void CanCallPopVisitedNode()
                {
                    testClass.PopVisitedNode();
                    false, "Create or modify test".Should().BeTrue();
                }

                [TestMethod]
                public void CanCallGetModelReferenceForNode()
                {
                    var node = new BinaryExpression();
                    var result = testClass.GetModelReferenceForNode(node);
                    false, "Create or modify test".Should().BeTrue();
                }

                [TestMethod]
                public void CannotCallGetModelReferenceForNodeWithNullNode()
                {
                    Action act = () => testClass.GetModelReferenceForNode(default(Expression)); act.Should().Throw<ArgumentNullException>();
                }

                [TestMethod]
                public void GetModelReferenceForNodePerformsMapping()
                {
                    var node = new BinaryExpression();
                    var result = testClass.GetModelReferenceForNode(node);
                    result.Type.Should().Be(node.Type);
                }

                [TestMethod]
                public void QueryContextIsInitializedCorrectly()
                {
                    testClass.QueryContext.Should().Be(queryContext);
                }

                [TestMethod]
                public void CanGetVisitedNode()
                {
                    testClass.VisitedNode.Should().BeOfType<Expression>();
                    false, "Create or modify test".Should().BeTrue();
                }

                [TestMethod]
                public void CanGetModelReference()
                {
                    testClass.ModelReference.Should().BeOfType<QueryModelReference>();
                    false, "Create or modify test".Should().BeTrue();
                }

                [TestMethod]
                public void CanSetAndGetAfterNestedVisitCallback()
                {
                    var testValue = default(Action);
                    testClass.AfterNestedVisitCallback = testValue;
                    testClass.AfterNestedVisitCallback.Should().Be(testValue);
                }
        */
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