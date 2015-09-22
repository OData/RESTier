// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.WebApi.Results
{
    /// <summary>
    /// The result of an OData query.
    /// </summary>
    public abstract class ODataQueryResult
    {
        private IEdmTypeReference edmType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataQueryResult" /> class.
        /// </summary>
        /// <param name="edmType">The EDM type reference of the OData result.</param>
        protected ODataQueryResult(IEdmTypeReference edmType)
        {
            Ensure.NotNull(edmType, "edmType");

            this.edmType = edmType;
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
    }
}
