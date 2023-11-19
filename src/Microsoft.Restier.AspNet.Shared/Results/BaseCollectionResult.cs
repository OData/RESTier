// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.OData.Edm;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore
#else
namespace Microsoft.Restier.AspNet
#endif
{
    /// <summary>
    /// Represents a collection of objects being returned from an action.
    /// </summary>
    internal abstract class BaseCollectionResult : BaseResult
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCollectionResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns a collection of objects.</param>
        /// <param name="edmType">The EDM type reference of the objects.</param>
        protected BaseCollectionResult(IQueryable query, IEdmTypeReference edmType)
            : base(edmType)
        {
            Ensure.NotNull(query, nameof(query));

            Query = query;
            Type = query.GetType();
        }

        /// <summary>
        /// Gets the query that returns a collection of objects.
        /// </summary>
        public IQueryable Query { get; private set; }

        /// <summary>
        /// Gets the type of the query.
        /// </summary>
        public Type Type { get; private set; }
    }
}
