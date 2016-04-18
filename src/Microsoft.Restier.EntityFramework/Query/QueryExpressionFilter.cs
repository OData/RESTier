// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.EntityFramework.Query
{
    /// <summary>
    /// A query expression filter to handle EF related logic.
    /// </summary>
    internal class QueryExpressionFilter : IQueryExpressionFilter
    {
        // It will be ConventionBasedEntitySetFilter
        public IQueryExpressionFilter Inner { get; set; }

        /// <inheritdoc/>
        public Expression Filter(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");

            if (Inner != null)
            {
                var innerFilteredExpression = Inner.Filter(context);
                if (innerFilteredExpression != null && innerFilteredExpression != context.VisitedNode)
                {
                    return innerFilteredExpression;
                }
            }

            // TODO GitHubIssue#330: EF QueryExecutor will throw exception if check whether collections is null added.
            // Error message likes "Cannot compare elements of type 'ICollection`1[[EntityType]]'.
            // Only primitive types, enumeration types and entity types are supported."
            if (context.VisitedNode.NodeType == ExpressionType.NotEqual)
            {
                var binaryExp = (BinaryExpression)context.VisitedNode;
                var left = binaryExp.Left as MemberExpression;
                var right = binaryExp.Right as ConstantExpression;
                bool leftCheck = left != null && left.Type.IsGenericType
                    && left.Type.GetGenericTypeDefinition() == typeof(ICollection<>);
                bool rightCheck = right != null && right.Value == null;
                if (leftCheck && rightCheck)
                {
                    return Expression.Constant(true);
                }
            }

            return context.VisitedNode;
        }
    }
}
