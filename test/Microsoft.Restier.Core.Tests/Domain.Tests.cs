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
    public class DomainTests
    {
        private class TestModelHandler : HookHandler<ModelBuilderContext>
        {
            public DomainContext DomainContext { get; set; }

            public IEdmModel Model { get; set; }

            public override Task HandleAsync(ModelBuilderContext context, CancellationToken cancellationToken)
            {
                Assert.Same(DomainContext, context.DomainContext);
                context.Model = this.Model;
                return Task.FromResult<object>(null);
            }
        }

        private class TestModelMapper : IModelMapper
        {
            public bool TryGetRelevantType(
                DomainContext context,
                string name, out Type relevantType)
            {
                relevantType = typeof(string);
                return true;
            }

            public bool TryGetRelevantType(
                DomainContext context,
                string namespaceName, string name,
                out Type relevantType)
            {
                relevantType = typeof(DateTime);
                return true;
            }
        }

        private class TestQueryHandler : IQueryHandler
        {
            public DomainContext DomainContext { get; set; }

            public IEnumerable Results { get; set; }

            public Task<QueryResult> QueryAsync(
                QueryContext context,
                CancellationToken cancellationToken)
            {
                Assert.Same(DomainContext, context.DomainContext);
                return Task.FromResult(new QueryResult(Results));
            }
        }

        private class TestSubmitHandler : ISubmitHandler
        {
            public DomainContext DomainContext { get; set; }

            public ChangeSet ChangeSet { get; set; }

            public Task<SubmitResult> SubmitAsync(
                SubmitContext context,
                CancellationToken cancellationToken)
            {
                Assert.Same(DomainContext, context.DomainContext);
                return Task.FromResult(new SubmitResult(ChangeSet));
            }
        }

        private class TestDomain : IDomain
        {
            private DomainContext _context;

            public IEdmModel Model { get; private set; }

            public IEnumerable Results { get; private set; }

            public ChangeSet ChangeSet { get; private set; }

            public DomainContext Context
            {
                get
                {
                    if (_context == null)
                    {
                        var configuration = new DomainConfiguration();
                        var modelHandler = new TestModelHandler();
                        var modelMapper = new TestModelMapper();
                        var queryHandler = new TestQueryHandler();
                        var submitHandler = new TestSubmitHandler();
                        configuration.AddHookHandler(modelHandler);
                        configuration.AddHookHandler1<IModelMapper>(modelMapper);
                        configuration.SetHookPoint(
                            typeof(IQueryHandler), queryHandler);
                        configuration.SetHookPoint(
                            typeof(ISubmitHandler), submitHandler);
                        configuration.EnsureCommitted();
                        _context = new DomainContext(configuration);
                        modelHandler.DomainContext = _context;
                        Model = modelHandler.Model = new EdmModel();
                        queryHandler.DomainContext = _context;
                        Results = queryHandler.Results = new string[] { "Test" };
                        submitHandler.DomainContext = _context;
                        ChangeSet = submitHandler.ChangeSet = new ChangeSet();
                    }
                    return _context;
                }
            }

            public void Dispose()
            {
            }
        }

        [Fact]
        public void DomainSourceOfEntityContainerElementIsCorrect()
        {
            var domain = new TestDomain();
            var arguments = new object[0];

            var source = domain.Source("Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DomainData), methodCall.Method.DeclaringType);
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
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            Assert.Throws<NotSupportedException>(() => Domain.Source(context, "Test", arguments));
        }

        [Fact]
        public void SourceOfEntityContainerElementIsCorrect()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            var source = Domain.Source(context, "Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DomainData), methodCall.Method.DeclaringType);
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
        public void DomainSourceOfComposableFunctionIsCorrect()
        {
            var domain = new TestDomain();
            var arguments = new object[0];

            var source = domain.Source("Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DomainData), methodCall.Method.DeclaringType);
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
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            Assert.Throws<NotSupportedException>(() => Domain.Source(context, "Namespace", "Function", arguments));
        }

        [Fact]
        public void SourceOfComposableFunctionIsCorrect()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            var source = Domain.Source(context,
                "Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DomainData), methodCall.Method.DeclaringType);
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
        public void GenericDomainSourceOfEntityContainerElementIsCorrect()
        {
            var domain = new TestDomain();
            var arguments = new object[0];

            var source = domain.Source<string>("Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DomainData), methodCall.Method.DeclaringType);
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
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            Assert.Throws<ArgumentException>(() => Domain.Source<object>(context, "Test", arguments));
        }

        [Fact]
        public void GenericSourceOfEntityContainerElementIsCorrect()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            var source = Domain.Source<string>(context, "Test", arguments);
            Assert.Equal(typeof(string), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DomainData), methodCall.Method.DeclaringType);
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
        public void GenericDomainSourceOfComposableFunctionIsCorrect()
        {
            var domain = new TestDomain();
            var arguments = new object[0];

            var source = domain.Source<DateTime>(
                "Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DomainData), methodCall.Method.DeclaringType);
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
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            Assert.Throws<ArgumentException>(() => Domain.Source<object>(context, "Namespace", "Function", arguments));
        }

        [Fact]
        public void GenericSourceOfComposableFunctionIsCorrect()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            var arguments = new object[0];

            var source = Domain.Source<DateTime>(context,
                "Namespace", "Function", arguments);
            Assert.Equal(typeof(DateTime), source.ElementType);
            Assert.True(source.Expression is MethodCallExpression);
            var methodCall = source.Expression as MethodCallExpression;
            Assert.Null(methodCall.Object);
            Assert.Equal(typeof(DomainData), methodCall.Method.DeclaringType);
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
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);

            var source = Domain.Source<string>(context, "Test");
            Assert.Throws<NotSupportedException>(() => source.GetEnumerator());
        }

        [Fact]
        public void SourceQueryableCannotEnumerate()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);

            var source = Domain.Source<string>(context, "Test");
            Assert.Throws<NotSupportedException>(() => (source as IEnumerable).GetEnumerator());
        }

        [Fact]
        public void SourceQueryProviderCannotGenericExecute()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);

            var source = Domain.Source<string>(context, "Test");
            Assert.Throws<NotSupportedException>(() => source.Provider.Execute<string>(null));
        }

        [Fact]
        public void SourceQueryProviderCannotExecute()
        {
            var configuration = new DomainConfiguration();
            var modelMapper = new TestModelMapper();
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);

            var source = Domain.Source<string>(context, "Test");
            Assert.Throws<NotSupportedException>(() => source.Provider.Execute(null));
        }

        [Fact]
        public async Task DomainQueryAsyncWithQueryReturnsResults()
        {
            var domain = new TestDomain();

            var results = await domain.QueryAsync(
                domain.Source<string>("Test"));
            Assert.True(results.SequenceEqual(new string[] { "Test" }));
        }

        [Fact]
        public async Task DomainQueryAsyncWithSingletonQueryReturnsResult()
        {
            var domain = new TestDomain();

            var result = await domain.QueryAsync(
                domain.Source<string>("Test"), q => q.Single());
            Assert.Equal("Test", result);
        }

        [Fact]
        public async Task DomainQueryAsyncCorrectlyForwardsCall()
        {
            var domain = new TestDomain();

            var queryRequest = new QueryRequest(
                domain.Source<string>("Test"), true);
            var queryResult = await domain.QueryAsync(queryRequest);
            Assert.True(queryResult.Results.Cast<string>()
                .SequenceEqual(new string[] { "Test" }));
        }

        [Fact]
        public async Task QueryAsyncCorrectlyUsesQueryHandler()
        {
            var configuration = new DomainConfiguration();
            var modelHandler = new TestModelHandler();
            var modelMapper = new TestModelMapper();
            var queryHandler = new TestQueryHandler();
            configuration.AddHookHandler(modelHandler);
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.SetHookPoint(typeof(IQueryHandler), queryHandler);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            modelHandler.DomainContext = context;
            modelHandler.Model = new EdmModel();
            queryHandler.DomainContext = context;
            queryHandler.Results = new string[] { "Test" };

            var queryRequest = new QueryRequest(
                Domain.Source<string>(context, "Test"), true);
            var queryResult = await Domain.QueryAsync(context, queryRequest);
            Assert.True(queryResult.Results.Cast<string>()
                .SequenceEqual(new string[] { "Test" }));
        }

        [Fact]
        public async Task DomainSubmitAsyncCorrectlyForwardsCall()
        {
            var domain = new TestDomain();

            var submitResult = await domain.SubmitAsync();
            Assert.Same(domain.ChangeSet, submitResult.CompletedChangeSet);
        }

        [Fact]
        public async Task SubmitAsyncCorrectlyUsesSubmitHandler()
        {
            var configuration = new DomainConfiguration();
            var modelHandler = new TestModelHandler();
            var modelMapper = new TestModelMapper();
            var submitHandler = new TestSubmitHandler();
            configuration.AddHookHandler(modelHandler);
            configuration.AddHookHandler1<IModelMapper>(modelMapper);
            configuration.SetHookPoint(typeof(ISubmitHandler), submitHandler);
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);
            modelHandler.DomainContext = context;
            modelHandler.Model = new EdmModel();
            submitHandler.DomainContext = context;
            submitHandler.ChangeSet = new ChangeSet();

            var submitResult = await Domain.SubmitAsync(context);
            Assert.Same(submitHandler.ChangeSet,
                submitResult.CompletedChangeSet);
        }
    }
}
