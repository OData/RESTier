// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Per-route query validation limits exposed through the
    /// <see cref="RestierRouteOptions"/> bag. Any property left <c>null</c>
    /// inherits its value from the global <c>ODataOptions</c> (where
    /// applicable) or the underlying OData framework default.
    /// </summary>
    /// <remarks>
    /// Restier owns a single shared <c>RestierController</c> per route, so
    /// there is no per-action layer on which to hang the upstream
    /// <c>ODataValidationSettings</c> object. This bag is the route-level
    /// substitute: values set here win over both the global
    /// <c>ODataOptions</c> ceilings and any caller-supplied
    /// <c>ODataValidationSettings</c> DI registration. Conflicts emit
    /// <see cref="System.Diagnostics.Trace.TraceWarning(string)"/> at
    /// route-add time naming the winning value.
    /// </remarks>
    public class RestierValidationOptions
    {
        /// <summary>
        /// Maximum value the client may supply for <c>$top</c>. When unset,
        /// inherits <c>ODataOptions.QuerySettings.MaxTop</c>.
        /// </summary>
        public int? MaxTop { get; set; }

        /// <summary>
        /// Maximum value the client may supply for <c>$skip</c>. When unset,
        /// the underlying OData framework default applies (no upper bound).
        /// </summary>
        public int? MaxSkip { get; set; }

        /// <summary>
        /// Maximum depth permitted in <c>$expand</c>. When unset, the
        /// underlying OData framework default applies (2).
        /// </summary>
        public int? MaxExpansionDepth { get; set; }

        /// <summary>
        /// Maximum nesting of <c>any</c>/<c>all</c> lambda expressions
        /// inside <c>$filter</c>. When unset, the underlying OData framework
        /// default applies (1).
        /// </summary>
        public int? MaxAnyAllExpressionDepth { get; set; }

        /// <summary>
        /// Maximum number of comma-separated nodes in <c>$orderby</c>. When
        /// unset, the underlying OData framework default applies (5).
        /// </summary>
        public int? MaxOrderByNodeCount { get; set; }

        /// <summary>
        /// Maximum total node count of a parsed <c>$filter</c> expression
        /// tree. When unset, the underlying OData framework default applies
        /// (100).
        /// </summary>
        public int? MaxNodeCount { get; set; }
    }
}
