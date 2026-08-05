// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core.Spatial;

namespace Microsoft.Restier.EntityFrameworkCore.Spatial
{
    /// <summary>
    /// Extension methods for registering EF Core spatial types support with Restier.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the EF Core <see cref="NtsSpatialConverter"/> and
        /// <see cref="NtsSpatialModelMetadataProvider"/> in the route service container so that
        /// spatial properties round-trip through Microsoft.Spatial. Idempotent.
        /// </summary>
        public static IServiceCollection AddRestierSpatial(this IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISpatialTypeConverter, NtsSpatialConverter>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISpatialModelMetadataProvider, NtsSpatialModelMetadataProvider>());
            return services;
        }
    }
}
