// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class UnboundOperationAttribute : OperationAttribute
    {

        /// <summary>
        /// Gets or sets the entity set associated with the operation result.
        /// </summary>
        public string EntitySet { get; set; }
    }
}
