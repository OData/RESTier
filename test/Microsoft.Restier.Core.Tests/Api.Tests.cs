// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ApiTests
    {
        private class TestModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                var model = new EdmModel();
                var dummyType = new EdmEntityType("NS", "Dummy");
                model.AddElement(dummyType);
                var container = new EdmEntityContainer("NS", "DefaultContainer");
                container.AddEntitySet("Test", dummyType);
                model.AddElement(container);
                return Task.FromResult((IEdmModel) model);
            }
        }

        private class TestModelMapper : IModelMapper
        {
            public bool TryGetRelevantType(
                ApiContext context,
                string name, out Type relevantType)
            {
                relevantType = typeof(string);
                return true;
            }

            public bool TryGetRelevantType(
                ApiContext context,
                string namespaceName, string name,
                out Type relevantType)
            {
                relevantType = typeof(DateTime);
                return true;
            }
        }

        private class TestQuerySourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSourceStub(QueryExpressionContext context, bool embedded)
            {
                return Expression.Constant(new[] {"Test"}.AsQueryable());
            }
        }

        private class TestChangeSetPreparer : IChangeSetPreparer
        {
            public Task PrepareAsync(SubmitContext context, CancellationToken cancellationToken)
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
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                var modelBuilder = new TestModelBuilder();
                var modelMapper = new TestModelMapper();
                var querySourcer = new TestQuerySourcer();
                var changeSetPreparer = new TestChangeSetPreparer();
                var submitExecutor = new TestSubmitExecutor();

                services.AddCoreServices(this.GetType());
                services.AddService<IModelBuilder>((sp, next) => modelBuilder);
                services.AddService<IModelMapper>((sp, next) => modelMapper);
                services.AddService<IQueryExpressionSourcer>((sp, next) => querySourcer);
                services.AddService<IChangeSetPreparer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);

                return services;
            }
        }

        private class TestApiEmpty : ApiBase
        {
        }

        [Fact]
        public void ApiSourceOfEntityContainerElementIsCorrect()
        {
            var api = new TestApi();
            var arguments = new object[0];

            var source = api.GetQueryableSourceStub("Test", arguments);
            Assert.Equal(typeof (string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof (DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSourceStub", methodCall.Method.Name);
            Assert.Equal(typeof (string), methodCall.Method.GetGenericArguments()[0]);
            Assert.Equal(2, methodCall.Arguments.Count);
            Assert.True(methodCall.Arguments[0] is ConstantExpression);
            Assert.Equal("Test", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[1] is ConstantExpression);
            Assert.Equal(arguments, (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.Equal(source.Expression.ToString(), source.ToString());
        }

        [Fact]
        public void SourceOfEntityContainerElementThrowsIfNotMapped()
        {
            var api = new TestApiEmpty();
            var context = api.Context;
            var arguments = new object[0];

            Assert.Throws<NotSupportedException>(() => context.GetQueryableSourceStub("Test", arguments));
        }

        [Fact]
        public void SourceOfEntityContainerElementIsCorrect()
        {
            var api = new TestApi();
            var context = api.Context;
            var arguments = new object[0];

            var source = context.GetQueryableSourceStub("Test", arguments);
            Assert.Equal(typeof (string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof (DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSourceStub", methodCall.Method.Name);
            Assert.Equal(typeof (string), methodCall.Method.GetGenericArguments()[0]);
            Assert.Equal(2, methodCall.Arguments.Count);
            Assert.True(methodCall.Arguments[0] is ConstantExpression);
            Assert.Equal("Test", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[1] is ConstantExpression);
            Assert.Equal(arguments, (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.Equal(source.Expression.ToString(), source.ToString());
        }

        [Fact]
        public void ApiSourceOfComposableFunctionIsCorrect()
        {
            var api = new TestApi();
            var arguments = new object[0];

            var source = api.GetQueryableSourceStub("Namespace", "Function", arguments);
            Assert.Equal(typeof (DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof (DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSourceStub", methodCall.Method.Name);
            Assert.Equal(typeof (DateTime), methodCall.Method.GetGenericArguments()[0]);
            Assert.Equal(3, methodCall.Arguments.Count);
            Assert.True(methodCall.Arguments[0] is ConstantExpression);
            Assert.Equal("Namespace", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[1] is ConstantExpression);
            Assert.Equal("Function", (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[2] is ConstantExpression);
            Assert.Equal(arguments, (methodCall.Arguments[2] as ConstantExpression).Value);
            Assert.Equal(source.Expression.ToString(), source.ToString());
        }

        [Fact]
        public void SourceOfComposableFunctionThrowsIfNotMapped()
        {
            var api = new TestApiEmpty();
            var context = api.Context;
            var arguments = new object[0];

            Assert.Throws<NotSupportedException>(() => context.GetQueryableSourceStub("Namespace", "Function", arguments));
        }

        [Fact]
        public void SourceOfComposableFunctionIsCorrect()
        {
            var api = new TestApi();
            var context = api.Context;
            var arguments = new object[0];

            var source = context.GetQueryableSourceStub("Namespace", "Function", arguments);
            Assert.Equal(typeof (DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof (DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSourceStub", methodCall.Method.Name);
            Assert.Equal(typeof (DateTime), methodCall.Method.GetGenericArguments()[0]);
            Assert.Equal(3, methodCall.Arguments.Count);
            Assert.True(methodCall.Arguments[0] is ConstantExpression);
            Assert.Equal("Namespace", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[1] is ConstantExpression);
            Assert.Equal("Function", (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[2] is ConstantExpression);
            Assert.Equal(arguments, (methodCall.Arguments[2] as ConstantExpression).Value);
            Assert.Equal(source.Expression.ToString(), source.ToString());
        }

        [Fact]
        public void GenericApiSourceOfEntityContainerElementIsCorrect()
        {
            var api = new TestApi();
            var arguments = new object[0];

            var source = api.GetQueryableSourceStub<string>("Test", arguments);
            Assert.Equal(typeof (string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof (DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSourceStub", methodCall.Method.Name);
            Assert.Equal(typeof (string), methodCall.Method.GetGenericArguments()[0]);
            Assert.Equal(2, methodCall.Arguments.Count);
            Assert.True(methodCall.Arguments[0] is ConstantExpression);
            Assert.Equal("Test", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[1] is ConstantExpression);
            Assert.Equal(arguments, (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.Equal(source.Expression.ToString(), source.ToString());
        }

        [Fact]
        public void GenericSourceOfEntityContainerElementThrowsIfWrongType()
        {
            var api = new TestApi();
            var context = api.Context;
            var arguments = new object[0];

            Assert.Throws<ArgumentException>(() => context.GetQueryableSourceStub<object>("Test", arguments));
        }

        [Fact]
        public void GenericSourceOfEntityContainerElementIsCorrect()
        {
            var api = new TestApi();
            var context = api.Context;
            var arguments = new object[0];

            var source = context.GetQueryableSourceStub<string>("Test", arguments);
            Assert.Equal(typeof (string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof (DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSourceStub", methodCall.Method.Name);
            Assert.Equal(typeof (string), methodCall.Method.GetGenericArguments()[0]);
            Assert.Equal(2, methodCall.Arguments.Count);
            Assert.True(methodCall.Arguments[0] is ConstantExpression);
            Assert.Equal("Test", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[1] is ConstantExpression);
            Assert.Equal(arguments, (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.Equal(source.Expression.ToString(), source.ToString());
        }

        [Fact]
        public void GenericApiSourceOfComposableFunctionIsCorrect()
        {
            var api = new TestApi();
            var arguments = new object[0];

            var source = api.GetQueryableSourceStub<DateTime>(
                "Namespace", "Function", arguments);
            Assert.Equal(typeof (DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof (DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSourceStub", methodCall.Method.Name);
            Assert.Equal(typeof (DateTime), methodCall.Method.GetGenericArguments()[0]);
            Assert.Equal(3, methodCall.Arguments.Count);
            Assert.True(methodCall.Arguments[0] is ConstantExpression);
            Assert.Equal("Namespace", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[1] is ConstantExpression);
            Assert.Equal("Function", (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[2] is ConstantExpression);
            Assert.Equal(arguments, (methodCall.Arguments[2] as ConstantExpression).Value);
            Assert.Equal(source.Expression.ToString(), source.ToString());
        }

        [Fact]
        public void GenericSourceOfComposableFunctionThrowsIfWrongType()
        {
            var api = new TestApi();
            var context = api.Context;
            var arguments = new object[0];

            Assert.Throws<ArgumentException>(() => context.GetQueryableSourceStub<object>("Namespace", "Function", arguments));
        }

        [Fact]
        public void GenericSourceOfComposableFunctionIsCorrect()
        {
            var api = new TestApi();
            var context = api.Context;
            var arguments = new object[0];

            var source = context.GetQueryableSourceStub<DateTime>("Namespace", "Function", arguments);
            Assert.Equal(typeof (DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof (DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSourceStub", methodCall.Method.Name);
            Assert.Equal(typeof (DateTime), methodCall.Method.GetGenericArguments()[0]);
            Assert.Equal(3, methodCall.Arguments.Count);
            Assert.True(methodCall.Arguments[0] is ConstantExpression);
            Assert.Equal("Namespace", (methodCall.Arguments[0] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[1] is ConstantExpression);
            Assert.Equal("Function", (methodCall.Arguments[1] as ConstantExpression).Value);
            Assert.True(methodCall.Arguments[2] is ConstantExpression);
            Assert.Equal(arguments, (methodCall.Arguments[2] as ConstantExpression).Value);
            Assert.Equal(source.Expression.ToString(), source.ToString());
        }

        [Fact]
        public void SourceQueryableCannotGenericEnumerate()
        {
            var api = new TestApi();
            var context = api.Context;

            var source = context.GetQueryableSourceStub<string>("Test");
            Assert.Throws<NotSupportedException>(() => source.GetEnumerator());
        }

        [Fact]
        public void SourceQueryableCannotEnumerate()
        {
            var api = new TestApi();
            var context = api.Context;

            var source = context.GetQueryableSourceStub<string>("Test");
            Assert.Throws<NotSupportedException>(() => (source as IEnumerable).GetEnumerator());
        }

        [Fact]
        public void SourceQueryProviderCannotGenericExecute()
        {
            var api = new TestApi();
            var context = api.Context;

            var source = context.GetQueryableSourceStub<string>("Test");
            Assert.Throws<NotSupportedException>(() => source.Provider.Execute<string>(null));
        }

        [Fact]
        public void SourceQueryProviderCannotExecute()
        {
            var api = new TestApi();
            var context = api.Context;

            var source = context.GetQueryableSourceStub<string>("Test");
            Assert.Throws<NotSupportedException>(() => source.Provider.Execute(null));
        }

        [Fact]
        public async Task ApiQueryAsyncWithQueryReturnsResults()
        {
            var api = new TestApi();

            var request = new QueryRequest(api.GetQueryableSourceStub<string>("Test"));
            var result = await api.Context.QueryAsync(request);
            var results = result.Results.Cast<string>();

            Assert.True(results.SequenceEqual(new[] {"Test"}));
        }

        [Fact]
        public async Task ApiQueryAsyncCorrectlyForwardsCall()
        {
            var api = new TestApi();

            var queryRequest = new QueryRequest(
                api.GetQueryableSourceStub<string>("Test"));
            var queryResult = await api.QueryAsync(queryRequest);
            Assert.True(queryResult.Results.Cast<string>()
                .SequenceEqual(new[] {"Test"}));
        }

        [Fact]
        public async Task ApiSubmitAsyncCorrectlyForwardsCall()
        {
            var api = new TestApi();

            var submitResult = await api.SubmitAsync();
            Assert.NotNull(submitResult.CompletedChangeSet);
        }
    }
}
