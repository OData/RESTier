// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedQueryExpressionProcessor"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ConventionBasedQueryExpressionProcessorTests
    {
        private readonly IServiceProvider serviceProvider;
        private readonly TestTraceListener testTraceListener = new TestTraceListener();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedQueryExpressionProcessorTests"/> class.
        /// </summary>
        public ConventionBasedQueryExpressionProcessorTests()
        {
            var serviceProviderFixture = new ServiceProviderMock();
            serviceProvider = serviceProviderFixture.ServiceProvider.Object;
            Type type = typeof(Test);
            serviceProviderFixture.ModelMapper
                .Setup(x => x.TryGetRelevantType(It.IsAny<ModelContext>(), It.IsAny<string>(), out type))
                .Returns(true);
            Trace.Listeners.Add(testTraceListener);
        }

        /// <summary>
        /// Checks that we can construct the <see cref="ConventionBasedQueryExpressionProcessor"/> class.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi));
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Checks that we cannot construct ConventionBasedQueryExpressionProcessor with a null api type.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullTargetType()
        {
            Action act = () => new ConventionBasedQueryExpressionProcessor(default(Type));
            act.Should().Throw<ArgumentNullException>();
        }

        // TODO: more testing.
        /*
                [TestMethod]
                public void CanCallProcess()
                {
                    var context = new QueryExpressionContext(new QueryContext(new ApiBase(new Mock<IServiceProvider>().Object), new QueryRequest(new Mock<IQueryable>().Object)));
                    var result = _testClass.Process(context);
                    false, "Create or modify test".Should().BeTrue();
                }
        */

        /// <summary>
        /// Checks that processing by the inner processor will bypass the current one.
        /// </summary>
        [TestMethod]
        public void InnerProcessorShortCircuits()
        {
            var api = new QueryFilterApi(serviceProvider);
            var instance = new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi));
            var queryable = api.GetQueryableSource("Tests");
            var queryRequest = new QueryRequest(queryable);
            var queryContext = new QueryContext(api, queryRequest);
            var queryExpressionContext = new QueryExpressionContext(queryContext);
            var processorMock = new Mock<IQueryExpressionProcessor>();
            var expression = Expression.Constant(42);
            processorMock.Setup(x => x.Process(queryExpressionContext)).Returns(expression);
            instance.Inner = processorMock.Object;

            var result = instance.Process(queryExpressionContext);

            result.Should().Be(expression);
        }

        // TODO: More tests.

        /// <summary>
        /// Cannot call the Process method with a null context.
        /// </summary>
        [TestMethod]
        public void CannotCallProcessWithNullContext()
        {
            var instance = new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi));
            Action act = () => instance.Process(default(QueryExpressionContext));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can get and set the Inner property.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetInner()
        {
            var instance = new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi));
            var testValue = new Mock<IQueryExpressionProcessor>().Object;
            instance.Inner = testValue;
            instance.Inner.Should().Be(testValue);
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }

        private class QueryFilterApi : ApiBase
        {
            public QueryFilterApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }

        private class Test
        {
        }
    }
}