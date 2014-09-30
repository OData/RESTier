// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.Domain.Query;
using Microsoft.OData.Edm;

namespace Microsoft.Data.Domain.Conventions
{
    /// <summary>
    /// A conventional query expression filter on entity set.
    /// </summary>
    public class ConventionalEntitySetFilter : IQueryExpressionFilter
    {
        private Type _targetType;

        private ConventionalEntitySetFilter(Type targetType)
        {
            this._targetType = targetType;
        }

        /// <inheritdoc/>
        public static void ApplyTo(
            DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");
            configuration.AddHookPoint(typeof(IQueryExpressionFilter),
                new ConventionalEntitySetFilter(targetType));
        }

        /// <inheritdoc/>
        public Expression Filter(QueryExpressionContext context)
        {
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
            var method = this._targetType.GetMethod(
                "OnFilter" + entitySet.Name,
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase |
                BindingFlags.DeclaredOnly);
            if (method != null && method.IsPrivate &&
                method.ReturnType == returnType)
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.QueryContext.DomainContext.GetProperty(
                        this._targetType.AssemblyQualifiedName);
                    if (target == null ||
                        !this._targetType.IsAssignableFrom(target.GetType()))
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
                    var query = Activator.CreateInstance(
                        queryType, context.VisitedNode);
                    var result = method.Invoke(target,
                        new object[] { query }) as IQueryable;
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
