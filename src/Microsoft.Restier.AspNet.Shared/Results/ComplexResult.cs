// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.OData.Edm;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore
#else
namespace Microsoft.Restier.AspNet
#endif
{
    /// <summary>
    /// Represents a single complex value being returned from an action.
    /// </summary>
    internal class ComplexResult : BaseSingleResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns a complex value.</param>
        /// <param name="edmType">The EDM type reference of the complex value.</param>
        public ComplexResult(IQueryable query, IEdmTypeReference edmType)
            : base(query, edmType)
        {
        }
    }
}
