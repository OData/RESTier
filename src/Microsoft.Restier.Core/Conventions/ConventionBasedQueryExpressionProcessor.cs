// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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
                if (!(dataSourceStubReference.Element is IEdmEntitySet entitySet))
                {
                    return null;
                }

                if (!(entitySet.Type is IEdmCollectionType collectionType))
                {
                    return null;
                }

                if (!(collectionType.ElementType.Definition is IEdmEntityType entityType))
                {
                    return null;
                }

                return AppendOnFilterExpression(context, entitySet, entityType);
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

                if (!(propType.Definition is IEdmEntityType entityType))
                {
                    return null;
                }

                // In case of type inheritance, get the base type
                while (entityType.BaseType is not null)
                {
                    entityType = (IEdmEntityType)entityType.BaseType;
                }

                // Get the model, query it for the entity set of a given type.
                var entitySet = context.QueryContext.Model.EntityContainer.EntitySets().FirstOrDefault(c => c.EntityType() == entityType);
                if (entitySet is null)
                {
                    return null;
                }

                return AppendOnFilterExpression(context, entitySet, entityType);
            }

            return null;
        }

        private Expression AppendOnFilterExpression(QueryExpressionContext context, IEdmEntitySet entitySet, IEdmEntityType entityType)
        {
            var expectedMethodName = ConventionBasedMethodNameFactory.GetEntitySetMethodName(entitySet, RestierPipelineState.Submit, RestierEntitySetOperation.Filter);
            var expectedMethod = targetApiType.GetQualifiedMethod(expectedMethodName);
            if (expectedMethod is null || (!expectedMethod.IsFamily && !expectedMethod.IsFamilyOrAssembly))
            {
                if (expectedMethod is not null)
                {
                    Trace.WriteLine($"Restier Filter found '{expectedMethodName}' but it is inaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
                }
                else
                {
                    var actualMethodName = expectedMethodName.Replace(entitySet.Name, entityType.Name);
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

            // The LINQ expression built below has three cases
            // For navigation property, just add a where condition from OnFilter method
            // For collection property, will be like "Param_0.Prop.AsQueryable().Where(...)"
            // For collection property of derived type, will be like "Param_0.Prop.AsQueryable().Where(...).OfType()"
            var returnType = context.VisitedNode.Type.FindGenericType(typeof(IQueryable<>));
            var enumerableQueryParameter = (object)context.VisitedNode;
            Type elementType = null;

            if (returnType is null)
            {
                // This means append for properties model reference
                var collectionType = context.VisitedNode.Type.FindGenericType(typeof(ICollection<>));
                if (collectionType is null)
                {
                    return null;
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
            if (!(expectedMethod.Invoke(apiBase, new object[] { query }) is IQueryable result))
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
    }
}
