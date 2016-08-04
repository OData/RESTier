// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Restier.Core
{
    internal abstract class QueryableSource : IOrderedQueryable, IQueryProvider
    {
        public QueryableSource(Expression expression)
        {
            this.Expression = expression;
        }

        public abstract Type ElementType { get; }

        public Expression Expression { get; private set; }

        IQueryProvider IQueryable.Provider
        {
            get
            {
                return this;
            }
        }

        public override string ToString()
        {
            return this.Expression.ToString();
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(
            Expression expression)
        {
            Ensure.NotNull(expression, "expression");
            if (!typeof(IQueryable<TElement>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentException(Resources.ExpressionMustBeQueryable);
            }

            return new QueryableSource<TElement>(expression);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            Ensure.NotNull(expression, "expression");
            var type = expression.Type.FindGenericType(typeof(IQueryable<>));
            if (type == null)
            {
                throw new ArgumentException(Resources.ExpressionMustBeQueryable);
            }

            type = typeof(QueryableSource<>).MakeGenericType(
                type.GetGenericArguments()[0]);
            return Activator.CreateInstance(
                type,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new object[] { expression },
                null) as IQueryable;
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            throw new NotSupportedException(Resources.CallQueryableSourceMethodNotSupported);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            throw new NotSupportedException(Resources.CallQueryableSourceMethodNotSupported);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException(Resources.CallQueryableSourceMethodNotSupported);
        }
    }

    internal class QueryableSource<T> : QueryableSource, IOrderedQueryable<T>
    {
        public QueryableSource(Expression expression)
            : base(expression)
        {
        }

        public override Type ElementType
        {
            get
            {
                return typeof(T);
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotSupportedException(Resources.CallQueryableSourceMethodNotSupported);
        }
    }
}
