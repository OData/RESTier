// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents the Restier operations available to an <see cref="IEdmOperationImport"/>.
    /// </summary>
    public enum RestierOperationMethod
    {

        /// <summary>
        /// Represents the OperationImport being executed.
        /// </summary>
        Execute = 1,

    }
}