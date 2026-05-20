// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Per-route configuration for a Restier route. Pass an
    /// <c>Action&lt;RestierRouteOptions&gt;</c> to
    /// <c>ODataOptions.AddRestierRoute</c> to customize batching, naming
    /// convention, deep-operation depth, and OData-spec conformance.
    /// </summary>
    public class RestierRouteOptions
    {
        /// <summary>
        /// Deep insert/update settings (max nesting depth).
        /// </summary>
        public DeepOperationSettings DeepOperations { get; } = new();

        /// <summary>
        /// Opt-in OData-spec conformance toggles.
        /// </summary>
        public RestierConformanceOptions Conformance { get; } = new();

        /// <summary>
        /// Per-route query validation limits (<c>$top</c>, <c>$expand</c>
        /// depth, etc.). Any property left <c>null</c> defaults from the
        /// global <c>ODataOptions</c> or the OData framework default. Values
        /// set here take precedence over any caller-supplied
        /// <c>ODataValidationSettings</c> DI registration; conflicts with
        /// <c>ODataOptions.SetMaxTop</c> emit a Trace warning at route-add
        /// time.
        /// </summary>
        public RestierValidationOptions Validation { get; } = new();

        /// <summary>
        /// When <c>true</c> (default), the Restier batch handler is registered
        /// for the route.
        /// </summary>
        public bool UseRestierBatching { get; set; } = true;

        /// <summary>
        /// Naming convention applied to EDM property names and the resulting
        /// JSON. Defaults to <see cref="RestierNamingConvention.PascalCase"/>.
        /// </summary>
        public RestierNamingConvention NamingConvention { get; set; }
            = RestierNamingConvention.PascalCase;
    }
}
