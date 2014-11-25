// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq.Expressions;

namespace Microsoft.Data.Domain.Query
{
    /// <summary>
    /// Represents a hook point that normalizes a query expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query expression normalization converts an expression that identifies
    /// domain data in a non-normalized manner to its equivalent normalized
    /// representation, which is a method call to one of the methods defined
    /// on the <see cref="DomainData"/> class.
    /// </para>
    /// <para>
    /// Normalization is the first step that occurs when processing a query
    /// expression and ensures that arbitrary expressions identifying domain
    /// data, such as "myData.Customers", are converted to a representation
    /// over which query expression visitors can reason in a general manner.
    /// </para>
    /// <para>
    /// This is a multi-cast hook point whose instances
    /// are used in the reverse order of registration.
    /// </para>
    /// </remarks>
    public interface IQueryExpressionNormalizer
    {
        /// <summary>
        /// Normalizes an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// A normalized expression of the same type as the visited node, or
        /// if normalization did not apply, the visited node or <c>null</c>.
        /// </returns>
        Expression Normalize(QueryExpressionContext context);
    }
}
