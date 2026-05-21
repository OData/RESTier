// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ApiBase"/> queryable extension methods.
    /// </summary>
    public class QueryableApiExtensionsTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IQueryExecutor queryExecutor;
        private readonly IModelMapper modelMapper;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBaseExtensionsTests"/> class.
        /// </summary>
        public QueryableApiExtensionsTests()
        {
            modelMapper = Substitute.For<IModelMapper>();
            queryExecutor = Substitute.For<IQueryExecutor>();
            var executorFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExecutor>>();
            executorFactory.Create().Returns(queryExecutor);
            var mapperFactory = Substitute.For<IChainOfResponsibilityFactory<IModelMapper>>();
            mapperFactory.Create().Returns(modelMapper);
            var sourcerFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionSourcer>>();
            sourcerFactory.Create().Returns(Substitute.For<IQueryExpressionSourcer>());
            var authorizerFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionAuthorizer>>();
            authorizerFactory.Create().Returns(default(IQueryExpressionAuthorizer));
            var expanderFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionExpander>>();
            expanderFactory.Create().Returns(default(IQueryExpressionExpander));
            var processorFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionProcessor>>();
            processorFactory.Create().Returns(default(IQueryExpressionProcessor));
            queryHandler = new DefaultQueryHandler(sourcerFactory, executorFactory, mapperFactory, authorizerFactory, expanderFactory, processorFactory);
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
        }

        /// <summary>
        /// Can call GetQueryAbleSource.
        /// </summary>
        [Fact]
        public void CanCallGetQueryableSourceWithApiBaseAndStringAndArrayOfObject()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var name = "Tests";
            Type expectedType = typeof(Test);

            modelMapper.TryGetRelevantType(Arg.Any<InvocationContext>(), name, out Arg.Any<Type>()).Returns(true).AndDoes(x => x[2] = expectedType);

            var arguments = new[] { new object(), new object(), new object() };
            var result = api.GetQueryableSource(name, arguments);

            result.Should().BeAssignableTo<IQueryable<Test>>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource with a first argument that is null.
        /// </summary>
        [Fact]
        public void CannotCallGetQueryableSourceWithApiBaseAndStringAndArrayOfObjectWithNullApi()
        {
            Action act = () => default(ApiBase).GetQueryableSource("TestValue119728298", new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource with an invalid ElementType name.
        /// </summary>
        /// <param name="value">The element Type name.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallGetQueryableSourceWithApiBaseAndStringAndArrayOfObjectWithInvalidName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource(value, new[] { new object(), new object(), new object() });
                    act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource(value, new[] { new object(), new object(), new object() });
                    act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Can call GetQueryAbleSource with a namespace.
        /// </summary>
        [Fact]
        public void CanCallGetQueryableSourceWithApiBaseAndStringAndStringAndArrayOfObject()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var namespaceName = "Microsoft.Restier.Tests.Core";
            var name = "Tests";
            Type expectedType = typeof(Test);

            modelMapper.TryGetRelevantType(Arg.Any<InvocationContext>(), namespaceName, name, out Arg.Any<Type>()).Returns(true).AndDoes(x => x[3] = expectedType);

            var arguments = new[] { new object(), new object(), new object() };
            var result = api.GetQueryableSource(namespaceName, name, arguments);

            result.Should().BeAssignableTo<IQueryable<Test>>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource with a first argument that is null.
        /// </summary>
        [Fact]
        public void CannotCallGetQueryableSourceWithApiBaseAndStringAndStringAndArrayOfObjectWithNullApi()
        {
            Action act = () => default(ApiBase).GetQueryableSource("TestValue486544476", "TestValue2009865785", new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource with an invalid namespace name.
        /// </summary>
        /// <param name="value">The namespace name.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallGetQueryableSourceWithApiBaseAndStringAndStringAndArrayOfObjectWithInvalidNamespaceName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource(value, "TestValue1716986786", new[] { new object(), new object(), new object() });
                    act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource(value, "TestValue1716986786", new[] { new object(), new object(), new object() });
                    act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource with an invalid ElementType name.
        /// </summary>
        /// <param name="value">The element Type name.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallGetQueryableSourceWithApiBaseAndStringAndStringAndArrayOfObjectWithInvalidName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource("TestValue1228629775", value, new[] { new object(), new object(), new object() }); act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource("TestValue1228629775", value, new[] { new object(), new object(), new object() }); act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Can call GetQueryAbleSource`1[TElement].
        /// </summary>
        [Fact]
        public void CanCallGetQueryableSourceWithTElementAndApiBaseAndStringAndArrayOfObject()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var name = "Tests";
            Type expectedType = typeof(Test);

            modelMapper.TryGetRelevantType(Arg.Any<InvocationContext>(), name, out Arg.Any<Type>()).Returns(true).AndDoes(x => x[2] = expectedType);

            var arguments = new[] { new object(), new object(), new object() };
            var result = api.GetQueryableSource<Test>(name, arguments);

            result.Should().BeAssignableTo<IQueryable<Test>>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource`1[TElement]. with an invalid TElement type.
        /// </summary>
        [Fact]
        public void CannotCallGetQueryableSourceWithInvalidTElement()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var name = "Tests";
            Type expectedType = typeof(Test);

            modelMapper.TryGetRelevantType(Arg.Any<InvocationContext>(), name, out Arg.Any<Type>()).Returns(true).AndDoes(x => x[2] = expectedType);

            var arguments = new[] { new object(), new object(), new object() };

            Action act = () => api.GetQueryableSource<QueryableApiExtensionsTests>(name, new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource`1[TElement]. with a first argument that is null.
        /// </summary>
        [Fact]
        public void CannotCallGetQueryableSourceWithTElementAndApiBaseAndStringAndArrayOfObjectWithNullApi()
        {
            Action act = () => default(ApiBase).GetQueryableSource<Test>("TestValue2056669437", new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call the non-generic Type-keyed GetQueryableSource overload.
        /// </summary>
        [Fact]
        public void CanCallGetQueryableSourceWithApiBaseAndTypeAndStringAndArrayOfObject()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var name = "Tests";
            Type expectedType = typeof(Test);

            modelMapper.TryGetRelevantType(Arg.Any<InvocationContext>(), name, out Arg.Any<Type>()).Returns(true).AndDoes(x => x[2] = expectedType);

            var arguments = new[] { new object(), new object(), new object() };
            var result = api.GetQueryableSource(typeof(Test), name, arguments);

            result.Should().BeAssignableTo<IQueryable<Test>>();
        }

        /// <summary>
        /// Cannot call the non-generic Type-keyed GetQueryableSource with a null api.
        /// </summary>
        [Fact]
        public void CannotCallGetQueryableSourceWithApiBaseAndTypeAndStringAndArrayOfObjectWithNullApi()
        {
            Action act = () => default(ApiBase).GetQueryableSource(typeof(Test), "TestValue", new[] { new object() });
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call the non-generic Type-keyed GetQueryableSource with a null element type.
        /// </summary>
        [Fact]
        public void CannotCallGetQueryableSourceWithApiBaseAndNullTypeAndStringAndArrayOfObject()
        {
            Action act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource(default(Type), "TestValue", new[] { new object() });
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call the non-generic Type-keyed GetQueryableSource when the supplied element type
        /// does not match the type resolved from the model.
        /// </summary>
        [Fact]
        public void CannotCallGetQueryableSourceWithApiBaseAndMismatchedTypeAndStringAndArrayOfObject()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var name = "Tests";
            Type resolvedType = typeof(Test);

            modelMapper.TryGetRelevantType(Arg.Any<InvocationContext>(), name, out Arg.Any<Type>()).Returns(true).AndDoes(x => x[2] = resolvedType);

            Action act = () => api.GetQueryableSource(typeof(QueryableApiExtensionsTests), name, new[] { new object() });
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource`1[TElement]. with an invalid ElementType name.
        /// </summary>
        /// <param name="value">The element Type name.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallGetQueryableSourceWithTElementAndApiBaseAndStringAndArrayOfObjectWithInvalidName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource<Test>(value, new[] { new object(), new object(), new object() }); act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource<Test>(value, new[] { new object(), new object(), new object() }); act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Can call GetQueryAbleSource`1[TElement].
        /// </summary>
        [Fact]
        public void CanCallGetQueryableSourceWithTElementAndApiBaseAndStringAndStringAndArrayOfObject()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var namespaceName = "Microsoft.Restier.Tests.Core";
            var name = "Tests";
            Type expectedType = typeof(Test);

            modelMapper.TryGetRelevantType(Arg.Any<InvocationContext>(), namespaceName, name, out Arg.Any<Type>()).Returns(true).AndDoes(x => x[3] = expectedType);

            var arguments = new[] { new object(), new object(), new object() };
            var result = api.GetQueryableSource<Test>(namespaceName, name, arguments);

            result.Should().BeAssignableTo<IQueryable<Test>>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource`1[TElement]. with an invalid TElement type.
        /// </summary>
        [Fact]
        public void CannotCallGetQueryableSourceWithInvalidTElementAndNamespace()
        {
            var api = new TestApi(model, queryHandler, submitHandler);
            var namespaceName = "Microsoft.Restier.Tests.Core";
            var name = "Tests";
            Type expectedType = typeof(Test);

            modelMapper.TryGetRelevantType(Arg.Any<InvocationContext>(), namespaceName, name, out Arg.Any<Type>()).Returns(true).AndDoes(x => x[3] = expectedType);

            var arguments = new[] { new object(), new object(), new object() };

            Action act = () => api.GetQueryableSource<QueryableApiExtensionsTests>(namespaceName, name, new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource with a first argument that is null.
        /// </summary>
        [Fact]
        public void CannotCallGetQueryableSourceWithTElementAndApiBaseAndStringAndStringAndArrayOfObjectWithNullApi()
        {
            Action act = () => default(ApiBase).GetQueryableSource<Test>("TestValue1686186750", "TestValue1325825672", new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource`1[TElement]. with an invalid namespace name.
        /// </summary>
        /// <param name="value">The namespace name.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallGetQueryableSourceWithTElementAndApiBaseAndStringAndStringAndArrayOfObjectWithInvalidNamespaceName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource<Test>(value, "TestValue1716986786", new[] { new object(), new object(), new object() }); act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource<Test>(value, "TestValue1716986786", new[] { new object(), new object(), new object() }); act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource`1[TElement] with an invalid ElementType name.
        /// </summary>
        /// <param name="value">The element Type name.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallGetQueryableSourceWithTElementAndApiBaseAndStringAndStringAndArrayOfObjectWithInvalidName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource("TestValue1228629775", value, new[] { new object(), new object(), new object() }); act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(model, queryHandler, submitHandler).GetQueryableSource("TestValue1228629775", value, new[] { new object(), new object(), new object() }); act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Can call QueryAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallQueryAsync()
        {
            var api = new TestApi(model, queryHandler, submitHandler);

            IQueryable<Test> queryable = new List<Test>()
            {
                new Test() { Name = "The", },
                new Test() { Name = "Quick", },
                new Test() { Name = "Brown", },
                new Test() { Name = "Fox", },
            }.AsQueryable();

            queryExecutor.ExecuteQueryAsync(Arg.Any<QueryContext>(), Arg.Any<IQueryable<Test>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new QueryResult(queryable)));

            var source = Expression.Constant(queryable);
            var request = new QueryRequest(new QueryableSource<Test>(source));

            var cancellationToken = CancellationToken.None;
            var result = await api.QueryAsync(request, cancellationToken);
            result.Results.Should().BeEquivalentTo(queryable);
        }

        /// <summary>
        /// Cannot call QueryAsync with a null Query request.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CannotCallQueryAsyncWithNullRequest()
        {
            Func<Task> act = () => new TestApi(model, queryHandler, submitHandler).QueryAsync(default(QueryRequest), CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

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