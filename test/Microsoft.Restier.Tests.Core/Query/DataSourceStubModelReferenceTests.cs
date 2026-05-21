// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Unit tests for the <see cref="DataSourceStubModelReference"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class DataSourceStubModelReferenceTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;

        private readonly IQueryable<Test> queryable = new List<Test>()
        {
            new Test() { Name = "The" },
            new Test() { Name = "Quick" },
            new Test() { Name = "Brown" },
            new Test() { Name = "Fox" },
        }.AsQueryable();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceStubModelReferenceTests"/> class.
        /// </summary>
         public DataSourceStubModelReferenceTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
        }

        /// <summary>
        /// Tests whether the DataSourceStubModelReference can be constructed.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var queryContext = new QueryContext(
                new TestApi(model, queryHandler,submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");
            testClass.Should().NotBeNull();
        }

        /// <summary>
        /// Tests whether the DataSourceStubModelReference can be constructed.
        /// </summary>
        [TestMethod]
        public void CanConstructWithNamespace()
        {
            var queryContext = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));
            var testClass = new DataSourceStubModelReference(
                queryContext, "Microsoft.Restier.Tests.Core.Query", "Tests");
            testClass.Should().NotBeNull();
        }

        /// <summary>
        /// Can Get an EntitySet.
        /// </summary>
        [TestMethod]
        public void CanGetEntitySet()
        {
            var model = Substitute.For<IEdmModel>();
            var entityContainer = Substitute.For<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItem = Substitute.For<IEdmEntityContainerElement, IEdmEntitySet>();
            entityContainerElementItem.Name.Returns("Tests");
            var edmEntitySet = entityContainerElementItem.As<IEdmEntitySet>();
            list.Add(entityContainerElementItem);

            model.EntityContainer.Returns(entityContainer);
            entityContainer.Elements.Returns(list);

            var queryContext = new QueryContext(
                new TestApi(this.model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))))
            {
                Model = model,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.EntitySet.Should().BeAssignableTo<IEdmEntitySet>();
        }

        /// <summary>
        /// Cannot get an EntitySet.
        /// </summary>
        [TestMethod]
        public void CannotGetEntitySet()
        {
            var model = Substitute.For<IEdmModel>();
            var entityContainer = Substitute.For<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItem = Substitute.For<IEdmEntityContainerElement>();
            entityContainerElementItem.Name.Returns("Tests");
            var edmEntitySet = entityContainerElementItem.As<IEdmEntitySet>();
            list.Add(entityContainerElementItem);

            model.EntityContainer.Returns(entityContainer);
            entityContainer.Elements.Returns(list);

            var queryContext = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))))
            {
                Model = model,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.EntitySet.Should().BeNull();
        }

        /// <summary>
        /// Can get the Edm Type from an IEdmNavigationSource.
        /// </summary>
        [TestMethod]
        public void CanGetTypeIEdmNavigationSource()
        {
            var model = Substitute.For<IEdmModel>();
            var entityContainer = Substitute.For<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItem = Substitute.For<IEdmEntityContainerElement, IEdmNavigationSource>();
            entityContainerElementItem.Name.Returns("Tests");
            var source = entityContainerElementItem.As<IEdmNavigationSource>();
            var edmType = Substitute.For<IEdmType>();
            source.Type.Returns(edmType);

            list.Add(entityContainerElementItem);

            model.EntityContainer.Returns(entityContainer);
            entityContainer.Elements.Returns(list);

            var queryContext = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))))
            {
                Model = model,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.Type.Should().BeAssignableTo<IEdmType>();
            testClass.Type.Should().Be(edmType);
        }

        /// <summary>
        /// Can get the Edm Type from an IEdmFunctionImport.
        /// </summary>
        [TestMethod]
        public void CanGetTypeIEdmFunctionImport()
        {
            var model = Substitute.For<IEdmModel>();
            var entityContainer = Substitute.For<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItem = Substitute.For<IEdmEntityContainerElement, IEdmFunctionImport>();
            entityContainerElementItem.Name.Returns("Tests");
            var source = entityContainerElementItem.As<IEdmFunctionImport>();
            var edmType = Substitute.For<IEdmType>();

#pragma warning disable CS0618 // ReturnType is obsolete but is what GetReturn() reads under the hood
            source.Function.ReturnType.Definition.Returns(edmType);
#pragma warning restore CS0618
            list.Add(entityContainerElementItem);

            model.EntityContainer.Returns(entityContainer);
            entityContainer.Elements.Returns(list);

            var queryContext = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))))
            {
                Model = model,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.Type.Should().BeAssignableTo<IEdmType>();
            testClass.Type.Should().Be(edmType);
        }

        /// <summary>
        /// Can get the Edm Type from an IEdmFunction.
        /// </summary>
        [TestMethod]
        public void CanGetTypeIEdmFunction()
        {
            var model = Substitute.For<IEdmModel>();
            var entityContainer = Substitute.For<IEdmEntityContainer>();
            var list = new List<IEdmSchemaElement>();
            var schemaElement = Substitute.For<IEdmSchemaElement, IEdmFunction>();
            schemaElement.Name.Returns("Tests");
            schemaElement.Namespace.Returns("Microsoft.Restier.Tests.Core.Query");
            var source = schemaElement.As<IEdmFunction>();
            var edmType = Substitute.For<IEdmType>();

#pragma warning disable CS0618 // ReturnType is obsolete but is what GetReturn() reads under the hood
            source.ReturnType.Definition.Returns(edmType);
#pragma warning restore CS0618
            list.Add(schemaElement);

            model.SchemaElements.Returns(list);

            var queryContext = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))))
            {
                Model = model,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Microsoft.Restier.Tests.Core.Query", "Tests");

            testClass.Type.Should().BeAssignableTo<IEdmType>();
            testClass.Type.Should().Be(edmType);
        }

        /// <summary>
        /// Cannot get the Edm Type.
        /// </summary>
        [TestMethod]
        public void CannotGetType()
        {
            var model = Substitute.For<IEdmModel>();
            var entityContainer = Substitute.For<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItem = Substitute.For<IEdmEntityContainerElement>();
            entityContainerElementItem.Name.Returns("Tests");
            list.Add(entityContainerElementItem);

            model.EntityContainer.Returns(entityContainer);
            entityContainer.Elements.Returns(list);

            var queryContext = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))))
            {
                Model = model,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.Type.Should().BeNull();
        }

        /// <summary>
        /// Can get an element.
        /// </summary>
        [TestMethod]
        public void CanGetElement()
        {
            var model = Substitute.For<IEdmModel>();
            var entityContainer = Substitute.For<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItem = Substitute.For<IEdmEntityContainerElement>();
            entityContainerElementItem.Name.Returns("Tests");
            list.Add(entityContainerElementItem);

            model.EntityContainer.Returns(entityContainer);
            entityContainer.Elements.Returns(list);

            var queryContext = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))))
            {
                Model = model,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.Element.Should().BeAssignableTo<IEdmElement>();
        }

        /// <summary>
        /// Cannot get an element.
        /// </summary>
        [TestMethod]
        public void CannotGetElement()
        {
            var model = Substitute.For<IEdmModel>();
            var entityContainer = Substitute.For<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItem = Substitute.For<IEdmEntityContainerElement>();
            entityContainerElementItem.Name.Returns("Testing");
            list.Add(entityContainerElementItem);

            model.EntityContainer.Returns(entityContainer);
            entityContainer.Elements.Returns(list);

            var queryContext = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))))
            {
                Model = model,
            };
            var testClass = new DataSourceStubModelReference(
                queryContext, "Tests");

            testClass.Element.Should().BeNull();
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
