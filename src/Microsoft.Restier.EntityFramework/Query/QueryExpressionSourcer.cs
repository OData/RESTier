// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if !EF7
using System.Data.Entity;
#endif
using System.Linq;
using System.Linq.Expressions;
#if EF7
using Microsoft.Data.Entity;
#endif
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.EntityFramework.Query
{
    /// <summary>
    /// Represents a query expression sourcer that uses a DbContext.
    /// </summary>
    internal class QueryExpressionSourcer : IQueryExpressionSourcer
    {
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
            Ensure.NotNull(context, "context");

            if (context.ModelReference.EntitySet == null)
            {
                // EF provider can only source *EntitySet*.
                return null;
            }

            var dbContext = context.QueryContext
                .ApiContext.GetProperty<DbContext>(DbApiConstants.DbContextKey);
            var dbSetProperty = dbContext.GetType().GetProperties()
                .FirstOrDefault(prop => prop.Name == context.ModelReference.EntitySet.Name);
            if (dbSetProperty == null)
            {
                // EF provider can only source EntitySet from *DbSet property*.
                return null;
            }

            if (!embedded)
            {
                // TODO GitHubIssue#37 : Add API entity manager for tracking entities
                // the underlying DbContext shouldn't track the entities
                var dbSet = dbSetProperty.GetValue(dbContext);

                ////dbSet = dbSet.GetType().GetMethod("AsNoTracking").Invoke(dbSet, null);
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
