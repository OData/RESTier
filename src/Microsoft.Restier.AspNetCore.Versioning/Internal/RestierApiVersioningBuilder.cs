// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore.Versioning.Internal
{

    /// <summary>
    /// Concrete <see cref="IRestierApiVersioningBuilder"/>. Mutable across multiple
    /// <c>AddRestierApiVersioning</c> calls; its pending registrations are drained by the
    /// options configurator.
    /// </summary>
    internal sealed class RestierApiVersioningBuilder : IRestierApiVersioningBuilder
    {

        private readonly List<PendingVersionRegistration> _pending = new();
        private readonly object _lock = new();

        public IReadOnlyList<PendingVersionRegistration> PendingRegistrations
        {
            get
            {
                lock (_lock)
                {
                    return _pending.ToArray();
                }
            }
        }

        public IRestierApiVersioningBuilder AddVersion<TApi>(
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            Action<RestierRouteOptions> configureOptions = null)
            where TApi : ApiBase
        {
            if (basePrefix is null)
            {
                throw new ArgumentNullException(nameof(basePrefix));
            }

            if (configureRouteServices is null)
            {
                throw new ArgumentNullException(nameof(configureRouteServices));
            }

            foreach (var read in ApiVersionAttributeReader.Read(typeof(TApi)))
            {
                lock (_lock)
                {
                    _pending.Add(new PendingVersionRegistration(
                        typeof(TApi),
                        read.ApiVersion,
                        read.IsDeprecated,
                        basePrefix,
                        configureRouteServices,
                        configureVersioning,
                        configureOptions));
                }
            }

            return this;
        }

        public IRestierApiVersioningBuilder AddVersion<TApi>(
            ApiVersion apiVersion,
            bool deprecated,
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            Action<RestierRouteOptions> configureOptions = null)
            where TApi : ApiBase
        {
            if (apiVersion is null)
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            if (basePrefix is null)
            {
                throw new ArgumentNullException(nameof(basePrefix));
            }

            if (configureRouteServices is null)
            {
                throw new ArgumentNullException(nameof(configureRouteServices));
            }

            lock (_lock)
            {
                _pending.Add(new PendingVersionRegistration(
                    typeof(TApi),
                    apiVersion,
                    deprecated,
                    basePrefix,
                    configureRouteServices,
                    configureVersioning,
                    configureOptions));
            }

            return this;
        }

    }

}
