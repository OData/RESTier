// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Linq;
using Microsoft.OData.Edm;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore
#else
namespace Microsoft.Restier.AspNet
#endif
{
    /// <summary>
    /// Represents a single object being returned from an action.
    /// </summary>
    internal abstract class BaseSingleResult : BaseResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSingleResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns an object.</param>
        /// <param name="edmType">The EDM type reference of the object.</param>
        protected BaseSingleResult(IQueryable query, IEdmTypeReference edmType)
            : base(edmType)
        {
            Ensure.NotNull(query, nameof(query));

            Result = query.SingleOrDefault();
            Type = query.ElementType;
        }

        /// <summary>
        /// Gets the result object.
        /// </summary>
        public object Result { get; private set; }

        /// <summary>
        /// Gets the type of the result object.
        /// </summary>
        public Type Type { get; private set; }
    }
}
