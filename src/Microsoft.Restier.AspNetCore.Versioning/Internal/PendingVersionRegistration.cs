// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore.Versioning.Internal
{

    /// <summary>
    /// One pending versioned-route registration captured by
    /// <see cref="IRestierApiVersioningBuilder.AddVersion{TApi}(string, Action{IServiceCollection}, Action{RestierVersioningOptions}, Action{RestierRouteOptions})"/>
    /// (and overloads) and consumed by <c>RestierApiVersioningOptionsConfigurator</c> when
    /// <c>ODataOptions</c> materializes.
    /// </summary>
    internal sealed class PendingVersionRegistration
    {

        public PendingVersionRegistration(
            Type apiType,
            ApiVersion apiVersion,
            bool isDeprecated,
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> applyVersioningOptions,
            Action<RestierRouteOptions> configureOptions)
        {
            ApiType = apiType;
            ApiVersion = apiVersion;
            IsDeprecated = isDeprecated;
            BasePrefix = basePrefix;
            ConfigureRouteServices = configureRouteServices;
            ApplyVersioningOptions = applyVersioningOptions;
            ConfigureOptions = configureOptions;
        }

        public Type ApiType { get; }

        public ApiVersion ApiVersion { get; }

        public bool IsDeprecated { get; }

        public string BasePrefix { get; }

        public Action<IServiceCollection> ConfigureRouteServices { get; }

        public Action<RestierVersioningOptions> ApplyVersioningOptions { get; }

        public Action<RestierRouteOptions> ConfigureOptions { get; }

    }

}
