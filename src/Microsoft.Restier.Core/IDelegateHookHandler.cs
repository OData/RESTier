// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Representing an interface that supports delegation chain.
    /// </summary>
    /// <typeparam name="T">The <see cref="IHookHandler"/> type.</typeparam>
    public interface IDelegateHookHandler<T> where T : IHookHandler
    {
        /// <summary>
        /// Gets or sets the inner handler
        /// </summary>
        /// <remarks>
        /// This property would be auto-set during hook registration.
        /// </remarks>
        T InnerHandler { get; set; }
    }
}
