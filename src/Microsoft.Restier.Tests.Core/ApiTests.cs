// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
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
            public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
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
                return Expression.Constant(new[] {"Test"}.AsQueryable());
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

        [Fact]
        public void ApiSourceOfEntityContainerElementIsCorrect()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var arguments = new object[0];

            var source = api.GetQueryableSource("Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSource", methodCall.Method.Name);
            Assert.Equal(typeof(string), methodCall.Method.GetGenericArguments()[0]);
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
            var container = new RestierContainerBuilder(typeof(TestApiEmpty));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            var arguments = new object[0];

            Assert.Throws<NotSupportedException>(() => api.GetQueryableSource("Test", arguments));
        }

        [Fact]
        public void SourceOfEntityContainerElementIsCorrect()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            var arguments = new object[0];

            var source = api.GetQueryableSource("Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSource", methodCall.Method.Name);
            Assert.Equal(typeof(string), methodCall.Method.GetGenericArguments()[0]);
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
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var arguments = new object[0];

            var source = api.GetQueryableSource("Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSource", methodCall.Method.Name);
            Assert.Equal(typeof(DateTime), methodCall.Method.GetGenericArguments()[0]);
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
            var container = new RestierContainerBuilder(typeof(TestApiEmpty));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            var arguments = new object[0];

            Assert.Throws<NotSupportedException>(() => api.GetQueryableSource("Namespace", "Function", arguments));
        }

        [Fact]
        public void SourceOfComposableFunctionIsCorrect()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var arguments = new object[0];

            var source = api.GetQueryableSource("Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSource", methodCall.Method.Name);
            Assert.Equal(typeof(DateTime), methodCall.Method.GetGenericArguments()[0]);
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
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var arguments = new object[0];

            var source = api.GetQueryableSource<string>("Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSource", methodCall.Method.Name);
            Assert.Equal(typeof(string), methodCall.Method.GetGenericArguments()[0]);
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
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            var arguments = new object[0];

            Assert.Throws<ArgumentException>(() => api.GetQueryableSource<object>("Test", arguments));
        }

        [Fact]
        public void GenericSourceOfEntityContainerElementIsCorrect()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var arguments = new object[0];

            var source = api.GetQueryableSource<string>("Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSource", methodCall.Method.Name);
            Assert.Equal(typeof(string), methodCall.Method.GetGenericArguments()[0]);
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
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var arguments = new object[0];

            var source = api.GetQueryableSource<DateTime>(
                "Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSource", methodCall.Method.Name);
            Assert.Equal(typeof(DateTime), methodCall.Method.GetGenericArguments()[0]);
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
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var arguments = new object[0];

            Assert.Throws<ArgumentException>(() => api.GetQueryableSource<object>("Namespace", "Function", arguments));
        }

        [Fact]
        public void GenericSourceOfComposableFunctionIsCorrect()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var arguments = new object[0];

            var source = api.GetQueryableSource<DateTime>("Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DataSourceStub), methodCall.Method.DeclaringType);
            Assert.Equal("GetQueryableSource", methodCall.Method.Name);
            Assert.Equal(typeof(DateTime), methodCall.Method.GetGenericArguments()[0]);
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
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var source = api.GetQueryableSource<string>("Test");
            Assert.Throws<NotSupportedException>(() => source.GetEnumerator());
        }

        [Fact]
        public void SourceQueryableCannotEnumerate()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var source = api.GetQueryableSource<string>("Test");
            Assert.Throws<NotSupportedException>(() => (source as IEnumerable).GetEnumerator());
        }

        [Fact]
        public void SourceQueryProviderCannotGenericExecute()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var source = api.GetQueryableSource<string>("Test");
            Assert.Throws<NotSupportedException>(() => source.Provider.Execute<string>(null));
        }

        [Fact]
        public void SourceQueryProviderCannotExecute()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var source = api.GetQueryableSource<string>("Test");
            Assert.Throws<NotSupportedException>(() => source.Provider.Execute(null));
        }

        [Fact]
        public async Task ApiQueryAsyncWithQueryReturnsResults()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var request = new QueryRequest(api.GetQueryableSource<string>("Test"));
            var result = await api.QueryAsync(request);
            var results = result.Results.Cast<string>();

            Assert.True(results.SequenceEqual(new[] {"Test"}));
        }

        [Fact]
        public async Task ApiQueryAsyncCorrectlyForwardsCall()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var queryRequest = new QueryRequest(
                api.GetQueryableSource<string>("Test"));
            var queryResult = await api.QueryAsync(queryRequest);
            Assert.True(queryResult.Results.Cast<string>()
                .SequenceEqual(new[] {"Test"}));
        }

        [Fact]
        public async Task ApiSubmitAsyncCorrectlyForwardsCall()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var submitResult = await api.SubmitAsync();
            Assert.NotNull(submitResult.CompletedChangeSet);
        }
    }
}
