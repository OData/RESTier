// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EFCore
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
{
    /// <summary>
    /// Controls how RESTier wraps the underlying <c>DbSet</c> in the EF query
    /// pipeline. Configured via <see cref="RestierEFOptions"/>.
    /// </summary>
    public enum RestierEFTrackingBehavior
    {
        /// <summary>
        /// Use the provider's recommended default. On EF Core this maps to
        /// <c>AsNoTrackingWithIdentityResolution</c>. On EF6 it maps to
        /// <c>AsNoTracking</c>, except for requests whose
        /// <c>$expand</c> tree contains a cycle — those fall back to tracked
        /// queries so identity is preserved across the cycle.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Force <c>AsNoTracking</c> for every query. Fastest, but
        /// identity is not preserved within a single query result. On
        /// recursive expands under EF6 this can produce duplicate
        /// materialized entities for the same key.
        /// </summary>
        NoTracking = 1,

        /// <summary>
        /// Force <c>AsNoTrackingWithIdentityResolution</c>. EF Core only —
        /// on EF6 this falls back to plain <c>AsNoTracking</c> because the
        /// underlying API does not exist.
        /// </summary>
        NoTrackingWithIdentityResolution = 2,

        /// <summary>
        /// Restore pre-#726 behavior — leave the <c>DbSet</c> tracked. Use
        /// when hook code mutates returned entities and expects those
        /// mutations to be picked up by <c>SaveChanges</c>.
        /// </summary>
        TrackAll = 3,
    }
}
