// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Represents a policy applicable to an API that can be
    /// activated during an API flow then later deactivated.
    /// </summary>
    public interface IApiPolicy
    {
        /// <summary>
        /// Activates this API policy.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        void Activate(InvocationContext context);

        /// <summary>
        /// Deactivates this API policy.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        void Deactivate(InvocationContext context);
    }
}
