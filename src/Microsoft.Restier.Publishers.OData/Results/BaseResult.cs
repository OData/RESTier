// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Publishers.OData
{
    /// <summary>
    /// Represents the result of an OData query.
    /// </summary>
    internal abstract class BaseResult
    {
        private readonly IEdmTypeReference edmType;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseResult" /> class.
        /// </summary>
        /// <param name="edmType">The EDM type reference of the OData result.</param>
        /// <param name="api">The api instance where the action is executed.</param>
        protected BaseResult(IEdmTypeReference edmType, ApiBase api)
        {
            Ensure.NotNull(edmType, "edmType");
            Ensure.NotNull(api, "api");

            this.edmType = edmType;
            this.Api = api;
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
        public ApiBase Api { get; private set; }
    }
}
