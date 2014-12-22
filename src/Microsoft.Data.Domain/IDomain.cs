// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents a domain.
    /// </summary>
    /// <remarks>
    /// A domain composes a domain configuration with semantics
    /// around the creation and disposal of a domain context.
    /// </remarks>
    public interface IDomain
    {
        /// <summary>
        /// Gets the context for this domain.
        /// </summary>
        DomainContext Context { get; }
    }
}
