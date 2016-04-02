// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Core.Conventions
{
    /// <summary>
    /// A convention-based query expression filter on entity set.
    /// </summary>
    internal class ConventionBasedEntitySetFilter : IQueryExpressionFilter
    {
        private Type targetType;

        private ConventionBasedEntitySetFilter(Type targetType)
        {
            this.targetType = targetType;
        }

        /// <inheritdoc/>
        public static void ApplyTo(
            IServiceCollection services,
            Type targetType)
        {
            Ensure.NotNull(services, "services");
            Ensure.NotNull(targetType, "targetType");
            services.CutoffPrevious<IQueryExpressionFilter>(new ConventionBasedEntitySetFilter(targetType));
        }

        /// <inheritdoc/>
        public Expression Filter(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");
            if (context.ModelReference == null)
            {
                return null;
            }

            var dataSourceStubReference = context.ModelReference as DataSourceStubReference;
            if (dataSourceStubReference == null)
            {
                return null;
            }

            var entitySet = dataSourceStubReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return null;
            }

            var returnType = context.VisitedNode.Type
                .FindGenericType(typeof(IQueryable<>));
            var elementType = returnType.GetGenericArguments()[0];
            var methodName = ConventionBasedChangeSetConstants.FilterMethodEntitySetFilter + entitySet.Name;
            var method = this.targetType.GetQualifiedMethod(methodName);
            if (method != null && method.IsFamily &&
                method.ReturnType == returnType)
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.QueryContext.ApiContext.
                        ServiceProvider.GetService(targetType);
                    if (target == null)
                    {
                        return null;
                    }
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 1 &&
                    parameters[0].ParameterType == returnType)
                {
                    var queryType = typeof(EnumerableQuery<>)
                        .MakeGenericType(elementType);
                    var query = Activator.CreateInstance(queryType, context.VisitedNode);
                    var result = method.Invoke(target, new object[] { query }) as IQueryable;
                    if (result != null && result != query)
                    {
                        return result.Expression;
                    }
                }
            }

            return null;
        }
    }
}
