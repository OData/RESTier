// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Core.Query
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// Query expression context tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class QueryExpressionContextTests
    {
        private readonly ServiceProviderMock serviceProviderFixture;
        private readonly QueryExpressionContext testClass;
        private readonly QueryContext queryContext;
        private readonly MethodInfo testGetQuerableSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryExpressionContextTests"/> class.
        /// </summary>
        public QueryExpressionContextTests()
        {
            serviceProviderFixture = new ServiceProviderMock();
            var api = new TestApi(serviceProviderFixture.ServiceProvider.Object);
            var queryableSource = new QueryableSource<Test>(Expression.Constant(new Mock<IQueryable>().Object));
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
            var visitedNode = Expression.Constant(new Mock<IQueryable>().Object);
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
            public TestApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }

        private class Test
        {
            public string Name { get; set; }
        }
    }
}