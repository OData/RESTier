// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Fluent builder used to declare versioned Restier routes. Each <c>AddVersion</c> call
    /// captures a pending registration applied when <c>ODataOptions</c> materializes.
    /// </summary>
    public interface IRestierApiVersioningBuilder
    {

        /// <summary>
        /// Registers one or more versions for <typeparamref name="TApi"/>, reading every
        /// <c>[ApiVersion]</c> attribute on the type.
        /// </summary>
        /// <typeparam name="TApi">The <see cref="ApiBase"/>-derived type for these versions.</typeparam>
        /// <param name="basePrefix">The logical API prefix; the version segment is appended to it.</param>
        /// <param name="configureRouteServices">Per-route DI configuration delegate.</param>
        /// <param name="configureVersioning">Optional per-call versioning options (segment formatter, sunset, explicit prefix).</param>
        /// <param name="configureOptions">Optional callback to mutate the per-route <see cref="RestierRouteOptions"/> bag.</param>
        IRestierApiVersioningBuilder AddVersion<TApi>(
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            Action<RestierRouteOptions> configureOptions = null)
            where TApi : ApiBase;

        /// <summary>
        /// Registers a specific <paramref name="apiVersion"/> for <typeparamref name="TApi"/>,
        /// without reading any <c>[ApiVersion]</c> attribute.
        /// </summary>
        /// <typeparam name="TApi">The <see cref="ApiBase"/>-derived type for this version.</typeparam>
        /// <param name="apiVersion">The version to register.</param>
        /// <param name="deprecated">Whether this version is deprecated.</param>
        /// <param name="basePrefix">The logical API prefix; the version segment is appended to it.</param>
        /// <param name="configureRouteServices">Per-route DI configuration delegate.</param>
        /// <param name="configureVersioning">Optional per-call versioning options (segment formatter, sunset, explicit prefix).</param>
        /// <param name="configureOptions">Optional callback to mutate the per-route <see cref="RestierRouteOptions"/> bag.</param>
        IRestierApiVersioningBuilder AddVersion<TApi>(
            ApiVersion apiVersion,
            bool deprecated,
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            Action<RestierRouteOptions> configureOptions = null)
            where TApi : ApiBase;

    }

}
