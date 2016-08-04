// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based query expression processor which will apply OnFilter logic into query expression.
    /// </summary>
    internal class ConventionBasedQueryExpressionProcessor : IQueryExpressionProcessor
    {
        private Type targetType;

        private ConventionBasedQueryExpressionProcessor(Type targetType)
        {
            this.targetType = targetType;
        }

        // Inner should be null unless user add one as inner most
        public IQueryExpressionProcessor Inner { get; set; }

        /// <inheritdoc/>
        public static void ApplyTo(
            IServiceCollection services,
            Type targetType)
        {
            Ensure.NotNull(services, "services");
            Ensure.NotNull(targetType, "targetType");
            services.AddService<IQueryExpressionProcessor>(
                (sp, next) => new ConventionBasedQueryExpressionProcessor(targetType)
            {
                Inner = next,
            });
        }

        /// <inheritdoc/>
        public Expression Process(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");

            if (Inner != null)
            {
                var innerFilteredExpression = Inner.Process(context);
                if (innerFilteredExpression != null && innerFilteredExpression != context.VisitedNode)
                {
                    return innerFilteredExpression;
                }
            }

            var dataSourceStubReference = context.ModelReference as DataSourceStubModelReference;
            if (dataSourceStubReference != null)
            {
                var entitySet = dataSourceStubReference.Element as IEdmEntitySet;
                if (entitySet == null)
                {
                    return null;
                }

                var collectionType = entitySet.Type as IEdmCollectionType;
                if (collectionType == null)
                {
                    return null;
                }

                var entityType = collectionType.ElementType.Definition as IEdmEntityType;
                if (entityType == null)
                {
                    return null;
                }

                return AppendOnFilterExpression(context, entityType.Name);
            }

            var propertyModelReference = context.ModelReference as PropertyModelReference;
            if (propertyModelReference != null && propertyModelReference.Property != null)
            {
                // Could be a single navigation property or a collection navigation property
                var propType = propertyModelReference.Property.Type;
                var collectionTypeReference = propType as IEdmCollectionTypeReference;
                if (collectionTypeReference != null)
                {
                    var collectionType = collectionTypeReference.Definition as IEdmCollectionType;
                    propType = collectionType.ElementType;
                }

                var entityType = propType.Definition as IEdmEntityType;
                if (entityType == null)
                {
                    return null;
                }

                // In case of type inheritance, get the base type
                while (entityType.BaseType != null)
                {
                    entityType = (IEdmEntityType)entityType.BaseType;
                }

                return AppendOnFilterExpression(context, entityType.Name);
            }

            return null;
        }

        private Expression AppendOnFilterExpression(QueryExpressionContext context, string entityTypeName)
        {
            var methodName = ConventionBasedChangeSetConstants.FilterMethodEntitySetFilter + entityTypeName;
            var method = this.targetType.GetQualifiedMethod(methodName);
            if (method == null || !method.IsFamily)
            {
                return null;
            }

            var parameter = method.GetParameters().SingleOrDefault();
            if (parameter == null ||
                parameter.ParameterType != method.ReturnType)
            {
                return null;
            }

            object apiBase = null;
            if (!method.IsStatic)
            {
                apiBase = context.QueryContext.GetApiService<ApiBase>();
                if (apiBase == null ||
                    !this.targetType.IsInstanceOfType(apiBase))
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

            if (returnType == null)
            {
                // This means append for properties model reference
                var collectionType = context.VisitedNode.Type.FindGenericType(typeof(ICollection<>));
                if (collectionType == null)
                {
                    return null;
                }

                elementType = collectionType.GetGenericArguments()[0];
                returnType = typeof(IQueryable<>).MakeGenericType(elementType);

                enumerableQueryParameter = Expression.Call(
                    ExpressionHelperMethods.QueryableAsQueryableGeneric.MakeGenericMethod(elementType),
                    context.VisitedNode);
            }
            else
            {
                elementType = returnType.GetGenericArguments()[0];
            }

            var queryType = typeof(EnumerableQuery<>).MakeGenericType(elementType);
            var query = Activator.CreateInstance(queryType, enumerableQueryParameter);
            var result = method.Invoke(apiBase, new object[] { query }) as IQueryable;
            if (result == null)
            {
                return null;
            }

            if (method.ReturnType == returnType)
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
