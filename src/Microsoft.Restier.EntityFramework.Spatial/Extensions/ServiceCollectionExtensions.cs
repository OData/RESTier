// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core.Spatial;

namespace Microsoft.Restier.EntityFramework.Spatial
{
    /// <summary>
    /// Extension methods for registering EF6 spatial types support with Restier.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the EF6 <see cref="DbSpatialConverter"/> and <see cref="DbSpatialModelMetadataProvider"/>
        /// in the route service container so that spatial properties round-trip through Microsoft.Spatial.
        /// Idempotent.
        /// </summary>
        public static IServiceCollection AddRestierSpatial(this IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISpatialTypeConverter, DbSpatialConverter>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISpatialModelMetadataProvider, DbSpatialModelMetadataProvider>());
            return services;
        }
    }
}
