// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Core.Conventions
{
    /// <summary>
    /// A conventional query expression filter on entity set.
    /// </summary>
    internal class ConventionalEntitySetFilter : IQueryExpressionFilter
    {
        private Type targetType;

        private ConventionalEntitySetFilter(Type targetType)
        {
            this.targetType = targetType;
        }

        /// <inheritdoc/>
        public static void ApplyTo(
            DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");
            configuration.AddHookHandler<IQueryExpressionFilter>(new ConventionalEntitySetFilter(targetType));
        }

        /// <inheritdoc/>
        public Expression Filter(QueryExpressionContext context)
        {
            Ensure.NotNull(context);
            if (context.ModelReference == null)
            {
                return null;
            }

            var domainDataReference = context.ModelReference as DomainDataReference;
            if (domainDataReference == null)
            {
                return null;
            }

            var entitySet = domainDataReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return null;
            }

            var returnType = context.VisitedNode.Type
                .FindGenericType(typeof(IQueryable<>));
            var elementType = returnType.GetGenericArguments()[0];
            var method = this.targetType.GetQualifiedMethod("OnFilter" + entitySet.Name);
            if (method != null && method.IsPrivate &&
                method.ReturnType == returnType)
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.QueryContext.DomainContext.GetProperty(
                        typeof(Domain).AssemblyQualifiedName);
                    if (target == null ||
                        !this.targetType.IsAssignableFrom(target.GetType()))
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
