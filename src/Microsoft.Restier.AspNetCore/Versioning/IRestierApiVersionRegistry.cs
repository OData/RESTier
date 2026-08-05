// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Read-only access to the set of versioned Restier routes registered via the
    /// Microsoft.Restier.AspNetCore.Versioning package.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Materialization invariant</b>: descriptors are populated when
    /// <see cref="Microsoft.Extensions.Options.IOptions{ODataOptions}"/>'s <c>Value</c> first
    /// materializes. Any component that reads this registry directly MUST first resolve
    /// <c>IOptions&lt;ODataOptions&gt;.Value</c> from the same scope to guarantee the
    /// configurator pipeline has run. <c>IOptions&lt;T&gt;.Value</c> caches.
    /// </para>
    /// </remarks>
    public interface IRestierApiVersionRegistry
    {

        /// <summary>
        /// All registered version descriptors, in registration order.
        /// </summary>
        IReadOnlyList<RestierApiVersionDescriptor> Descriptors { get; }

        /// <summary>
        /// Finds the descriptor whose composed <see cref="RestierApiVersionDescriptor.RoutePrefix"/>
        /// equals <paramref name="routePrefix"/> (ordinal). Returns null if not found.
        /// </summary>
        RestierApiVersionDescriptor FindByPrefix(string routePrefix);

        /// <summary>
        /// Finds the descriptor whose <see cref="RestierApiVersionDescriptor.GroupName"/>
        /// equals <paramref name="groupName"/> (ordinal, case-insensitive).
        /// Returns null if not found.
        /// </summary>
        RestierApiVersionDescriptor FindByGroupName(string groupName);

        /// <summary>
        /// Returns descriptors that share the supplied logical API group key —
        /// the <c>basePrefix</c> passed to <c>AddVersion</c>. Used by header reporting
        /// so <c>api-supported-versions</c> / <c>api-deprecated-versions</c> reflect only
        /// the API the request belongs to, not unrelated APIs at other prefixes.
        /// </summary>
        IReadOnlyList<RestierApiVersionDescriptor> FindByBasePrefix(string basePrefix);

    }

}
