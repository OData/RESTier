// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
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
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Query
{
    /// <summary>
    /// Unit tests for <see cref="KeylessViewQueryExpressionSourcer"/>. These cover
    /// the registry-hit / miss / non-stub-reference branches and the
    /// tracking-behavior passthrough on <see cref="RestierEFTrackingBehavior.TrackAll"/>.
    /// The full tracking decision matrix (NoTracking / Default /
    /// NoTrackingWithIdentityResolution against an actual EF queryable) is covered
    /// by the higher-level EFCore integration tests added in Task 8 — those
    /// branches require a real DbContext because <c>ApplyTracking</c> reflects
    /// on EF-specific extension methods that throw on a plain in-memory
    /// <see cref="IQueryable"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class KeylessViewQueryExpressionSourcerTests
    {
        private const string ViewName = "FakeView";

        private readonly IQueryHandler queryHandler = Substitute.For<IQueryHandler>();
        private readonly ISubmitHandler submitHandler = Substitute.For<ISubmitHandler>();

        /// <summary>
        /// Registry knows the view, tracking not requested → returns a
        /// <c>ConstantExpression</c> whose value is the IQueryable produced
        /// by the registered <see cref="KeylessViewEntry.SourceFactory"/>.
        /// </summary>
        [TestMethod]
        public void ReplaceQueryableSource_RegistryHit_TrackingNotRequested_ReturnsConstantOfFactoryResult()
        {
            // Arrange
            var underlying = new[] { new FakeView { Id = 1 } }.AsQueryable();
            var registry = new KeylessViewRegistry();
            registry.Register(ViewName, typeof(FakeView), _ => underlying);

            var (context, _) = BuildFunctionImportContext(ViewName, allowNoTracking: false);
            var sourcer = new KeylessViewQueryExpressionSourcer(registry, new RestierEFOptions());

            // Act
            var result = sourcer.ReplaceQueryableSource(context, embedded: false);

            // Assert
            result.Should().BeOfType<ConstantExpression>();
            ((ConstantExpression)result).Value.Should().BeSameAs(underlying);
        }

        /// <summary>
        /// Registry is empty for the view name → returns <c>null</c> so the
        /// chain falls through to the next sourcer.
        /// </summary>
        [TestMethod]
        public void ReplaceQueryableSource_RegistryMiss_ReturnsNull()
        {
            // Arrange — empty registry.
            var registry = new KeylessViewRegistry();
            var (context, _) = BuildFunctionImportContext(ViewName, allowNoTracking: false);
            var sourcer = new KeylessViewQueryExpressionSourcer(registry, new RestierEFOptions());

            // Act
            var result = sourcer.ReplaceQueryableSource(context, embedded: false);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// The visited node is not a DataSourceStub call — for example, a
        /// regular EntitySet reference reaching the EF sourcer chain — so
        /// the keyless-view sourcer must return <c>null</c> and let the
        /// next link handle it.
        /// </summary>
        [TestMethod]
        public void ReplaceQueryableSource_NotADataSourceStubReference_ReturnsNull()
        {
            // Arrange — a QueryExpressionContext with no visited node yields
            // a null ModelReference, which fails the DataSourceStubModelReference
            // type-check inside the sourcer.
            var registry = new KeylessViewRegistry();
            registry.Register(ViewName, typeof(FakeView), _ => Enumerable.Empty<FakeView>().AsQueryable());

            var api = new TestApi(BuildEdmModel(ViewName), queryHandler, submitHandler);
            var queryable = new[] { new FakeView { Id = 1 } }.AsQueryable();
            var queryRequest = new QueryRequest(queryable);
            var queryContext = new QueryContext(api, queryRequest);
            SetQueryContextModel(queryContext, api.Model);
            var queryExpressionContext = new QueryExpressionContext(queryContext);
            // Intentionally do NOT call PushVisitedNode → ModelReference is null.

            var sourcer = new KeylessViewQueryExpressionSourcer(registry, new RestierEFOptions());

            // Act
            var result = sourcer.ReplaceQueryableSource(queryExpressionContext, embedded: false);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tracking is requested with <see cref="RestierEFTrackingBehavior.TrackAll"/>:
        /// the sourcer must run ApplyTracking but return the original IQueryable
        /// unmodified — TrackAll is a passthrough on both EF flavours. The other
        /// tracking-behavior cases are covered by Task 8's EFCore integration
        /// tests because ApplyTracking reflects on EF-specific extension methods
        /// that don't compose against a plain in-memory IQueryable.
        /// </summary>
        [TestMethod]
        public void ReplaceQueryableSource_AllowNoTrackingTrue_TrackAll_PassesThrough()
        {
            // Arrange
            var underlying = new[] { new FakeView { Id = 1 } }.AsQueryable();
            var registry = new KeylessViewRegistry();
            registry.Register(ViewName, typeof(FakeView), _ => underlying);

            var (context, _) = BuildFunctionImportContext(ViewName, allowNoTracking: true);
            var options = new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.TrackAll };
            var sourcer = new KeylessViewQueryExpressionSourcer(registry, options);

            // Act
            var result = sourcer.ReplaceQueryableSource(context, embedded: false);

            // Assert — TrackAll is `case TrackAll: return dbSet;` in ApplyTracking,
            // so the constant wraps the same IQueryable instance as the no-tracking
            // case.
            result.Should().BeOfType<ConstantExpression>();
            ((ConstantExpression)result).Value.Should().BeSameAs(underlying);
        }

        /// <summary>
        /// If the inner sourcer in the chain produced a result, the keyless-view
        /// sourcer must return it untouched and never consult the registry.
        /// </summary>
        [TestMethod]
        public void ReplaceQueryableSource_InnerProducesResult_ShortCircuits()
        {
            // Arrange
            var registry = new KeylessViewRegistry();
            // NB: registry intentionally left empty — must not be consulted.

            var (context, _) = BuildFunctionImportContext(ViewName, allowNoTracking: false);
            var sourcer = new KeylessViewQueryExpressionSourcer(registry, new RestierEFOptions());

            var innerResult = Expression.Constant(42);
            var inner = Substitute.For<IQueryExpressionSourcer>();
            inner.ReplaceQueryableSource(context, false).Returns(innerResult);
            sourcer.Inner = inner;

            // Act
            var result = sourcer.ReplaceQueryableSource(context, embedded: false);

            // Assert
            result.Should().BeSameAs(innerResult);
        }

        private (QueryExpressionContext context, IEdmModel model) BuildFunctionImportContext(
            string viewName,
            bool allowNoTracking)
        {
            var model = BuildEdmModel(viewName);
            var api = new TestApi(model, queryHandler, submitHandler);
            var queryable = new[] { new FakeView { Id = 1 } }.AsQueryable();
            var queryRequest = new QueryRequest(queryable);
            // QueryRequest.AllowNoTracking is internal-set; reach it via reflection from this test project.
            if (allowNoTracking)
            {
                typeof(QueryRequest)
                    .GetProperty(nameof(QueryRequest.AllowNoTracking))
                    .SetValue(queryRequest, true);
            }

            var queryContext = new QueryContext(api, queryRequest);
            SetQueryContextModel(queryContext, model);
            var queryExpressionContext = new QueryExpressionContext(queryContext);

            var getQueryableSource = typeof(DataSourceStub)
                .GetMethods()
                .Single(m => m.Name == nameof(DataSourceStub.GetQueryableSource)
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(FakeView));

            var stubCall = Expression.Call(
                null,
                getQueryableSource,
                Expression.Constant(viewName),
                Expression.Constant(Array.Empty<object>(), typeof(object[])));
            queryExpressionContext.PushVisitedNode(stubCall);

            return (queryExpressionContext, model);
        }

        /// <summary>
        /// QueryContext.Model has an internal setter; this test project does not
        /// have InternalsVisibleTo into Microsoft.Restier.Core, so we route the
        /// assignment through reflection rather than a property initializer.
        /// </summary>
        private static void SetQueryContextModel(QueryContext queryContext, IEdmModel model)
        {
            typeof(QueryContext)
                .GetProperty(nameof(QueryContext.Model))
                .SetValue(queryContext, model);
        }

        private static IEdmModel BuildEdmModel(string viewName)
        {
            var edmModel = new EdmModel();
            var complexType = new EdmComplexType("TestNs", "FakeView");
            complexType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32);
            edmModel.AddElement(complexType);
            edmModel.SetAnnotationValue(complexType, new ClrTypeAnnotation(typeof(FakeView)));

            var returnTypeRef = new EdmCollectionTypeReference(
                new EdmCollectionType(new EdmComplexTypeReference(complexType, isNullable: true)));
            var function = new EdmFunction(
                "TestNs.Views",
                viewName,
                returnTypeRef,
                isBound: false,
                entitySetPathExpression: null,
                isComposable: false);
            edmModel.AddElement(function);

            var container = new EdmEntityContainer("TestNs", "Container");
            container.AddFunctionImport(viewName, function);
            edmModel.AddElement(container);

            return edmModel;
        }

        private class TestApi : ApiBase
        {
            public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class FakeView
        {
            public int Id { get; set; }
        }
    }
}
