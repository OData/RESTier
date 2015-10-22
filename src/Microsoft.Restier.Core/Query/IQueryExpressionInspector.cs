﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a hook point that inspects a query expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query expression inspection evaluates an expression to determine
    /// if it is valid according to API logic such as authorization rules.
    /// </para>
    /// <para>
    /// Inspection is the first step that occurs when processing a query
    /// expression after its children have been visited, so it occurs during
    /// upward traversal of the query expression. This ensures that inspection
    /// has a chance to take place before the node is altered in any way (with
    /// the exception of normalization of expressions identifying API data).
    /// </para>
    /// <para>
    /// This is a multi-cast hook point whose instances
    /// are used in the reverse order of registration.
    /// </para>
    /// </remarks>
    public interface IQueryExpressionInspector : IHookHandler
    {
        /// <summary>
        /// Inspects an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// <c>true</c> if the inspection passed; otherwise, <c>false</c>.
        /// </returns>
        bool Inspect(QueryExpressionContext context);
    }
}
