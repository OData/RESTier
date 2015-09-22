// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.WebApi.Results
{
    /// <summary>
    /// The result of an OData query.
    /// </summary>
    public abstract class ODataQueryResult
    {
        private readonly IEdmTypeReference edmType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataQueryResult" /> class.
        /// </summary>
        /// <param name="edmType">The EDM type reference of the OData result.</param>
        /// <param name="context">The context where the action is executed.</param>
        protected ODataQueryResult(IEdmTypeReference edmType, DomainContext context)
        {
            Ensure.NotNull(edmType, "edmType");
            Ensure.NotNull(context, "context");

            this.edmType = edmType;
            this.Context = context;
        }

        /// <summary>
        /// Gets the EDM type reference of the OData result.
        /// </summary>
        public IEdmTypeReference EdmType
        {
            get
            {
                return this.edmType;
            }
        }

        /// <summary>
        /// Gets the context where the action is executed.
        /// </summary>
        public DomainContext Context { get; private set; }
    }
}
