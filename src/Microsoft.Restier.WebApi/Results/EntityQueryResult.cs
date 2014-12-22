// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.WebApi.Results
{
    /// <summary>
    /// The result of an OData query.
    /// </summary>
    public abstract class EntityQueryResult
    {
        private IEdmTypeReference edmType;

        protected EntityQueryResult(IEdmTypeReference edmType)
        {
            Ensure.NotNull(edmType, "edmType");

            this.edmType = edmType;
        }

        public IEdmTypeReference EdmType
        {
            get
            {
                return this.edmType;
            }
        }
    }
}
