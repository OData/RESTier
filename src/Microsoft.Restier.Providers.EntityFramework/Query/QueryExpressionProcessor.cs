// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Providers.EntityFramework
{
    /// <summary>
    /// A query expression filter to handle EF related logic.
    /// </summary>
    internal class QueryExpressionProcessor : IQueryExpressionProcessor
    {
        // It will be ConventionBasedEntitySetProcessor
        public IQueryExpressionProcessor Inner { get; set; }

        /// <inheritdoc/>
        public Expression Process(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");

            if (Inner != null)
            {
                var innerFilteredExpression = Inner.Process(context);
                if (innerFilteredExpression != null && innerFilteredExpression != context.VisitedNode)
                {
                    return innerFilteredExpression;
                }
            }

            // TODO GitHubIssue#330: EF QueryExecutor will throw exception if check whether collections is null added.
            // Exception message likes "Cannot compare elements of type 'ICollection`1[[EntityType]]'.
            // Only primitive types, enumeration types and entity types are supported."
            // EF does not support complex != null neither, and it requires complex not null.
            // EF model builder set complex type not null by default, but Web Api OData does not.
            if (context.VisitedNode.NodeType == ExpressionType.NotEqual)
            {
                var binaryExp = (BinaryExpression)context.VisitedNode;
                var left = binaryExp.Left as MemberExpression;
                var right = binaryExp.Right as ConstantExpression;

                bool rightCheck = right != null && right.Value == null;

                // Check right first which is simple
                if (!rightCheck)
                {
                    return context.VisitedNode;
                }

                bool leftCheck = false;
                if (left != null)
                {
                    // If it is a collection, then replace coll != null with true
                    leftCheck = left.Type.IsGenericType
                        && left.Type.GetGenericTypeDefinition() == typeof(ICollection<>);

                    // If it is a complex, replace complex!=null with true
                    if (!leftCheck)
                    {
                        var modelRef = context.GetModelReferenceForNode(left);
                        if (modelRef != null && modelRef.Type != null)
                        {
                            leftCheck = modelRef.Type.TypeKind.Equals(EdmTypeKind.Complex);
                        }
                    }
                }

                if (leftCheck)
                {
                    return Expression.Constant(true);
                }
            }

            return context.VisitedNode;
        }
    }
}
