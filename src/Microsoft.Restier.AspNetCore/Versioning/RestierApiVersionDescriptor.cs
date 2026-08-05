// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Read-only description of a single versioned Restier route.
    /// Populated by the Microsoft.Restier.AspNetCore.Versioning package and consumed by
    /// version-aware OpenAPI integrations (NSwag, Swagger) and the version-discovery
    /// response-header middleware.
    /// </summary>
    public sealed class RestierApiVersionDescriptor
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierApiVersionDescriptor"/> class.
        /// </summary>
        /// <param name="version">The version string (e.g., "1.0").</param>
        /// <param name="basePrefix">The logical API group key — the <c>basePrefix</c> passed to <c>AddVersion</c>.</param>
        /// <param name="routePrefix">The composed route prefix (e.g., "api/v1").</param>
        /// <param name="apiType">The <see cref="Microsoft.Restier.Core.ApiBase"/>-derived type for this version.</param>
        /// <param name="isDeprecated">Whether this version is deprecated.</param>
        /// <param name="groupName">The group name used as the OpenAPI document name (e.g., "v1").</param>
        /// <param name="sunsetDate">Optional sunset date emitted via the <c>Sunset</c> response header.</param>
        public RestierApiVersionDescriptor(
            string version,
            string basePrefix,
            string routePrefix,
            Type apiType,
            bool isDeprecated,
            string groupName,
            DateTimeOffset? sunsetDate)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            BasePrefix = basePrefix ?? throw new ArgumentNullException(nameof(basePrefix));
            RoutePrefix = routePrefix ?? throw new ArgumentNullException(nameof(routePrefix));
            ApiType = apiType ?? throw new ArgumentNullException(nameof(apiType));
            IsDeprecated = isDeprecated;
            GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
            SunsetDate = sunsetDate;
        }

        /// <summary>The version string (e.g., "1.0").</summary>
        public string Version { get; }

        /// <summary>The logical API group key — the <c>basePrefix</c> passed to <c>AddVersion</c>.</summary>
        public string BasePrefix { get; }

        /// <summary>The composed route prefix (e.g., "api/v1").</summary>
        public string RoutePrefix { get; }

        /// <summary>The <see cref="Microsoft.Restier.Core.ApiBase"/>-derived type for this version.</summary>
        public Type ApiType { get; }

        /// <summary>Whether this version is deprecated.</summary>
        public bool IsDeprecated { get; }

        /// <summary>The group name used as the OpenAPI document name (e.g., "v1").</summary>
        public string GroupName { get; }

        /// <summary>Optional sunset date emitted via the <c>Sunset</c> response header.</summary>
        public DateTimeOffset? SunsetDate { get; }

    }

}
