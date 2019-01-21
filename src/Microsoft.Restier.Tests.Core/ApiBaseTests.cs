// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core
{

    [TestClass]
    public class ApiBaseTests : RestierTestBase
    {

        [TestMethod]
        public async Task DefaultApiBaseCanBeCreatedAndDisposed()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>();

            Action exceptionTest = () => { api.Dispose(); };
            exceptionTest.Should().NotThrow<Exception>();
        }

        #region EntitySets

        [TestMethod]
        public async Task GetQueryableSource_EntitySet_IsConfiguredCorrectly()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var arguments = new object[0];
            var source = api.GetQueryableSource("Test", arguments);

            CheckQueryable(source, typeof(string), new List<string> { "Test" }, arguments);
        }

        [TestMethod]
        public async Task GetQueryableSource_OfT_EntitySet_IsConfiguredCorrectly()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var arguments = new object[0];
            var source = api.GetQueryableSource<string>("Test", arguments);

            CheckQueryable(source, typeof(string), new List<string> { "Test" }, arguments);
        }

        [TestMethod]
        public async Task GetQueryableSource_EntitySet_ThrowsIfNotMapped()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApiEmpty>() as ApiBase;
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource("Test", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public async Task GetQueryableSource_OfT_ContainerElementThrowsIfWrongType()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<object>("Test", arguments); };
            exceptionTest.Should().Throw<ArgumentException>();

        }

        #endregion

        #region Functions

        [TestMethod]
        public async Task GetQueryableSource_ComposableFunction_IsConfiguredCorrectly()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var arguments = new object[0];
            var source = api.GetQueryableSource("Namespace", "Function", arguments);

            CheckQueryable(source, typeof(DateTime), new List<string> { "Namespace", "Function" }, arguments);
        }

        [TestMethod]
        public async Task GetQueryableSource_OfT_ComposableFunction_IsConfiguredCorrectly()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var arguments = new object[0];
            var source = api.GetQueryableSource<DateTime>("Namespace", "Function", arguments);

            CheckQueryable(source, typeof(DateTime), new List<string> { "Namespace", "Function" }, arguments);
        }

        [TestMethod]
        public async Task GetQueryableSource_ComposableFunction_ThrowsIfNotMapped()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApiEmpty>() as ApiBase;
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public async Task GetQueryableSource_OfT_ComposableFunction_ThrowsIfNotMapped()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApiEmpty>() as ApiBase;
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<DateTime>("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public async Task GetQueryableSource_ComposableFunction_ThrowsIfWrongType()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var arguments = new object[0];

            Action exceptionTest = () => { api.GetQueryableSource<object>("Namespace", "Function", arguments); };
            exceptionTest.Should().Throw<ArgumentException>();

        }

        #endregion

        #region QueryAsync

        [TestMethod]
        public async Task QueryAsync_WithQueryReturnsResults()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;

            var request = new QueryRequest(api.GetQueryableSource<string>("Test"));
            var result = await api.QueryAsync(request);
            var results = result.Results.Cast<string>();

            results.SequenceEqual(new[] {"Test"}).Should().BeTrue();
        }

        [TestMethod]
        public async Task QueryAsync_CorrectlyForwardsCall()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var queryRequest = new QueryRequest(api.GetQueryableSource<string>("Test"));
            var queryResult = await api.QueryAsync(queryRequest);

            queryResult.Results.Cast<string>().SequenceEqual(new[] { "Test" }).Should().BeTrue();
        }

        #endregion

        #region SubmitAsync

        [TestMethod]
        public async Task SubmitAsync_CorrectlyForwardsCall()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var submitResult = await api.SubmitAsync();

            submitResult.CompletedChangeSet.Should().NotBeNull();
        }

        #endregion

        #region Exceptions

        [TestMethod]
        public async Task GetQueryableSource_CannotEnumerate()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.GetEnumerator(); };
            exceptionTest.Should().Throw<NotSupportedException>();

        }

        [TestMethod]
        public async Task GetQueryableSource_CannotEnumerateIEnumerable()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { (source as IEnumerable).GetEnumerator(); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public async Task GetQueryableSource_ProviderCannotGenericExecute()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.Provider.Execute<string>(null); };
            exceptionTest.Should().Throw<NotSupportedException>();

        }

        [TestMethod]
        public async Task GetQueryableSource_ProviderCannotExecute()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApi>() as ApiBase;
            var source = api.GetQueryableSource<string>("Test");

            Action exceptionTest = () => { source.Provider.Execute(null); };
            exceptionTest.Should().Throw<NotSupportedException>();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Runs a set of checks against an IQueryable to make sure it has been processed properly.
        /// </summary>
        /// <param name="source">The <see cref="IQueryable{T}"/> or <see cref="IQueryable"/> to test.</param>
        /// <param name="elementType">The <see cref="Type"/> returned by the <paramref name="source"/>.</param>
        /// <param name="expressionValues">A <see cref="List{string}"/> containing the parts of the expression to check for.</param>
        /// <param name="arguments">An array of arguments that the <see cref="IQueryable"/> we're testing requires. RWM: In the tests, this is an empty array. Not sure if that is v alid or not.</param>
        public void CheckQueryable(IQueryable source, Type elementType, List<string> expressionValues, object[] arguments)
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

        #endregion

        #region Test Resources

        private class TestModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var model = new EdmModel();
                var dummyType = new EdmEntityType("NS", "Dummy");
                model.AddElement(dummyType);
                var container = new EdmEntityContainer("NS", "DefaultContainer");
                container.AddEntitySet("Test", dummyType);
                model.AddElement(container);
                return Task.FromResult((IEdmModel)model);
            }
        }

        private class TestModelMapper : IModelMapper
        {
            public bool TryGetRelevantType(
                ModelContext context,
                string name, out Type relevantType)
            {
                relevantType = typeof(string);
                return true;
            }

            public bool TryGetRelevantType(
                ModelContext context,
                string namespaceName, string name,
                out Type relevantType)
            {
                relevantType = typeof(DateTime);
                return true;
            }
        }

        private class TestQuerySourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                return Expression.Constant(new[] { "Test" }.AsQueryable());
            }
        }

        private class TestChangeSetInitializer : IChangeSetInitializer
        {
            public Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken)
            {
                context.ChangeSet = new ChangeSet();
                return Task.FromResult<object>(null);
            }
        }

        private class TestSubmitExecutor : ISubmitExecutor
        {
            public Task<SubmitResult> ExecuteSubmitAsync(SubmitContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(new SubmitResult(context.ChangeSet));
            }
        }

        private class TestApi : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                var modelBuilder = new TestModelBuilder();
                var modelMapper = new TestModelMapper();
                var querySourcer = new TestQuerySourcer();
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();

                services.AddCoreServices(apiType);
                services.AddService<IModelBuilder>((sp, next) => modelBuilder);
                services.AddService<IModelMapper>((sp, next) => modelMapper);
                services.AddService<IQueryExpressionSourcer>((sp, next) => querySourcer);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);

                return services;
            }

            public TestApi(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestApiEmpty : ApiBase
        {
            public TestApiEmpty(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        #endregion

    }
}
