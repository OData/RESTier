// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A factory to create <see cref="IServiceScope"/> for <see cref="ApiContext"/>.
    /// </summary>
    [CLSCompliant(false)]
    public interface IApiScopeFactory
    {
        /// <summary>
        /// Creates an <see cref="IServiceScope"/>.
        /// </summary>
        /// <returns>An <see cref="IServiceScope"/>.</returns>
        IServiceScope CreateApiScope();
    }
}
