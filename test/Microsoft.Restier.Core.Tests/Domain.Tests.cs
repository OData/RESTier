// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
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
        private class TestModelBuilder : IDelegateHookHandler<IModelBuilder>, IModelBuilder
        {
            public IModelBuilder InnerHandler { get; set; }

            public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
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
            public Expression Source(QueryExpressionContext context, bool embedded)
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

        private class TestApi : IApi
        {
            private ApiContext _context;
            
            public ApiContext Context
            {
                get
                {
                    if (_context == null)
                    {
                        var configuration = new ApiConfiguration();
                        var modelBuilder = new TestModelBuilder();
                        var modelMapper = new TestModelMapper();
                        var querySourcer = new TestQuerySourcer();
                        var changeSetPreparer = new TestChangeSetPreparer();
                        var submitExecutor = new TestSubmitExecutor();
                        configuration.AddHookHandler<IModelBuilder>(modelBuilder);
                        configuration.AddHookHandler<IModelMapper>(modelMapper);
                        configuration.AddHookHandler<IQueryExpressionSourcer>(querySourcer);
                        configuration.AddHookHandler<IChangeSetPreparer>(changeSetPreparer);
                        configuration.AddHookHandler<ISubmitExecutor>(submitExecutor);
                        configuration.EnsureCommitted();
                        _context = new ApiContext(configuration);
                    }

                    return _context;
                }
            }

            public void Dispose()
            {
            }
        }

        [Fact]
        public void ApiSourceOfEntityContainerElementIsCorrect()
        {
            var api = new TestApi();
            var arguments = new object[0];

            var source = api.Source("Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(ApiData), methodCall.Method.DeclaringType);
            Assert.Equal("Source", methodCall.Method.Name);
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
            var configuration = new ApiConfiguration();
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);
            var arguments = new object[0];

            Assert.Throws<NotSupportedException>(() => Api.Source(context, "Test", arguments));
        }

        [Fact]
        public void SourceOfEntityContainerElementIsCorrect()
        {
            var configuration = new ApiConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);
            var arguments = new object[0];

            var source = Api.Source(context, "Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(ApiData), methodCall.Method.DeclaringType);
            Assert.Equal("Source", methodCall.Method.Name);
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
            var api = new TestApi();
            var arguments = new object[0];

            var source = api.Source("Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(ApiData), methodCall.Method.DeclaringType);
            Assert.Equal("Source", methodCall.Method.Name);
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
            var configuration = new ApiConfiguration();
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);
            var arguments = new object[0];

            Assert.Throws<NotSupportedException>(() => Api.Source(context, "Namespace", "Function", arguments));
        }

        [Fact]
        public void SourceOfComposableFunctionIsCorrect()
        {
            var configuration = new ApiConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);
            var arguments = new object[0];

            var source = Api.Source(context,
                "Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(ApiData), methodCall.Method.DeclaringType);
            Assert.Equal("Source", methodCall.Method.Name);
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
            var api = new TestApi();
            var arguments = new object[0];

            var source = api.Source<string>("Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(ApiData), methodCall.Method.DeclaringType);
            Assert.Equal("Source", methodCall.Method.Name);
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
            var configuration = new ApiConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);
            var arguments = new object[0];

            Assert.Throws<ArgumentException>(() => Api.Source<object>(context, "Test", arguments));
        }

        [Fact]
        public void GenericSourceOfEntityContainerElementIsCorrect()
        {
            var configuration = new ApiConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);
            var arguments = new object[0];

            var source = Api.Source<string>(context, "Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(ApiData), methodCall.Method.DeclaringType);
            Assert.Equal("Source", methodCall.Method.Name);
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
            var api = new TestApi();
            var arguments = new object[0];

            var source = api.Source<DateTime>(
                "Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(ApiData), methodCall.Method.DeclaringType);
            Assert.Equal("Source", methodCall.Method.Name);
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
            var configuration = new ApiConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);
            var arguments = new object[0];

            Assert.Throws<ArgumentException>(() => Api.Source<object>(context, "Namespace", "Function", arguments));
        }

        [Fact]
        public void GenericSourceOfComposableFunctionIsCorrect()
        {
            var configuration = new ApiConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);
            var arguments = new object[0];

            var source = Api.Source<DateTime>(context,
                "Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(ApiData), methodCall.Method.DeclaringType);
            Assert.Equal("Source", methodCall.Method.Name);
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
            var configuration = new ApiConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);

            var source = Api.Source<string>(context, "Test");
            Assert.Throws<NotSupportedException>(() => source.GetEnumerator());
        }

        [Fact]
        public void SourceQueryableCannotEnumerate()
        {
            var configuration = new ApiConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);

            var source = Api.Source<string>(context, "Test");
            Assert.Throws<NotSupportedException>(() => (source as IEnumerable).GetEnumerator());
        }

        [Fact]
        public void SourceQueryProviderCannotGenericExecute()
        {
            var configuration = new ApiConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);

            var source = Api.Source<string>(context, "Test");
            Assert.Throws<NotSupportedException>(() => source.Provider.Execute<string>(null));
        }

        [Fact]
        public void SourceQueryProviderCannotExecute()
        {
            var configuration = new ApiConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new ApiContext(configuration);

            var source = Api.Source<string>(context, "Test");
            Assert.Throws<NotSupportedException>(() => source.Provider.Execute(null));
        }

        [Fact]
        public async Task ApiQueryAsyncWithQueryReturnsResults()
        {
            var api = new TestApi();

            var results = await api.QueryAsync(
                api.Source<string>("Test"));
            Assert.True(results.SequenceEqual(new string[] { "Test" }));
        }

        [Fact]
        public async Task ApiQueryAsyncWithSingletonQueryReturnsResult()
        {
            var api = new TestApi();

            var result = await api.QueryAsync(
                api.Source<string>("Test"), q => q.Single());
            Assert.Equal("Test", result);
        }

        [Fact]
        public async Task ApiQueryAsyncCorrectlyForwardsCall()
        {
            var api = new TestApi();

            var queryRequest = new QueryRequest(
                api.Source<string>("Test"), true);
            var queryResult = await api.QueryAsync(queryRequest);
            Assert.True(queryResult.Results.Cast<string>()
                .SequenceEqual(new string[] { "Test" }));
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
