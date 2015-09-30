// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents an API.
    /// </summary>
    /// <remarks>
    /// An API composes an API configuration with semantics
    /// around the creation and disposal of an API context.
    /// </remarks>
    public interface IApi : System.IDisposable
    {
        /// <summary>
        /// Gets the context for this API.
        /// </summary>
        ApiContext Context { get; }
    }
}
