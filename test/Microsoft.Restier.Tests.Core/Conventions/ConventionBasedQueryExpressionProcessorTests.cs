// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedQueryExpressionProcessor"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ConventionBasedQueryExpressionProcessorTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;
        private readonly TestTraceListener testTraceListener = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedQueryExpressionProcessorTests"/> class.
        /// </summary>
        public ConventionBasedQueryExpressionProcessorTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();

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
        /// Checks that processing by the inner processorFactory will bypass the current one.
        /// </summary>
        [TestMethod]
        public void InnerProcessorShortCircuits()
        {
            queryHandler.EnsureElementType(Arg.Any<InvocationContext>(), null, "Tests").Returns(typeof(Test));
            var api = new QueryFilterApi(model, queryHandler, submitHandler);
            var instance = new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi));
            var queryable = api.GetQueryableSource("Tests");
            var queryRequest = new QueryRequest(queryable);
            var queryContext = new QueryContext(api, queryRequest);
            var queryExpressionContext = new QueryExpressionContext(queryContext);
            var processor = Substitute.For<IQueryExpressionProcessor>();
            var expression = Expression.Constant(42);
            processor.Process(queryExpressionContext).Returns(expression);
            instance.Inner = processor;

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
            var testValue = Substitute.For<IQueryExpressionProcessor>();
            instance.Inner = testValue;
            instance.Inner.Should().Be(testValue);
        }

        /// <summary>
        /// Verifies that when a DataSourceStubModelReference resolves to an unbound
        /// IEdmFunctionImport returning Collection(ComplexType) — the keyless-view shape —
        /// Process routes through the shared OnFilter pipeline, the convention method is
        /// invoked, and a non-null filtered expression is returned. This is the
        /// function-import counterpart to the IEdmEntitySet branch.
        /// </summary>
        [TestMethod]
        public void Process_FunctionImportReturningComplexCollection_InvokesOnFilterConvention()
        {
            // Arrange — EDM scaffold: one ComplexType + one function import returning Collection(ComplexType).
            var edmModel = new EdmModel();
            var complexType = new EdmComplexType("TestNs", "FakeView");
            complexType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32);
            edmModel.AddElement(complexType);
            edmModel.SetAnnotationValue(complexType, new ClrTypeAnnotation(typeof(FakeView)));

            var returnTypeRef = new EdmCollectionTypeReference(
                new EdmCollectionType(new EdmComplexTypeReference(complexType, isNullable: true)));
            var function = new EdmFunction(
                "TestNs.Views",
                "FakeView",
                returnTypeRef,
                isBound: false,
                entitySetPathExpression: null,
                isComposable: false);
            edmModel.AddElement(function);

            var container = new EdmEntityContainer("TestNs", "Container");
            container.AddFunctionImport("FakeView", function);
            edmModel.AddElement(container);

            // Arrange — API + query context.
            var api = new FunctionImportFilterApi(edmModel, queryHandler, submitHandler);
            var queryable = new[] { new FakeView { Id = 1 } }.AsQueryable();
            var queryRequest = new QueryRequest(queryable);
            var queryContext = new QueryContext(api, queryRequest)
            {
                Model = edmModel,
            };
            var queryExpressionContext = new QueryExpressionContext(queryContext);

            // Build a MethodCallExpression for DataSourceStub.GetQueryableSource<FakeView>("FakeView", new object[0])
            // so that QueryExpressionContext.ModelReference resolves to a DataSourceStubModelReference
            // whose Element is the function import.
            var getQueryableSource = typeof(DataSourceStub)
                .GetMethods()
                .Single(m => m.Name == nameof(DataSourceStub.GetQueryableSource)
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(FakeView));

            var stubCall = Expression.Call(
                null,
                getQueryableSource,
                Expression.Constant("FakeView"),
                Expression.Constant(Array.Empty<object>(), typeof(object[])));
            queryExpressionContext.PushVisitedNode(stubCall);

            var processor = new ConventionBasedQueryExpressionProcessor(typeof(FunctionImportFilterApi));

            // Act
            var result = processor.Process(queryExpressionContext);

            // Assert — the function-import branch fired the OnFilterFakeView convention method.
            result.Should().NotBeNull();
            api.OnFilterCallCount.Should().Be(1);
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class QueryFilterApi : ApiBase
        {
            public QueryFilterApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class FunctionImportFilterApi : ApiBase
        {
            public FunctionImportFilterApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }

            public int OnFilterCallCount { get; private set; }

            protected internal IQueryable<FakeView> OnFilterFakeView(IQueryable<FakeView> source)
            {
                OnFilterCallCount++;
                return source.Where(v => v.Id > 0);
            }
        }

        private class FakeView
        {
            public int Id { get; set; }
        }

        private class Test
        {
        }
    }
}