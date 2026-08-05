// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Built-in <see cref="ApiVersion"/>-to-URL-segment formatters.
    /// </summary>
    public static class ApiVersionSegmentFormatters
    {

        /// <summary>
        /// Formats an <see cref="ApiVersion"/> as <c>v{Major}</c> (e.g., "v1").
        /// </summary>
        public static Func<ApiVersion, string> Major { get; } = static v => $"v{v.MajorVersion}";

        /// <summary>
        /// Formats an <see cref="ApiVersion"/> as <c>v{Major}.{Minor}</c> (e.g., "v1.0").
        /// </summary>
        public static Func<ApiVersion, string> MajorMinor { get; } = static v => $"v{v.MajorVersion}.{v.MinorVersion}";

    }

}
