// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Asp.Versioning;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore.Versioning.Internal
{

    /// <summary>
    /// <see cref="IConfigureOptions{ODataOptions}"/> that drains the builder's pending
    /// version registrations and applies them to the materialized <c>ODataOptions</c>.
    /// </summary>
    internal sealed class RestierApiVersioningOptionsConfigurator : IConfigureOptions<ODataOptions>
    {

        private readonly RestierApiVersioningBuilder _builder;
        private readonly RestierApiVersionRegistry _registry;
        private bool _hasRun;
        private readonly object _lock = new();

        public RestierApiVersioningOptionsConfigurator(
            RestierApiVersioningBuilder builder,
            RestierApiVersionRegistry registry)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Configure(ODataOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            lock (_lock)
            {
                if (_hasRun)
                {
                    return;
                }

                _hasRun = true;
            }

            foreach (var pending in _builder.PendingRegistrations)
            {
                ApplyOne(options, pending);
            }
        }

        private void ApplyOne(ODataOptions options, PendingVersionRegistration pending)
        {
            var versioningOptions = new RestierVersioningOptions();
            pending.ApplyVersioningOptions?.Invoke(versioningOptions);

            // P2 fix: normalize basePrefix by trimming trailing slash so "api" and "api/" are equivalent.
            var normalizedBasePrefix = (pending.BasePrefix ?? string.Empty).TrimEnd('/');

            var groupName = versioningOptions.GroupNameFormatter?.Invoke(pending.ApiVersion)
                ?? versioningOptions.SegmentFormatter(pending.ApiVersion);
            var routePrefix = versioningOptions.ExplicitRoutePrefix
                ?? ComposePrefix(normalizedBasePrefix, versioningOptions.SegmentFormatter(pending.ApiVersion));

            // Duplicate detection: same (ApiVersion, BasePrefix) is rejected.
            var collision = _registry.Descriptors.FirstOrDefault(d =>
                string.Equals(d.Version, pending.ApiVersion.ToString(), StringComparison.Ordinal)
                && string.Equals(d.BasePrefix, normalizedBasePrefix, StringComparison.Ordinal));
            if (collision is not null)
            {
                throw new InvalidOperationException(
                    $"A Restier API version is already registered with version {pending.ApiVersion} at base prefix " +
                    $"\"{normalizedBasePrefix}\" for type {collision.ApiType.FullName}; " +
                    $"refused to register conflicting type {pending.ApiType.FullName}.");
            }

            // P1 fix: detect GroupName collision across different basePrefixes.
            var groupNameCollision = _registry.Descriptors.FirstOrDefault(d =>
                string.Equals(d.GroupName, groupName, StringComparison.Ordinal)
                && !string.Equals(d.BasePrefix, normalizedBasePrefix, StringComparison.Ordinal));
            if (groupNameCollision is not null)
            {
                throw new InvalidOperationException(
                    $"A Restier API descriptor with GroupName \"{groupName}\" is already registered for base prefix " +
                    $"\"{groupNameCollision.BasePrefix}\" (type {groupNameCollision.ApiType.FullName}). " +
                    $"The new registration at base prefix \"{normalizedBasePrefix}\" (type {pending.ApiType.FullName}) " +
                    $"would create a duplicate OpenAPI document group name. " +
                    $"Set {nameof(RestierVersioningOptions)}.{nameof(RestierVersioningOptions.GroupNameFormatter)} " +
                    $"on each AddVersion call to produce distinct group names.");
            }

            _registry.Add(
                pending.ApiVersion,
                normalizedBasePrefix,
                routePrefix,
                pending.ApiType,
                pending.IsDeprecated,
                groupName,
                versioningOptions.SunsetDate);

            // Reflect into the AddRestierRoute extension. The generic constraint makes this
            // a one-time cost per host boot.
            var addRestierRoute = typeof(RestierODataOptionsExtensions)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(m => m.Name == nameof(RestierODataOptionsExtensions.AddRestierRoute)
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 4);
            var closed = addRestierRoute.MakeGenericMethod(pending.ApiType);
            closed.Invoke(null, new object[]
            {
                options,
                routePrefix,
                pending.ConfigureRouteServices,
                pending.ConfigureOptions,
            });
        }

        private static string ComposePrefix(string basePrefix, string segment)
        {
            if (string.IsNullOrEmpty(basePrefix))
            {
                return segment;
            }

            return basePrefix.TrimEnd('/') + "/" + segment;
        }

    }

}
