// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based query expression processor which will apply OnFilter logic into query expression.
    /// </summary>
    public class ConventionBasedQueryExpressionProcessor : IQueryExpressionProcessor
    {
        private readonly Type targetApiType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedQueryExpressionProcessor"/> class.
        /// </summary>
        /// <param name="targetApiType">The target type to check for filter functions.</param>
        public ConventionBasedQueryExpressionProcessor(Type targetApiType)
        {
            Ensure.NotNull(targetApiType, nameof(targetApiType));
            this.targetApiType = targetApiType;
        }

        /// <summary>
        /// Gets a reference to an inner query expression processor in case they are chained.
        /// </summary>
        public IQueryExpressionProcessor Inner { get; set; }

        /// <inheritdoc/>
        public Expression Process(QueryExpressionContext context)
        {
            Ensure.NotNull(context, nameof(context));

            if (Inner is not null)
            {
                var innerProcessedExpression = Inner.Process(context);
                if (innerProcessedExpression is not null && innerProcessedExpression != context.VisitedNode)
                {
                    return innerProcessedExpression;
                }
            }

            if (context.ModelReference is DataSourceStubModelReference dataSourceStubReference)
            {
                if (dataSourceStubReference.Element is IEdmEntitySet entitySet)
                {
                    if (entitySet.Type is not IEdmCollectionType entityCollectionType)
                    {
                        return null;
                    }

                    if (entityCollectionType.ElementType.Definition is not IEdmEntityType entityType)
                    {
                        return null;
                    }

                    var expectedMethodName = ConventionBasedMethodNameFactory.GetEntitySetMethodName(
                        entitySet, RestierPipelineState.Submit, RestierEntitySetOperation.Filter);
                    return AppendOnFilterExpression(context, expectedMethodName, entitySet.Name, entityType);
                }

                if (dataSourceStubReference.Element is IEdmFunctionImport functionImport)
                {
                    // Keyless-view shape: an unbound function import returning Collection(<ComplexType>).
                    // Routes through the shared OnFilter pipeline using a method name produced by
                    // GetFunctionImportMethodName (e.g. OnFilterBooksByPublisher).
                    var returnTypeRef = functionImport.Function.GetReturn()?.Type;
                    if (returnTypeRef is null || !returnTypeRef.IsCollection())
                    {
                        return null;
                    }

                    if (returnTypeRef.Definition is not IEdmCollectionType complexCollectionType)
                    {
                        return null;
                    }

                    if (complexCollectionType.ElementType.Definition is not IEdmComplexType complexType)
                    {
                        return null;
                    }

                    var expectedMethodName = ConventionBasedMethodNameFactory.GetFunctionImportMethodName(
                        functionImport.Name, RestierPipelineState.Submit, RestierEntitySetOperation.Filter);
                    if (string.IsNullOrEmpty(expectedMethodName))
                    {
                        return null;
                    }

                    return AppendOnFilterExpression(context, expectedMethodName, functionImport.Name, complexType);
                }

                return null;
            }

            if (context.ModelReference is PropertyModelReference propertyModelReference && propertyModelReference.Property is not null)
            {
                // Could be a single navigation property or a collection navigation property
                var propType = propertyModelReference.Property.Type;
                if (propType is IEdmCollectionTypeReference collectionTypeReference)
                {
                    var collectionType = collectionTypeReference.Definition as IEdmCollectionType;
                    propType = collectionType.ElementType;
                }

                if (propType.Definition is not IEdmEntityType entityType)
                {
                    return null;
                }

                // In case of type inheritance, get the base type
                while (entityType.BaseType is not null)
                {
                    entityType = (IEdmEntityType)entityType.BaseType;
                }

                // Get the model, query it for the entity set of a given type.
                var entitySet = context.QueryContext.Model.EntityContainer.EntitySets().FirstOrDefault(c => c.EntityType == entityType);
                if (entitySet is null)
                {
                    return null;
                }

                var expectedMethodName = ConventionBasedMethodNameFactory.GetEntitySetMethodName(
                    entitySet, RestierPipelineState.Submit, RestierEntitySetOperation.Filter);
                return AppendOnFilterExpression(context, expectedMethodName, entitySet.Name, entityType);
            }

            return null;
        }

        private Expression AppendOnFilterExpression(QueryExpressionContext context, string expectedMethodName, string elementContainerName, IEdmType edmElementType)
        {
            var elementTypeName = (edmElementType as IEdmSchemaElement)?.Name;
            var expectedMethod = targetApiType.GetQualifiedMethod(expectedMethodName);
            if (expectedMethod is null || (!expectedMethod.IsFamily && !expectedMethod.IsFamilyOrAssembly))
            {
                if (expectedMethod is not null)
                {
                    Trace.WriteLine($"Restier Filter found '{expectedMethodName}' but it is inaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
                }
                else if (!string.IsNullOrEmpty(elementContainerName) && !string.IsNullOrEmpty(elementTypeName))
                {
                    var actualMethodName = expectedMethodName.Replace(elementContainerName, elementTypeName);
                    var actualMethod = targetApiType.GetQualifiedMethod(actualMethodName);
                    if (actualMethod is not null)
                    {
                        Trace.WriteLine($"BREAKING: Restier Filter expected'{expectedMethodName}' but found '{actualMethodName}'. Your method will not be called until you correct the method name.");
                    }
                }

                return null;
            }

            var parameter = expectedMethod.GetParameters().SingleOrDefault();
            if (parameter is null || parameter.ParameterType != expectedMethod.ReturnType)
            {
                return null;
            }

            object apiBase = null;
            if (!expectedMethod.IsStatic)
            {
                apiBase = context.QueryContext.Api;
                if (apiBase is null || !targetApiType.IsInstanceOfType(apiBase))
                {
                    return null;
                }
            }

            // The LINQ expression built below has four cases
            // For navigation property, just add a where condition from OnFilter method
            // For collection property, will be like "Param_0.Prop.AsQueryable().Where(...)"
            // For collection property of derived type, will be like "Param_0.Prop.AsQueryable().Where(...).OfType()"
            // For single navigation property, apply filter as conditional: predicate(entity) ? entity : null
            var returnType = context.VisitedNode.Type.FindGenericType(typeof(IQueryable<>));
            var enumerableQueryParameter = (object)context.VisitedNode;
            Type elementType;
            if (returnType is null)
            {
                // This means append for properties model reference
                var collectionType = context.VisitedNode.Type.FindGenericType(typeof(ICollection<>));
                if (collectionType is null)
                {
                    // Single navigation property case (e.g., Book.Publisher)
                    return ApplySingleNavigationFilter(context, expectedMethod, apiBase);
                }

                elementType = collectionType.GetGenericArguments()[0];
                returnType = typeof(IQueryable<>).MakeGenericType(elementType);

                enumerableQueryParameter = Expression.Call(ExpressionHelperMethods.QueryableAsQueryableGeneric.MakeGenericMethod(elementType), context.VisitedNode);
            }
            else
            {
                elementType = returnType.GetGenericArguments()[0];
            }

            var queryType = typeof(EnumerableQuery<>).MakeGenericType(elementType);
            var query = Activator.CreateInstance(queryType, enumerableQueryParameter);
            if (expectedMethod.Invoke(apiBase, new object[] { query }) is not IQueryable result)
            {
                return null;
            }

            if (expectedMethod.ReturnType == returnType)
            {
                if (result != query)
                {
                    return result.Expression;
                }
            }
            else
            {
                // This means calling onFilter against derived type and based type is returned
                // Need to convert back to derived type with OfType
                result = ExpressionHelpers.OfType(result, elementType);
                return result.Expression;
            }

            return null;
        }

        /// <summary>
        /// Applies an OnFilter method to a single navigation property by extracting the filter
        /// predicate and converting it to a conditional expression: predicate(entity) ? entity : null.
        /// </summary>
        private static Expression ApplySingleNavigationFilter(
            QueryExpressionContext context, MethodInfo expectedMethod, object apiBase)
        {
            // Only filter the navigation when it is consumed as an entity (i.e. projected/expanded),
            // where nulling it is meaningful and EF Core can translate the resulting CASE. When the
            // navigation node is instead the source of a member access — e.g. "book.Publisher.Id"
            // inside another predicate — replacing it with "predicate ? book.Publisher : null" would
            // both change the meaning of that scalar path and yield "(predicate ? entity : null).Member",
            // which the EF Core relational translator rejects (HTTP 500). Leave such references intact.
            // See BLR-7389 and https://github.com/OData/RESTier/issues/519.
            if (context.ParentNode is MemberExpression parentMember
                && parentMember.Expression == context.VisitedNode)
            {
                return null;
            }

            var elementType = context.VisitedNode.Type;
            var returnType = typeof(IQueryable<>).MakeGenericType(elementType);

            // Verify the expected method's return type is compatible
            if (expectedMethod.ReturnType != returnType)
            {
                return null;
            }

            // Create a dummy empty queryable to invoke the filter method and capture its expression
            var emptyArray = Array.CreateInstance(elementType, 0);
            var queryType = typeof(EnumerableQuery<>).MakeGenericType(elementType);
            var query = Activator.CreateInstance(queryType, new object[] { emptyArray });

            if (expectedMethod.Invoke(apiBase, new object[] { query }) is not IQueryable result)
            {
                return null;
            }

            // If the filter method didn't modify the query, no filter to apply
            if (result == query)
            {
                return null;
            }

            // Extract Where predicates from the filtered expression
            var predicate = ExtractCombinedPredicate(result.Expression, elementType);
            if (predicate is null)
            {
                return null;
            }

            // Replace the predicate's parameter with the actual entity expression
            var replacedBody = new ParameterReplacingVisitor(
                predicate.Parameters[0], context.VisitedNode).Visit(predicate.Body);

            // Build: predicate(entity) ? entity : default(EntityType)
            // This produces e.g. "book.Publisher.IsActive ? book.Publisher : null"
            return Expression.Condition(replacedBody, context.VisitedNode, Expression.Default(elementType));
        }

        /// <summary>
        /// Extracts the combined membership predicate from a queryable expression tree into a single
        /// lambda over <paramref name="elementType"/>. Chained <c>Where</c> calls combine with AND,
        /// <c>Union</c> branches combine with OR (set union is disjunction of membership), and other
        /// operators (e.g. <c>OrderBy</c>, <c>Distinct</c>) are transparent. Returns <c>null</c> when
        /// the tree contains no <c>Where</c> predicate at all (i.e. every row passes).
        /// </summary>
        private static LambdaExpression ExtractCombinedPredicate(Expression expression, Type elementType)
        {
            var parameter = Expression.Parameter(elementType, "entity");
            var body = ExtractPredicateBody(expression, parameter);
            return body is null ? null : Expression.Lambda(body, parameter);
        }

        /// <summary>
        /// Recursively builds a boolean expression over <paramref name="parameter"/> describing which
        /// rows the queryable in <paramref name="expression"/> keeps, or <c>null</c> when it filters
        /// nothing (every row passes). Handles <c>Where</c> (AND with its source), <c>Union</c>
        /// (OR of both branches — the branch that <c>ExtractCombinedPredicate</c> previously dropped),
        /// and passes through any other <see cref="Queryable"/> operator to its source.
        /// </summary>
        private static Expression ExtractPredicateBody(Expression expression, ParameterExpression parameter)
        {
            if (expression is not MethodCallExpression methodCall ||
                methodCall.Method.DeclaringType != typeof(Queryable))
            {
                // The chain has bottomed out at the source (e.g. the EnumerableQuery constant): no filter.
                return null;
            }

            if (methodCall.Method.Name == nameof(Queryable.Where))
            {
                var sourceBody = ExtractPredicateBody(methodCall.Arguments[0], parameter);

                var predicateArg = methodCall.Arguments[1];

                // Unwrap Quote expressions to get the underlying lambda
                if (predicateArg is UnaryExpression quote && quote.NodeType == ExpressionType.Quote)
                {
                    predicateArg = quote.Operand;
                }

                if (predicateArg is not LambdaExpression lambda)
                {
                    return sourceBody;
                }

                var thisBody = new ParameterReplacingVisitor(lambda.Parameters[0], parameter).Visit(lambda.Body);
                return sourceBody is null ? thisBody : Expression.AndAlso(sourceBody, thisBody);
            }

            if (methodCall.Method.Name == nameof(Queryable.Union) && methodCall.Arguments.Count == 2)
            {
                var left = ExtractPredicateBody(methodCall.Arguments[0], parameter);
                var right = ExtractPredicateBody(methodCall.Arguments[1], parameter);

                // If either branch filters nothing, their union keeps every row, so there is no
                // effective predicate to apply.
                if (left is null || right is null)
                {
                    return null;
                }

                return Expression.OrElse(left, right);
            }

            // Any other operator (OrderBy, Select, Distinct, OfType, ...) is transparent to membership.
            return ExtractPredicateBody(methodCall.Arguments[0], parameter);
        }

        /// <summary>
        /// An expression visitor that replaces all occurrences of a specific parameter with another expression.
        /// </summary>
        private class ParameterReplacingVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression oldParameter;
            private readonly Expression newExpression;

            public ParameterReplacingVisitor(ParameterExpression oldParameter, Expression newExpression)
            {
                this.oldParameter = oldParameter;
                this.newExpression = newExpression;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == oldParameter ? newExpression : base.VisitParameter(node);
            }
        }
    }
}
