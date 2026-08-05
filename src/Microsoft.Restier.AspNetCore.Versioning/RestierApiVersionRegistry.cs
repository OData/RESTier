// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Concrete <see cref="IRestierApiVersionRegistry"/>. Append-only; descriptors are
    /// added by <c>RestierApiVersioningOptionsConfigurator</c> when
    /// <c>ODataOptions</c> materializes. Registered as a singleton.
    /// </summary>
    internal sealed class RestierApiVersionRegistry : IRestierApiVersionRegistry
    {

        private readonly List<RestierApiVersionDescriptor> _descriptors = new();
        private readonly object _lock = new();

        public IReadOnlyList<RestierApiVersionDescriptor> Descriptors
        {
            get
            {
                lock (_lock)
                {
                    return _descriptors.ToArray();
                }
            }
        }

        public RestierApiVersionDescriptor Add(
            ApiVersion apiVersion,
            string basePrefix,
            string routePrefix,
            Type apiType,
            bool isDeprecated,
            string groupName,
            DateTimeOffset? sunsetDate)
        {
            if (apiVersion is null)
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            var descriptor = new RestierApiVersionDescriptor(
                apiVersion.ToString(),
                basePrefix,
                routePrefix,
                apiType,
                isDeprecated,
                groupName,
                sunsetDate);

            lock (_lock)
            {
                _descriptors.Add(descriptor);
            }

            return descriptor;
        }

        public RestierApiVersionDescriptor FindByPrefix(string routePrefix)
        {
            if (routePrefix is null)
            {
                return null;
            }

            lock (_lock)
            {
                return _descriptors.FirstOrDefault(d => string.Equals(d.RoutePrefix, routePrefix, StringComparison.Ordinal));
            }
        }

        public RestierApiVersionDescriptor FindByGroupName(string groupName)
        {
            if (groupName is null)
            {
                return null;
            }

            lock (_lock)
            {
                return _descriptors.FirstOrDefault(d => string.Equals(d.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public IReadOnlyList<RestierApiVersionDescriptor> FindByBasePrefix(string basePrefix)
        {
            if (basePrefix is null)
            {
                return Array.Empty<RestierApiVersionDescriptor>();
            }

            lock (_lock)
            {
                return _descriptors.Where(d => string.Equals(d.BasePrefix, basePrefix, StringComparison.Ordinal)).ToArray();
            }
        }

    }

}
