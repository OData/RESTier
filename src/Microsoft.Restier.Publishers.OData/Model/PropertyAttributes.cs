// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Publishers.OData
{
    internal class PropertyAttributes
    {
        /// <summary>
        /// Gets or sets a value indicating whether the property should be ignored during update
        /// </summary>
        public bool IgnoreForUpdate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the property should be ignored during creation
        /// </summary>
        public bool IgnoreForCreation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether there is permission to read the property
        /// </summary>
        public bool NoReadPermission { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether there is permission to write the property
        /// </summary>
        public bool NoWritePermission { get; set; }
    }
}
