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
    /// <summary>
    /// Represents a Queryable Source.
    /// </summary>
    internal abstract class QueryableSource : IOrderedQueryable, IQueryProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryableSource"/> class.
        /// </summary>
        /// <param name="expression">The expression that represents the Queryable Source.</param>
        public QueryableSource(Expression expression) => Expression = expression;

        /// <summary>
        /// Gets the type of the elements that are queryable.
        /// </summary>
        public abstract Type ElementType { get; }

        /// <summary>
        /// Gets the query expression.
        /// </summary>
        public Expression Expression { get; private set; }

        /// <inheritdoc />
        IQueryProvider IQueryable.Provider => this;

        /// <inheritdoc />
        public override string ToString() => Expression.ToString();

        /// <inheritdoc />
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(
            Expression expression)
        {
            Ensure.NotNull(expression, nameof(expression));
            if (!typeof(IQueryable<TElement>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentException(Resources.ExpressionMustBeQueryable);
            }

            return new QueryableSource<TElement>(expression);
        }

        /// <inheritdoc />
        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            Ensure.NotNull(expression, nameof(expression));
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

        /// <inheritdoc />
        TResult IQueryProvider.Execute<TResult>(Expression expression) => throw new NotSupportedException(Resources.CallQueryableSourceMethodNotSupported);

        /// <inheritdoc />
        object IQueryProvider.Execute(Expression expression) => throw new NotSupportedException(Resources.CallQueryableSourceMethodNotSupported);

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException(Resources.CallQueryableSourceMethodNotSupported);
    }

    /// <summary>
    /// Represents a typed <see cref="QueryableSource"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    internal class QueryableSource<T> : QueryableSource, IOrderedQueryable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryableSource{T}"/> class.
        /// </summary>
        /// <param name="expression">The query expression.</param>
        public QueryableSource(Expression expression)
            : base(expression)
        {
        }

        /// <inheritdoc />
        public override Type ElementType => typeof(T);

        /// <inheritdoc />
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotSupportedException(Resources.CallQueryableSourceMethodNotSupported);
    }
}
