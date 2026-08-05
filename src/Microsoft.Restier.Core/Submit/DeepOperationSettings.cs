// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Configuration settings for deep insert and deep update operations.
    /// </summary>
    public class DeepOperationSettings
    {
        /// <summary>
        /// Gets or sets the maximum nesting depth for deep operations.
        /// Default is 5. Set to 0 to disable deep operations entirely.
        /// </summary>
        public int MaxDepth { get; set; } = 5;
    }
}
