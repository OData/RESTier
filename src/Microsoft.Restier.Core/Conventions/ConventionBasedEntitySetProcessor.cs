// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Core.Conventions
{
    /// <summary>
    /// A convention-based query expression filter on entity set.
    /// </summary>
    internal class ConventionBasedEntitySetProcessor : IQueryExpressionProcessor
    {
        private Type targetType;

        private ConventionBasedEntitySetProcessor(Type targetType)
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
            services.AddService<IQueryExpressionProcessor>((sp, next) => new ConventionBasedEntitySetProcessor(targetType)
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
                var entityType = dataSourceStubReference.Type as EdmEntityType;
                if (entityType == null)
                {
                    return null;
                }
                
                return AppendOnFilter(context, entityType.Name);
            }

            var propertyModelReference = context.ModelReference as PropertyModelReference;
            if (propertyModelReference != null)
            {
                // Could be a single navigation property or a collection navigation property
                var propType = propertyModelReference.Property.Type;
                var collectoinTypeRef = propType as IEdmCollectionTypeReference;
                if (collectoinTypeRef != null)
                {
                    var collectionType = collectoinTypeRef.Definition as IEdmCollectionType;
                    propType = collectionType.ElementType;
                }

                if (propType.TypeKind() != EdmTypeKind.Entity)
                {
                    return null;
                }

                var entityType = propType.Definition as EdmEntityType;
                if (entityType == null)
                {
                    return null;
                }

                // In case of type inheritance, get the base type
                var currentType = entityType;
                while (currentType.BaseType != null)
                {
                    currentType = (EdmEntityType) currentType.BaseType;
                }

                return AppendOnFilter(context, currentType.Name);
            }

            return null;
        }

        private Expression AppendOnFilter(QueryExpressionContext context, string entityTypeName)
        {
            var methodName = ConventionBasedChangeSetConstants.FilterMethodEntitySetFilter + entityTypeName;
            var method = this.targetType.GetQualifiedMethod(methodName);
            if (method == null || ! method.IsFamily)
            {
                return null;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 1 ||
                parameters[0].ParameterType != method.ReturnType)
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

            var returnType = context.VisitedNode.Type.FindGenericType(typeof(IQueryable<>));
            var enumerableQueryPara = (object)context.VisitedNode;
            Type elementType = null;

            if (returnType == null)
            {
                // This means append for properties model reference
                var collType = context.VisitedNode.Type.FindGenericType(typeof (ICollection<>));
                if (collType == null)
                {
                    return null;
                }

                elementType = collType.GetGenericArguments()[0];
                returnType = typeof(IQueryable<>).MakeGenericType(elementType);

                enumerableQueryPara = Expression.Call(
                    ExpressionHelperMethods.QueryableAsQueryableGeneric.MakeGenericMethod(elementType),
                    context.VisitedNode);
            }
            else
            {
                elementType = returnType.GetGenericArguments()[0];
            }
            
            var queryType = typeof(EnumerableQuery<>)
                    .MakeGenericType(elementType);
            var query = Activator.CreateInstance(queryType, enumerableQueryPara);
            var result = method.Invoke(apiBase, new object[] { query }) as IQueryable;
            if (method.ReturnType == returnType)
            {
                if (result != null && result != query)
                {
                    return result.Expression;
                }
            }
            else
            {
                // This means calling onFilter against derived type and based type is returned
                // Need to convert back to derived type
                if (result != null)
                {
                    result = ExpressionHelpers.OfType(result, elementType);
                    return result.Expression;
                }
            }

            return null;
        }
    }
}
