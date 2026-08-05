// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Composite <see cref="IApiVersionDescriptionProvider"/>: merges descriptions from an
    /// optional inner provider (typically Asp.Versioning's
    /// <c>DefaultApiVersionDescriptionProvider</c>, which reports MVC-controller versions)
    /// with descriptions sourced from <see cref="IRestierApiVersionRegistry"/>. Honors the
    /// materialization invariant by touching <c>IOptions&lt;ODataOptions&gt;.Value</c> before
    /// reading the registry.
    /// </summary>
    internal sealed class RestierApiVersionDescriptionProvider : IApiVersionDescriptionProvider
    {

        private readonly IOptions<ODataOptions> _odataOptions;
        private readonly IRestierApiVersionRegistry _registry;
        private readonly IApiVersionDescriptionProvider _inner;

        public RestierApiVersionDescriptionProvider(
            IOptions<ODataOptions> odataOptions,
            IRestierApiVersionRegistry registry,
            IApiVersionDescriptionProvider inner)
        {
            _odataOptions = odataOptions ?? throw new ArgumentNullException(nameof(odataOptions));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _inner = inner;   // optional
        }

        public IReadOnlyList<ApiVersionDescription> ApiVersionDescriptions
        {
            get
            {
                // Materialization invariant: read IOptions<ODataOptions>.Value before
                // consulting the registry so that ApiExplorer/Swashbuckle/NSwag consumers
                // resolving this provider during host startup see a populated registry.
                _ = _odataOptions.Value;

                IEnumerable<ApiVersionDescription> innerDescriptions = _inner?.ApiVersionDescriptions
                    ?? Array.Empty<ApiVersionDescription>();

                var registryDescriptions = _registry.Descriptors
                    .Select(d => new ApiVersionDescription(
                        IApiVersionParserExtensions.Parse(ApiVersionParser.Default, d.Version),
                        d.GroupName,
                        d.IsDeprecated));

                return innerDescriptions.Concat(registryDescriptions).ToArray();
            }
        }

        /// <summary>
        /// Returns whether the given <paramref name="apiVersion"/> is deprecated.
        /// If the registry has descriptors for the version, the "all-must-be-deprecated"
        /// rule applies. If not, the flag is sourced from the inner provider's descriptions
        /// (so a controller-only version's deprecation flag still surfaces).
        /// </summary>
        public bool IsDeprecated(ApiVersion apiVersion)
        {
            if (apiVersion is null)
            {
                return false;
            }

            // Materialization invariant.
            _ = _odataOptions.Value;

            var versionString = apiVersion.ToString();
            var registryMatches = _registry.Descriptors
                .Where(d => string.Equals(d.Version, versionString, StringComparison.Ordinal))
                .ToArray();

            if (registryMatches.Length > 0)
            {
                return registryMatches.All(d => d.IsDeprecated);
            }

            // Not a Restier-registered version; defer to the inner provider (e.g., MVC controllers)
            // by inspecting its ApiVersionDescriptions list.
            if (_inner != null)
            {
                var innerDesc = _inner.ApiVersionDescriptions
                    .FirstOrDefault(d => d.ApiVersion == apiVersion);
                if (innerDesc != null)
                {
                    return innerDesc.IsDeprecated;
                }
            }

            return false;
        }

    }

}
