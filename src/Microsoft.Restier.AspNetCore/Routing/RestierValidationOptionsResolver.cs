// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Resolves a route's final <see cref="ODataValidationSettings"/> from
/// the <see cref="RestierValidationOptions"/> bag and the global
/// <see cref="ODataOptions"/>. The bag is the only public configuration
/// channel; <see cref="ODataValidationSettings"/> is never read from DI.
/// A bag <c>MaxTop</c> that disagrees with the global
/// <c>SetMaxTop</c> wins and emits a <see cref="Trace.TraceWarning(string)"/>.
/// </summary>
internal static class RestierValidationOptionsResolver
{
    private const string WarningPrefix = "Restier: ";

    /// <summary>
    /// Builds the route's <see cref="ODataValidationSettings"/> from the
    /// bag and the global <see cref="ODataOptions"/>, emitting a
    /// <see cref="Trace.TraceWarning(string)"/> when <c>MaxTop</c>
    /// disagrees between the two channels. Call this once at route-add
    /// time for its warning side-effect; use <see cref="Build"/> at
    /// request time to avoid duplicate warnings.
    /// </summary>
    public static ODataValidationSettings Resolve(
        RestierValidationOptions bag,
        ODataOptions globalOptions,
        string routePrefix)
    {
        // ODataOptions.QueryConfigurations.MaxTop uses 0 as the "unset" sentinel; treat it as null.
        var rawGlobal = globalOptions?.QueryConfigurations?.MaxTop;
        var globalMaxTop = rawGlobal.HasValue && rawGlobal.Value > 0 ? rawGlobal : null;
        if (bag.MaxTop.HasValue && globalMaxTop.HasValue && globalMaxTop != bag.MaxTop)
        {
            Trace.TraceWarning(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}Route '{1}': RestierValidationOptions.MaxTop = {2} overrides ODataOptions.SetMaxTop value {3}.",
                    WarningPrefix,
                    routePrefix,
                    bag.MaxTop.Value,
                    globalMaxTop.Value));
        }
        return Build(bag, globalOptions);
    }

    /// <summary>
    /// Builds the route's <see cref="ODataValidationSettings"/> silently
    /// (no warnings). Used per-request by <c>RestierController</c>.
    /// </summary>
    public static ODataValidationSettings Build(
        RestierValidationOptions bag,
        ODataOptions globalOptions)
    {
        var resolved = new ODataValidationSettings();

        // ODataOptions.QueryConfigurations.MaxTop uses 0 as the "unset" sentinel; treat it as null.
        var rawGlobal = globalOptions?.QueryConfigurations?.MaxTop;
        var globalMaxTop = rawGlobal.HasValue && rawGlobal.Value > 0 ? rawGlobal : null;
        resolved.MaxTop = bag.MaxTop ?? globalMaxTop;

        // ODataValidationSettings.MaxSkip is int?, the others are int — that's why the assignment shape differs.
        if (bag.MaxSkip.HasValue) { resolved.MaxSkip = bag.MaxSkip; }
        if (bag.MaxExpansionDepth.HasValue) { resolved.MaxExpansionDepth = bag.MaxExpansionDepth.Value; }
        if (bag.MaxAnyAllExpressionDepth.HasValue) { resolved.MaxAnyAllExpressionDepth = bag.MaxAnyAllExpressionDepth.Value; }
        if (bag.MaxOrderByNodeCount.HasValue) { resolved.MaxOrderByNodeCount = bag.MaxOrderByNodeCount.Value; }
        if (bag.MaxNodeCount.HasValue) { resolved.MaxNodeCount = bag.MaxNodeCount.Value; }

        return resolved;
    }
}
