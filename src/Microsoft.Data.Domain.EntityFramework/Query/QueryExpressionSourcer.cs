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

using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Data.Domain.Query;

namespace Microsoft.Data.Domain.EntityFramework.Query
{
    /// <summary>
    /// Represents a query expression sourcer that uses a DbContext.
    /// </summary>
    public class QueryExpressionSourcer : IQueryExpressionSourcer
    {
        private QueryExpressionSourcer()
        {
        }

        /// <summary>
        /// Gets the single instance of this query expression sourcer.
        /// </summary>
        public static readonly QueryExpressionSourcer Instance =
            new QueryExpressionSourcer();

        /// <summary>
        /// Sources an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <param name="embedded">
        /// Indicates if the sourcing is occurring on an embedded node.
        /// </param>
        /// <returns>
        /// A data source expression that represents the visited node.
        /// </returns>
        public Expression Source(QueryExpressionContext context, bool embedded)
        {
            var dbContext = context.QueryContext
                .DomainContext.GetProperty<DbContext>("DbContext");
            var dbSetProperty = dbContext.GetType().GetProperties()
                .Where(prop => prop.Name == context.ModelReference.EntitySet.Name)
                .First();
            if (!embedded)
            {
                // TODO: once there is a real domain entity manager,
                // the underlying DbContext shouldn't track the entities
                var dbSet = dbSetProperty.GetValue(dbContext);
                //dbSet = dbSet.GetType().GetMethod("AsNoTracking").Invoke(dbSet, null);
                return Expression.Constant(dbSet);
            }
            else
            {
                return Expression.MakeMemberAccess(
                    Expression.Constant(dbContext),
                    dbSetProperty);
            }
        }
    }
}
