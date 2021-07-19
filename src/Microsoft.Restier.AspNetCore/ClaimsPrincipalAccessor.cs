// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Restier.AspNetCore.Abstractions;
using System.Security.Claims;
using System.Threading;

namespace Microsoft.Restier.AspNetCore
{

#nullable enable annotations

    /// <summary>
    /// Provides an implementation of <see cref="IClaimsPrincipalAccessor" /> based on the current execution context. 
    /// </summary>
    public class ClaimsPrincipalAccessor : IClaimsPrincipalAccessor
    {

        private static readonly AsyncLocal<ClaimsPrincipalHolder> _ClaimsPrincipalCurrent = new();

        /// <inheritdoc/>
        public ClaimsPrincipal? ClaimsPrincipal
        {
            get
            {
                return _ClaimsPrincipalCurrent.Value?.Context;
            }
            set
            {
                var holder = _ClaimsPrincipalCurrent.Value;
                if (holder != null)
                {
                    // Clear current ClaimsPrincipal trapped in the AsyncLocals, as its done.
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the ClaimsPrincipal in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    _ClaimsPrincipalCurrent.Value = new ClaimsPrincipalHolder { Context = value };
                }
            }
        }

        private class ClaimsPrincipalHolder
        {
            public ClaimsPrincipal? Context;
        }

    }

#nullable disable annotations

}