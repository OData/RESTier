﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Publishers.OData
{
    /// <summary>
    /// Represents a single enum value being returned from an action.
    /// </summary>
    internal class EnumResult : BaseSingleResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns a enum value.</param>
        /// <param name="edmType">The EDM type reference of the enum value.</param>
        public EnumResult(IQueryable query, IEdmTypeReference edmType)
            : base(query, edmType)
        {
        }
    }
}
