// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Restier.Tests.Core.Query.RegressionTests.Issue771
{
    /// <summary>
    /// CLR base entity whose <see cref="System.Type.FullName"/> mirrors the EDM type's
    /// namespace + name, so <see cref="IEdmModel.FindDeclaredType(string)"/> can resolve
    /// the structured type from the operand of a <see cref="ExpressionType.TypeAs"/>.
    /// </summary>
    public class Issue771BaseEntity
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Derived CLR entity used to construct <c>($it As Issue771DerivedEntity).Narrator</c>
    /// against an <see cref="Issue771BaseEntity"/> parameter.
    /// </summary>
    public class Issue771DerivedEntity : Issue771BaseEntity
    {
        public string Narrator { get; set; }
    }

    /// <summary>
    /// Regression tests for https://github.com/OData/RESTier/issues/771.
    /// </summary>
    /// <remarks>
    /// Filters that cast to a derived type via OData's type-segment property path
    /// (e.g. <c>$filter=Namespace.Derived/Narrator eq '...'</c>) translate to a LINQ
    /// <c>Where($it =&gt; ($it As Derived).Narrator == "...")</c>. The default
    /// <see cref="ExpressionVisitor"/> visits a lambda's body before its parameters,
    /// so when <c>ComputeMemberModelReference</c> processed the <c>TypeAs</c> branch
    /// it looked up <c>$it</c> in the model-reference cache while it was still
    /// unregistered and passed the resulting null to the
    /// <c>PropertyModelReference</c> constructor — which threw
    /// <see cref="ArgumentNullException"/>.
    /// </remarks>
    [TestClass]
    public class Issue771_DerivedTypeFilterCastTests
    {
        [TestMethod]
        public void ComputeMemberModelReference_TypeAsToDerivedWithUncachedParameter_DoesNotThrow()
        {
            var model = BuildModelWithInheritance();
            var queryContext = BuildQueryContext(model);
            var context = new QueryExpressionContext(queryContext);

            var (whereCall, sourceCall, lambda, comparison, memberAccess) = BuildWhereCastExpression();

            // Replay the order the QueryExpressionVisitor would push nodes when traversing
            // Where($it => (($it As Derived).Narrator == "value")). The lambda body is
            // visited first (default ExpressionVisitor behavior), so the parameter $it has
            // not yet been pushed when the MemberExpression is reached.
            context.PushVisitedNode(whereCall);
            context.PushVisitedNode(sourceCall);
            context.PopVisitedNode();
            context.PushVisitedNode(lambda);
            context.PushVisitedNode(comparison);

            Action act = () => context.PushVisitedNode(memberAccess);

            act.Should().NotThrow(
                because: "issue #771 surfaced an ArgumentNullException from PropertyModelReference when the lambda parameter was not yet cached");
        }

        private static EdmModel BuildModelWithInheritance()
        {
            var model = new EdmModel();

            var baseType = new EdmEntityType(typeof(Issue771BaseEntity).Namespace, nameof(Issue771BaseEntity));
            var idProperty = baseType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32);
            baseType.AddKeys(idProperty);
            model.AddElement(baseType);

            var derivedType = new EdmEntityType(typeof(Issue771DerivedEntity).Namespace, nameof(Issue771DerivedEntity), baseType);
            derivedType.AddStructuralProperty("Narrator", EdmPrimitiveTypeKind.String);
            model.AddElement(derivedType);

            var container = new EdmEntityContainer("Issue771", "DefaultContainer");
            container.AddEntitySet("Bases", baseType);
            model.AddElement(container);

            return model;
        }

        private static QueryContext BuildQueryContext(IEdmModel model)
        {
            var api = new TestApi(model, Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
            var source = new QueryableSource<Issue771BaseEntity>(
                Expression.Constant(Array.Empty<Issue771BaseEntity>().AsQueryable()));
            return new QueryContext(api, new QueryRequest(source))
            {
                Model = model,
            };
        }

        private static (MethodCallExpression whereCall,
            MethodCallExpression sourceCall,
            LambdaExpression lambda,
            BinaryExpression comparison,
            MemberExpression memberAccess) BuildWhereCastExpression()
        {
            var getQueryableSource = typeof(DataSourceStub)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "GetQueryableSource" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(Issue771BaseEntity));

            var sourceCall = Expression.Call(
                getQueryableSource,
                Expression.Constant("Bases"),
                Expression.Constant(Array.Empty<object>()));

            var parameter = Expression.Parameter(typeof(Issue771BaseEntity), "it");
            var typeAs = Expression.TypeAs(parameter, typeof(Issue771DerivedEntity));
            var memberAccess = Expression.Property(typeAs, nameof(Issue771DerivedEntity.Narrator));
            var comparison = Expression.Equal(memberAccess, Expression.Constant("value"));
            var lambda = Expression.Lambda<Func<Issue771BaseEntity, bool>>(comparison, parameter);

            var whereMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == nameof(Queryable.Where)
                    && m.GetParameters().Length == 2
                    && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2)
                .MakeGenericMethod(typeof(Issue771BaseEntity));

            var whereCall = Expression.Call(whereMethod, sourceCall, lambda);
            return (whereCall, sourceCall, lambda, comparison, memberAccess);
        }

        private class TestApi : ApiBase
        {
            public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }
    }
}
