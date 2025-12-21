// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
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
    /// Unit tests for the <see cref="ApiBase"/> extension methods.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ApiBaseExtensionsTests
    {
        private readonly ServiceProviderMock serviceProviderFixture;
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBaseExtensionsTests"/> class.
        /// </summary>
        public ApiBaseExtensionsTests()
        {
            serviceProviderFixture = new ServiceProviderMock();
            serviceProvider = serviceProviderFixture.ServiceProvider.Object;
        }

        /// <summary>
        /// Tests whether GetApiService works.
        /// </summary>
        [TestMethod]
        public void CanCallGetApiService()
        {
            var api = new TestApi(serviceProvider);
            var result = api.GetApiService<IQueryExpressionSourcer>();
            result.Should().BeAssignableTo<IQueryExpressionSourcer>();
        }

        /// <summary>
        /// Tests that the first argument of GetApiService cannot be null.
        /// </summary>
        [TestMethod]
        public void CannotCallGetApiServiceWithNullApi()
        {
            Action act = () => default(ApiBase).GetApiService<IQueryExpressionSourcer>();
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Tests that HasProperty can be called.
        /// </summary>
        [TestMethod]
        public void CanCallHasProperty()
        {
            var api = new TestApi(serviceProvider);
            var name = "TestValue183810822";
            api.SetProperty(name, "Test");
            var result = api.HasProperty(name);
            result.Should().BeTrue("Property has to be set");
        }

        /// <summary>
        /// Tests that the first argument of HasProperty cannot be null.
        /// </summary>
        [TestMethod]
        public void CannotCallHasPropertyWithNullApi()
        {
            Action act = () => default(ApiBase).HasProperty("TestValue1698394406");
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Tests invalid property names for the HasProperty method.
        /// </summary>
        /// <param name="value">The invalid values.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallHasPropertyWithInvalidName(string value)
        {
            Action act = () => new TestApi(serviceProvider).HasProperty(value);
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Can call the get method and get the property.
        /// </summary>
        [TestMethod]
        public void CanCallGetPropertyWithTAndApiBaseAndString()
        {
            var api = new TestApi(serviceProvider);
            var name = "TestValue183810822";
            var expected = "Test";
            api.SetProperty(name, expected);
            var result = api.GetProperty<string>(name);
            expected.Should().Be(result);
        }

        /// <summary>
        /// Cannnot call GetProperty with a first argument that is null.
        /// </summary>
        [TestMethod]
        public void CannotCallGetPropertyWithTAndApiBaseAndStringWithNullApi()
        {
            Action act = () => default(ApiBase).GetProperty<string>("TestValue1576834621");
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call GetProperty with an invalid property name.
        /// </summary>
        /// <param name="value">The property name.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallGetPropertyWithTAndApiBaseAndStringWithInvalidName(string value)
        {
            Action act = () => new TestApi(serviceProvider).GetProperty<string>(value);
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Cannnot call GetProperty with a first argument that is null.
        /// </summary>
        [TestMethod]
        public void CanCallGetPropertyWithApiBaseAndString()
        {
            var api = new TestApi(serviceProvider);
            var name = "TestValue183810822";
            var expected = "Test";
            api.SetProperty(name, expected);
            var result = api.GetProperty(name);
            expected.Should().Be(result as string);
        }

        /// <summary>
        /// Cannnot call GetProperty with a first argument that is null.
        /// </summary>
        [TestMethod]
        public void CannotCallGetPropertyWithApiBaseAndStringWithNullApi()
        {
            Action act = () => default(ApiBase).GetProperty("TestValue1431338836");
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call GetProperty with an invalid property name.
        /// </summary>
        /// <param name="value">The property name.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallGetPropertyWithApiBaseAndStringWithInvalidName(string value)
        {
            Action act = () => new TestApi(serviceProvider).GetProperty(value);
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Can call set property.
        /// </summary>
        [TestMethod]
        public void CanCallSetProperty()
        {
            var api = new TestApi(serviceProvider);
            var name = "TestValue183810822";
            var expected = "Test";
            api.SetProperty(name, expected);
            var result = api.GetProperty<string>(name);
            expected.Should().Be(result);
        }

        /// <summary>
        /// Cannnot call SetProperty with a first argument that is null.
        /// </summary>
        [TestMethod]
        public void CannotCallSetPropertyWithNullApi()
        {
            Action act = () => default(ApiBase).SetProperty("TestValue1247347624", new object());
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call SetProperty with an invalid property name.
        /// </summary>
        /// <param name="value">The property name.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallSetPropertyWithInvalidName(string value)
        {
            Action act = () => new TestApi(serviceProvider).SetProperty(value, new object());
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Can call remove property.
        /// </summary>
        [TestMethod]
        public void CanCallRemoveProperty()
        {
            var api = new TestApi(serviceProvider);
            var name = "TestValue183810822";
            var expected = "Test";
            api.SetProperty(name, expected);
            api.RemoveProperty(name);
            var result = api.GetProperty<string>(name);
            result.Should().BeNull();
        }

        /// <summary>
        /// Cannnot call RemoveProperty with a first argument that is null.
        /// </summary>
        [TestMethod]
        public void CannotCallRemovePropertyWithNullApi()
        {
            Action act = () => default(ApiBase).RemoveProperty("TestValue466003014");
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call RemoveProperty with an invalid property name.
        /// </summary>
        /// <param name="value">The property name.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallRemovePropertyWithInvalidName(string value)
        {
            Action act = () => new TestApi(serviceProvider).RemoveProperty(value);
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Can call GetModelAsync().
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public void CanCallGetModel()
        {
            var api = new TestApi(serviceProvider);
            var cancellationToken = CancellationToken.None;
            var result = api.GetModel();
            result.Should().NotBeNull();
        }

        /// <summary>
        /// Cannnot call GetModelAsync with a first argument that is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public void CannotCallGetModelWithNullApi()
        {
            Action act = () => default(ApiBase).GetModel();
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call GetQueryAbleSource.
        /// </summary>
        [TestMethod]
        public void CanCallGetQueryableSourceWithApiBaseAndStringAndArrayOfObject()
        {
            var api = new TestApi(serviceProvider);
            var name = "Tests";
            Type expectedType = typeof(Test);

            serviceProviderFixture.ModelMapper.Setup(x => x.TryGetRelevantType(It.IsAny<ModelContext>(), name, out expectedType)).Returns(true);

            var arguments = new[] { new object(), new object(), new object() };
            var result = api.GetQueryableSource(name, arguments);

            result.Should().BeAssignableTo<IQueryable<Test>>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource with a first argument that is null.
        /// </summary>
        [TestMethod]
        public void CannotCallGetQueryableSourceWithApiBaseAndStringAndArrayOfObjectWithNullApi()
        {
            Action act = () => default(ApiBase).GetQueryableSource("TestValue119728298", new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource with an invalid ElementType name.
        /// </summary>
        /// <param name="value">The element Type name.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallGetQueryableSourceWithApiBaseAndStringAndArrayOfObjectWithInvalidName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(serviceProvider).GetQueryableSource(value, new[] { new object(), new object(), new object() });
                    act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(serviceProvider).GetQueryableSource(value, new[] { new object(), new object(), new object() });
                    act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Can call GetQueryAbleSource with a namespace.
        /// </summary>
        [TestMethod]
        public void CanCallGetQueryableSourceWithApiBaseAndStringAndStringAndArrayOfObject()
        {
            var api = new TestApi(serviceProvider);
            var namespaceName = "Microsoft.Restier.Tests.Core";
            var name = "Tests";
            Type expectedType = typeof(Test);

            serviceProviderFixture.ModelMapper.Setup(x => x.TryGetRelevantType(It.IsAny<ModelContext>(), namespaceName, name, out expectedType)).Returns(true);

            var arguments = new[] { new object(), new object(), new object() };
            var result = api.GetQueryableSource(namespaceName, name, arguments);

            result.Should().BeAssignableTo<IQueryable<Test>>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource with a first argument that is null.
        /// </summary>
        [TestMethod]
        public void CannotCallGetQueryableSourceWithApiBaseAndStringAndStringAndArrayOfObjectWithNullApi()
        {
            Action act = () => default(ApiBase).GetQueryableSource("TestValue486544476", "TestValue2009865785", new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource with an invalid namespace name.
        /// </summary>
        /// <param name="value">The namespace name.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallGetQueryableSourceWithApiBaseAndStringAndStringAndArrayOfObjectWithInvalidNamespaceName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(serviceProvider).GetQueryableSource(value, "TestValue1716986786", new[] { new object(), new object(), new object() });
                    act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(serviceProvider).GetQueryableSource(value, "TestValue1716986786", new[] { new object(), new object(), new object() });
                    act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource with an invalid ElementType name.
        /// </summary>
        /// <param name="value">The element Type name.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallGetQueryableSourceWithApiBaseAndStringAndStringAndArrayOfObjectWithInvalidName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(serviceProvider).GetQueryableSource("TestValue1228629775", value, new[] { new object(), new object(), new object() }); act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(serviceProvider).GetQueryableSource("TestValue1228629775", value, new[] { new object(), new object(), new object() }); act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Can call GetQueryAbleSource`1[TElement].
        /// </summary>
        [TestMethod]
        public void CanCallGetQueryableSourceWithTElementAndApiBaseAndStringAndArrayOfObject()
        {
            var api = new TestApi(serviceProvider);
            var name = "Tests";
            Type expectedType = typeof(Test);

            serviceProviderFixture.ModelMapper.Setup(x => x.TryGetRelevantType(It.IsAny<ModelContext>(), name, out expectedType)).Returns(true);

            var arguments = new[] { new object(), new object(), new object() };
            var result = api.GetQueryableSource<Test>(name, arguments);

            result.Should().BeAssignableTo<IQueryable<Test>>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource`1[TElement]. with an invalid TElement type.
        /// </summary>
        [TestMethod]
        public void CannotCallGetQueryableSourceWithInvalidTElement()
        {
            var api = new TestApi(serviceProvider);
            var name = "Tests";
            Type expectedType = typeof(Test);

            serviceProviderFixture.ModelMapper.Setup(x => x.TryGetRelevantType(It.IsAny<ModelContext>(), name, out expectedType)).Returns(true);

            var arguments = new[] { new object(), new object(), new object() };

            Action act = () => api.GetQueryableSource<ApiBaseExtensionsTests>(name, new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource`1[TElement]. with a first argument that is null.
        /// </summary>
        [TestMethod]
        public void CannotCallGetQueryableSourceWithTElementAndApiBaseAndStringAndArrayOfObjectWithNullApi()
        {
            Action act = () => default(ApiBase).GetQueryableSource<Test>("TestValue2056669437", new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource`1[TElement]. with an invalid ElementType name.
        /// </summary>
        /// <param name="value">The element Type name.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallGetQueryableSourceWithTElementAndApiBaseAndStringAndArrayOfObjectWithInvalidName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(serviceProvider).GetQueryableSource<Test>(value, new[] { new object(), new object(), new object() }); act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(serviceProvider).GetQueryableSource<Test>(value, new[] { new object(), new object(), new object() }); act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Can call GetQueryAbleSource`1[TElement].
        /// </summary>
        [TestMethod]
        public void CanCallGetQueryableSourceWithTElementAndApiBaseAndStringAndStringAndArrayOfObject()
        {
            var api = new TestApi(serviceProvider);
            var namespaceName = "Microsoft.Restier.Tests.Core";
            var name = "Tests";
            Type expectedType = typeof(Test);

            serviceProviderFixture.ModelMapper.Setup(x => x.TryGetRelevantType(It.IsAny<ModelContext>(), namespaceName, name, out expectedType)).Returns(true);

            var arguments = new[] { new object(), new object(), new object() };
            var result = api.GetQueryableSource<Test>(namespaceName, name, arguments);

            result.Should().BeAssignableTo<IQueryable<Test>>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource`1[TElement]. with an invalid TElement type.
        /// </summary>
        [TestMethod]
        public void CannotCallGetQueryableSourceWithInvalidTElementAndNamespace()
        {
            var api = new TestApi(serviceProvider);
            var namespaceName = "Microsoft.Restier.Tests.Core";
            var name = "Tests";
            Type expectedType = typeof(Test);

            serviceProviderFixture.ModelMapper.Setup(x => x.TryGetRelevantType(It.IsAny<ModelContext>(), namespaceName, name, out expectedType)).Returns(true);

            var arguments = new[] { new object(), new object(), new object() };

            Action act = () => api.GetQueryableSource<ApiBaseExtensionsTests>(namespaceName, name, new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Cannnot call GetQueryAbleSource with a first argument that is null.
        /// </summary>
        [TestMethod]
        public void CannotCallGetQueryableSourceWithTElementAndApiBaseAndStringAndStringAndArrayOfObjectWithNullApi()
        {
            Action act = () => default(ApiBase).GetQueryableSource<Test>("TestValue1686186750", "TestValue1325825672", new[] { new object(), new object(), new object() });
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource`1[TElement]. with an invalid namespace name.
        /// </summary>
        /// <param name="value">The namespace name.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallGetQueryableSourceWithTElementAndApiBaseAndStringAndStringAndArrayOfObjectWithInvalidNamespaceName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(serviceProvider).GetQueryableSource<Test>(value, "TestValue1716986786", new[] { new object(), new object(), new object() }); act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(serviceProvider).GetQueryableSource<Test>(value, "TestValue1716986786", new[] { new object(), new object(), new object() }); act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Cannot call GetQueryAbleSource`1[TElement] with an invalid ElementType name.
        /// </summary>
        /// <param name="value">The element Type name.</param>
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void CannotCallGetQueryableSourceWithTElementAndApiBaseAndStringAndStringAndArrayOfObjectWithInvalidName(string value)
        {
            switch (value)
            {
                case null:
                    Action act = () => new TestApi(serviceProvider).GetQueryableSource("TestValue1228629775", value, new[] { new object(), new object(), new object() }); act.Should().Throw<ArgumentNullException>();
                    break;
                default:
                    act = () => new TestApi(serviceProvider).GetQueryableSource("TestValue1228629775", value, new[] { new object(), new object(), new object() }); act.Should().Throw<NotSupportedException>();
                    break;
            }
        }

        /// <summary>
        /// Can call QueryAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallQueryAsync()
        {
            var api = new TestApi(serviceProvider);

            serviceProviderFixture.QueryExecutor
                .Setup(x => x.ExecuteQueryAsync<Test>(It.IsAny<QueryContext>(), It.IsAny<IQueryable<Test>>(), It.IsAny<CancellationToken>()))
                .Returns<QueryContext, IQueryable<Test>, CancellationToken>((qc, iq, c) =>
                {
                    return Task.FromResult(new QueryResult(iq));
                });

            IQueryable<Test> queryable = new List<Test>()
            {
                new Test() { Name = "The", },
                new Test() { Name = "Quick", },
                new Test() { Name = "Brown", },
                new Test() { Name = "Fox", },
            }.AsQueryable();

            var source = Expression.Constant(queryable);
            var request = new QueryRequest(new QueryableSource<Test>(source));

            var cancellationToken = CancellationToken.None;
            var result = await api.QueryAsync(request, cancellationToken);
            result.Results.Should().BeEquivalentTo(queryable);
        }

        /// <summary>
        /// Cannot call QueryAsync with a null first argument.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallQueryAsyncWithNullApi()
        {
            var request = new QueryRequest(new QueryableSource<Test>(new Mock<Expression>().Object));
            Func<Task> act = () => default(ApiBase).QueryAsync(request, CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call QueryAsync with a null Query request.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallQueryAsyncWithNullRequest()
        {
            Func<Task> act = () => new TestApi(serviceProvider).QueryAsync(default(QueryRequest), CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

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