// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

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
