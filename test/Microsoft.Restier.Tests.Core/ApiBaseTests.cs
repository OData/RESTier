// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ApiBase"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public partial class ApiBaseTests
    {
        public TestContext TestContext { get; set; }

        private TestApiBase testClass;
        DefaultQueryHandler queryHandler;
        DefaultSubmitHandler submitHandler;
        TestModelBuilder modelBuilder = new TestModelBuilder();
        private readonly IChainOfResponsibilityFactory<IQueryExpressionSourcer> _sourcerFactory;
        private readonly IChainOfResponsibilityFactory<IQueryExpressionProcessor> _processorFactory;
        private readonly IChainOfResponsibilityFactory<IQueryExecutor> _executorFactory;
        private readonly IChainOfResponsibilityFactory<IModelMapper> _mapperFactory;
        private readonly IChainOfResponsibilityFactory<IQueryExpressionAuthorizer> _authorizerFactory;
        private readonly IChainOfResponsibilityFactory<IQueryExpressionExpander> _expanderFactory;
        private readonly IChainOfResponsibilityFactory<IChangeSetItemAuthorizer> _changeSetItemAuthorizerFactory;
        private readonly IChainOfResponsibilityFactory<IChangeSetItemValidator> _changesetItemValidatorFactory;
        private readonly IChainOfResponsibilityFactory<IChangeSetItemFilter> _changeSetItemFilterFactory;

        public ApiBaseTests()
        {
            _sourcerFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionSourcer>>();
            _sourcerFactory.Create().Returns(new TestQuerySourcer());
            _processorFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionProcessor>>();
            _processorFactory.Create().Returns(new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi)));
            _executorFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExecutor>>();
            _executorFactory.Create().Returns(new DefaultQueryExecutor());
            _mapperFactory = Substitute.For<IChainOfResponsibilityFactory<IModelMapper>>();
            _mapperFactory.Create().Returns(new TestModelMapper());
            _authorizerFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionAuthorizer>>();
            _authorizerFactory.Create().Returns(default(IQueryExpressionAuthorizer));
            _expanderFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionExpander>>();
            _expanderFactory.Create().Returns(default(IQueryExpressionExpander));


            _changeSetItemAuthorizerFactory = Substitute.For<IChainOfResponsibilityFactory<IChangeSetItemAuthorizer>>();
            _changeSetItemAuthorizerFactory.Create().Returns(new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi)));
            _changesetItemValidatorFactory = Substitute.For<IChainOfResponsibilityFactory<IChangeSetItemValidator>>();
            _changesetItemValidatorFactory.Create().Returns(new ConventionBasedChangeSetItemValidator());
            _changeSetItemFilterFactory = Substitute.For<IChainOfResponsibilityFactory<IChangeSetItemFilter>>();
            _changeSetItemFilterFactory.Create().Returns(new ConventionBasedChangeSetItemFilter(typeof(EmptyApi)));
            queryHandler = new DefaultQueryHandler(
                _sourcerFactory,
                _executorFactory,
                _mapperFactory,
                _authorizerFactory,
                _expanderFactory,
                _processorFactory
                );
            submitHandler = new DefaultSubmitHandler(
                new DefaultChangeSetInitializer(),
                new DefaultSubmitExecutor(),
                _changeSetItemAuthorizerFactory,
                _changesetItemValidatorFactory,
                _changeSetItemFilterFactory);
            testClass = new TestApiBase(modelBuilder.GetEdmModel(), queryHandler, submitHandler);
        }

        /// <summary>
        /// Cannot construct with a null model.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullModel()
        {
            Action act = () => new TestApiBase(default(IEdmModel), queryHandler, submitHandler);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct with a null query handler.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullQueryHandler()
        {
            Action act = () => new TestApiBase(modelBuilder.GetEdmModel(), default(IQueryHandler), submitHandler);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot construct with a null submit handler.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullSubmitHandler()
        {
            Action act = () => new TestApiBase(modelBuilder.GetEdmModel(), queryHandler, default(ISubmitHandler));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call SubmitAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallSubmitAsync()
        {
            var changeSetItemAuthorizer = Substitute.For<IChangeSetItemAuthorizer>();
            var changeSetItemValidator = Substitute.For<IChangeSetItemValidator>();
            var changeSetItemFilter = Substitute.For<IChangeSetItemFilter>();
            _changeSetItemAuthorizerFactory.Create().Returns(changeSetItemAuthorizer);
            _changesetItemValidatorFactory.Create().Returns(changeSetItemValidator);
            _changeSetItemFilterFactory.Create().Returns(changeSetItemFilter);

            submitHandler = new DefaultSubmitHandler(
                new DefaultChangeSetInitializer(),
                new DefaultSubmitExecutor(),
                _changeSetItemAuthorizerFactory,
                _changesetItemValidatorFactory,
                _changeSetItemFilterFactory);

            var changeSet = new ChangeSet();
            changeSet.Entries.Enqueue(
                new DataModificationItem(
                    "Tests",
                    typeof(Test),
                    typeof(Test),
                    RestierEntitySetOperation.Update,
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>()));
            var cancellationToken = CancellationToken.None;

            bool authCalled = false;

            // check for authorizer invocation.
            changeSetItemAuthorizer
                .AuthorizeAsync(Arg.Any<SubmitContext>(), Arg.Any<ChangeSetItem>(), Arg.Any<CancellationToken>())
                .Returns(async call =>
                {
                    authCalled = true;
                    return await Task.FromResult(authCalled);
                });

            bool preFilterCalled = false;
            bool postFilterCalled = false;

            // check for filter invocation.
            changeSetItemFilter
                .OnChangeSetItemProcessingAsync(Arg.Any<SubmitContext>(), Arg.Any<ChangeSetItem>(), Arg.Any<CancellationToken>())
                .Returns(async call =>
                {
                    preFilterCalled = true;
                    await Task.CompletedTask;
                });

            changeSetItemFilter
                .OnChangeSetItemProcessedAsync(Arg.Any<SubmitContext>(), Arg.Any<ChangeSetItem>(), Arg.Any<CancellationToken>())
                .Returns(async call =>
                {
                    postFilterCalled = true;
                    await Task.CompletedTask;
                });

            bool validationCalled = false;

            // check for validator invocation.
            changeSetItemValidator
                .ValidateChangeSetItemAsync(
                    Arg.Any<SubmitContext>(),
                    Arg.Any<ChangeSetItem>(),
                    Arg.Any<Collection<ChangeSetItemValidationResult>>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    validationCalled = true;
                    return Task.FromResult(authCalled);
                });

            testClass = new TestApiBase(modelBuilder.GetEdmModel(), queryHandler, submitHandler);
            var result = await testClass.SubmitAsync(changeSet, cancellationToken);
            authCalled.Should().BeTrue("AuthorizeAsync was not called");
            preFilterCalled.Should().BeTrue("OnChangeSetItemProcessingAsync was not called");
            postFilterCalled.Should().BeTrue("OnChangeSetItemProcessedAsync was not called");
            validationCalled.Should().BeTrue("ValidateChangeSetItemAsync was not called");
        }

        /// <summary>
        /// Can call SubmitAsync with unprocessed results. They should be returned immediately.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallSubmitAsyncWithUnprocessedResults()
        {
            var changeSetItemAuthorizer = Substitute.For<IChangeSetItemAuthorizer>();
            var changeSetItemValidator = Substitute.For<IChangeSetItemValidator>();
            var changeSetItemFilter = Substitute.For<IChangeSetItemFilter>();
            var changeSetInitializer = Substitute.For<IChangeSetInitializer>();
            _changeSetItemAuthorizerFactory.Create().Returns(changeSetItemAuthorizer);
            _changesetItemValidatorFactory.Create().Returns(changeSetItemValidator);
            _changeSetItemFilterFactory.Create().Returns(changeSetItemFilter);

            submitHandler = new DefaultSubmitHandler(
                changeSetInitializer,
                new DefaultSubmitExecutor(),
                _changeSetItemAuthorizerFactory,
                _changesetItemValidatorFactory,
                _changeSetItemFilterFactory);

            var changeSet = new ChangeSet();
            var cancellationToken = CancellationToken.None;
            var submitResult = new SubmitResult(changeSet);

            // Setup changeSetInitializer to produce a result immediately.
            changeSetInitializer
                .InitializeAsync(Arg.Any<SubmitContext>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var context = call.Arg<SubmitContext>();
                    context.Result = submitResult;
                    return Task.CompletedTask;
                });

            testClass = new TestApiBase(modelBuilder.GetEdmModel(), queryHandler, submitHandler);
            var result = await testClass.SubmitAsync(changeSet, cancellationToken);
            result.Should().Be(submitResult);
        }

        /// <summary>
        /// Can call Dispose with no parameters.
        /// </summary>
        [TestMethod]
        public void CanCallDisposeWithNoParameters()
        {
            testClass.Dispose();
            testClass.Disposed.Should().BeTrue("ApiBase instance is not disposed.");
        }

        [TestMethod]
        public void DefaultApiBaseCanBeCreatedAndDisposed()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);

            Action exceptionTest = () => { api.Dispose(); };
            exceptionTest.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void GetQueryableSource_EntitySet_IsConfiguredCorrectly()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];
            var source = api.GetQueryableSource("Test", arguments);

            CheckQueryable(source, typeof(string), new List<string> { "Test" }, arguments);
        }
        [TestMethod]
        public void GetQueryableSource_OfT_EntitySet_IsConfiguredCorrectly()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];
            var source = api.GetQueryableSource<string>("Test", arguments);

            CheckQueryable(source, typeof(string), new List<string> { "Test" }, arguments);
        }

        [TestMethod]
        public void GetQueryableSource_EntitySet_ThrowsIfNotMapped()
        {
            _sourcerFactory.Create().Returns(new TestQuerySourcer());

            _processorFactory.Create().Returns(new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi)));
            _executorFactory.Create().Returns(new DefaultQueryExecutor());
            _mapperFactory.Create().Returns(Substitute.For<IModelMapper>());

            queryHandler = new DefaultQueryHandler(
                _sourcerFactory,
                _executorFactory,
                _mapperFactory,
               _authorizerFactory,
               _expanderFactory,
               _processorFactory
               );
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource("Test", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void GetQueryableSource_OfT_ContainerElementThrowsIfWrongType()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<object>("Test", arguments); };
            exceptionTest.Should().Throw<ArgumentException>();

        }

        [TestMethod]
        public void GetQueryableSource_ComposableFunction_IsConfiguredCorrectly()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];
            var source = api.GetQueryableSource("Namespace", "Function", arguments);

            CheckQueryable(source, typeof(DateTime), new List<string> { "Namespace", "Function" }, arguments);
        }

        [TestMethod]
        public void GetQueryableSource_OfT_ComposableFunction_IsConfiguredCorrectly()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];
            var source = api.GetQueryableSource<DateTime>("Namespace", "Function", arguments);

            CheckQueryable(source, typeof(DateTime), new List<string> { "Namespace", "Function" }, arguments);
        }

        [TestMethod]
        public void GetQueryableSource_ComposableFunction_ThrowsIfNotMapped()
        {
            _sourcerFactory.Create().Returns(new TestQuerySourcer());

            _processorFactory.Create().Returns(new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi)));
            _executorFactory.Create().Returns(new DefaultQueryExecutor());
            _mapperFactory.Create().Returns(Substitute.For<IModelMapper>());

            queryHandler = new DefaultQueryHandler(
                _sourcerFactory,
                _executorFactory,
                _mapperFactory,
               _authorizerFactory,
               _expanderFactory,
               _processorFactory
               );
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void GetQueryableSource_OfT_ComposableFunction_ThrowsIfNotMapped()
        {
            _sourcerFactory.Create().Returns(new TestQuerySourcer());
            _processorFactory.Create().Returns(new ConventionBasedQueryExpressionProcessor(typeof(EmptyApi)));
            _executorFactory.Create().Returns(new DefaultQueryExecutor());
            _mapperFactory.Create().Returns(Substitute.For<IModelMapper>());

            queryHandler = new DefaultQueryHandler(
                _sourcerFactory,
                _executorFactory,
                _mapperFactory,
               _authorizerFactory,
               _expanderFactory,
               _processorFactory
               );
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<DateTime>("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void GetQueryableSource_ComposableFunction_ThrowsIfWrongType()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<object>("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public async Task QueryAsync_WithQueryReturnsResults()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);

            var request = new QueryRequest(api.GetQueryableSource<string>("Test"));
            var result = await api.QueryAsync(request, TestContext.CancellationTokenSource.Token);
            var results = result.Results.Cast<string>();

            results.SequenceEqual(new[] { "Test" }).Should().BeTrue();
        }

        [TestMethod]
        public async Task QueryAsync_CorrectlyForwardsCall()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var queryRequest = new QueryRequest(api.GetQueryableSource<string>("Test"));
            var queryResult = await api.QueryAsync(queryRequest, TestContext.CancellationTokenSource.Token);

            queryResult.Results.Cast<string>().SequenceEqual(new[] { "Test" }).Should().BeTrue();
        }

        [TestMethod]
        public async Task SubmitAsync_CorrectlyForwardsCall()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var submitResult = await api.SubmitAsync(cancellationToken: TestContext.CancellationTokenSource.Token);

            submitResult.CompletedChangeSet.Should().NotBeNull();
        }

        [TestMethod]
        public void GetQueryableSource_CannotEnumerate()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.GetEnumerator(); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void GetQueryableSource_CannotEnumerateIEnumerable()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { (source as IEnumerable).GetEnumerator(); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void GetQueryableSource_ProviderCannotGenericExecute()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.Provider.Execute<string>(null); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void GetQueryableSource_ProviderCannotExecute()
        {
            var model = modelBuilder.GetEdmModel();
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.Provider.Execute(null); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        /// <summary>
        /// Runs a set of checks against an IQueryable to make sure it has been processed properly.
        /// </summary>
        /// <param name="source">The <see cref="IQueryable{T}"/> or <see cref="IQueryable"/> to test.</param>
        /// <param name="elementType">The <see cref="Type"/> returned by the <paramref name="source"/>.</param>
        /// <param name="expressionValues">A <see cref="List{string}"/> containing the parts of the expression to check for.</param>
        /// <param name="arguments">An array of arguments that the <see cref="IQueryable"/> we're testing requires. RWM: In the tests, this is an empty array. Not sure if that is v alid or not.</param>
        private void CheckQueryable(IQueryable source, Type elementType, List<string> expressionValues, object[] arguments)
        {
            source.ElementType.Should().Be(elementType);
            (source.Expression is MethodCallExpression).Should().BeTrue();
            var methodCall = source.Expression as MethodCallExpression;
            methodCall.Object.Should().BeNull();
            methodCall.Method.DeclaringType.Should().Be(typeof(DataSourceStub));
            methodCall.Method.Name.Should().Be("GetQueryableSource");
            methodCall.Method.GetGenericArguments()[0].Should().Be(elementType);
            methodCall.Arguments.Should().HaveCount(expressionValues.Count + 1);

            for (var i = 0; i < expressionValues.Count; i++)
            {
                (methodCall.Arguments[i] is ConstantExpression).Should().BeTrue();
                (methodCall.Arguments[i] as ConstantExpression).Value.Should().Be(expressionValues[i]);
                source.ToString().Should().Be(source.Expression.ToString());
            }

            (methodCall.Arguments[expressionValues.Count] is ConstantExpression).Should().BeTrue();
            (methodCall.Arguments[expressionValues.Count] as ConstantExpression).Value.Should().Be(arguments);
            source.ToString().Should().Be(source.Expression.ToString());

        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class TestModelBuilder : IModelBuilder
        {
            /// <inheritdoc />
            public IModelBuilder Inner { get; set; }

            public IEdmModel GetEdmModel()
            {
                var model = new EdmModel();
                var dummyType = new EdmEntityType("NS", "Dummy");
                model.AddElement(dummyType);
                var container = new EdmEntityContainer("NS", "DefaultContainer");
                container.AddEntitySet("Test", dummyType);
                model.AddElement(container);
                return model;
            }
        }

        private class TestModelMapper : IModelMapper
        {
            /// <inheritdoc />
            public IModelMapper Inner { get; set; }

            public bool TryGetRelevantType(InvocationContext context, string name, out Type relevantType)
            {
                relevantType = typeof(string);
                return true;
            }

            public bool TryGetRelevantType(InvocationContext context, string namespaceName, string name, out Type relevantType)
            {
                relevantType = typeof(DateTime);
                return true;
            }
        }

        private class TestQuerySourcer : IQueryExpressionSourcer
        {
            /// <summary>
            /// Gets or sets the inner handler.
            /// </summary>
            public IQueryExpressionSourcer Inner { get; set; }

            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                return Expression.Constant(new[] { "Test" }.AsQueryable());
            }
        }

        private class TestApiBase : ApiBase
        {
            public TestApiBase(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }

            public bool Disposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        private class Test
        {
            public string Name { get; set; }
        }
    }
}