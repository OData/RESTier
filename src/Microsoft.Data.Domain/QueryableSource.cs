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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Data.Domain
{
    internal abstract class QueryableSource : IOrderedQueryable, IQueryProvider
    {
        public QueryableSource(Expression expression)
        {
            this.Expression = expression;
        }

        public abstract Type ElementType { get; }

        public Expression Expression { get; private set; }

        public override string ToString()
        {
            return this.Expression.ToString();
        }

        IQueryProvider IQueryable.Provider
        {
	        get
            {
                return this;
            }
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(
            Expression expression)
        {
            Ensure.NotNull(expression);
            if (!typeof(IQueryable<TElement>).IsAssignableFrom(expression.Type))
            {
                // TODO: error message
                throw new ArgumentException();
            }
            return new QueryableSource<TElement>(expression);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            var type = expression.Type.FindGenericType(typeof(IQueryable<>));
            if (type == null)
            {
                // TODO: error message
                throw new ArgumentException();
            }
            type = typeof(QueryableSource<>).MakeGenericType(
                type.GetGenericArguments()[0]);
            return Activator.CreateInstance(type,
                BindingFlags.Public | BindingFlags.Instance,
                null, new object[] { expression }, null) as IQueryable;
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            throw new NotSupportedException();
        }

        object IQueryProvider.Execute(Expression expression)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
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
            throw new NotSupportedException();
        }
    }
}
