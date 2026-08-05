// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNetCore
{
    /// <summary>
    /// Attributes for a property.
    /// </summary>
    [Flags]
    internal enum PropertyAttributes
    {
        /// <summary>
        /// No flag is set for the property.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Gets or sets a value indicating whether the property should be ignored during update.
        /// </summary>
        IgnoreForUpdate = 0x1,

        /// <summary>
        /// Gets or sets a value indicating whether the property should be ignored during creation.
        /// </summary>
        IgnoreForCreation = 0x2,

        /// <summary>
        /// Gets or sets a value indicating whether there is permission to read the property.
        /// </summary>
        NoReadPermission = 0x4,

        /// <summary>
        /// Gets or sets a value indicating whether there is permission to write the property.
        /// </summary>
        NoWritePermission = 0x8,
    }
}
