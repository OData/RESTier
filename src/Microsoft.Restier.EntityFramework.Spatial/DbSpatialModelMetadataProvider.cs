// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Reflection;
using Microsoft.Restier.Core.Spatial;

namespace Microsoft.Restier.EntityFramework.Spatial
{
    /// <summary>
    /// EF6 implementation of <see cref="ISpatialModelMetadataProvider"/>. Genus is fully determined
    /// by the storage CLR type (<see cref="DbGeography"/> vs <see cref="DbGeometry"/>); the
    /// <c>providerContext</c> argument is unused.
    /// </summary>
    public class DbSpatialModelMetadataProvider : ISpatialModelMetadataProvider
    {
        private static readonly Type[] StorageTypes = { typeof(DbGeography), typeof(DbGeometry) };

        /// <inheritdoc />
        public bool IsSpatialStorageType(Type clrType)
        {
            if (clrType is null)
            {
                return false;
            }

            return typeof(DbGeography).IsAssignableFrom(clrType)
                || typeof(DbGeometry).IsAssignableFrom(clrType);
        }

        /// <inheritdoc />
        public SpatialGenus? InferGenus(Type entityClrType, PropertyInfo property, object providerContext)
        {
            if (property is null)
            {
                return null;
            }

            var t = property.PropertyType;
            if (typeof(DbGeography).IsAssignableFrom(t))
            {
                return SpatialGenus.Geography;
            }

            if (typeof(DbGeometry).IsAssignableFrom(t))
            {
                return SpatialGenus.Geometry;
            }

            return null;
        }

        /// <inheritdoc />
        public IReadOnlyList<Type> IgnoredStorageTypes => StorageTypes;
    }
}
