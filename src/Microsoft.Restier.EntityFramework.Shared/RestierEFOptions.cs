// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EFCore
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
{
    /// <summary>
    /// Per-API options for the RESTier EF provider. Registered as a
    /// singleton in the route's service container by
    /// <c>AddEF6ProviderServices</c> / <c>AddEFCoreProviderServices</c>.
    /// </summary>
    public sealed class RestierEFOptions
    {
        /// <summary>
        /// Controls how the query pipeline wraps the underlying
        /// <c>DbSet</c>. Defaults to <see cref="RestierEFTrackingBehavior.Default"/>.
        /// </summary>
        public RestierEFTrackingBehavior TrackingBehavior { get; set; }
            = RestierEFTrackingBehavior.Default;
    }
}
