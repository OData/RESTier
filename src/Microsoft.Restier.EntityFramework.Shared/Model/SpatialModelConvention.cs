// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Spatial;

namespace Microsoft.Restier.EntityFramework.Shared.Model
{
    /// <summary>
    /// Adds Microsoft.Spatial primitive properties to the EDM model in place of storage-typed
    /// (DbGeography / DbGeometry / NetTopologySuite Geometry) properties on entity types.
    /// Invoked in two phases by <c>EFModelBuilder</c> around <c>ODataConventionModelBuilder.GetEdmModel</c>.
    /// </summary>
    public class SpatialModelConvention
    {
        private readonly IReadOnlyList<ISpatialModelMetadataProvider> providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialModelConvention"/> class.
        /// </summary>
        /// <param name="providers">The set of EF-flavor-specific spatial metadata providers.</param>
        public SpatialModelConvention(IEnumerable<ISpatialModelMetadataProvider> providers)
        {
            this.providers = providers?.ToArray() ?? Array.Empty<ISpatialModelMetadataProvider>();
        }

        /// <summary>
        /// Gets a value indicating whether any spatial metadata providers were supplied.
        /// </summary>
        public bool HasProviders => providers.Count > 0;

        /// <summary>
        /// Per-property capture produced by <see cref="CapturePhase"/> and consumed by the augment phase.
        /// </summary>
        public sealed class Capture
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Capture"/> class.
            /// </summary>
            /// <param name="entityClrType">The CLR entity type that owns the property.</param>
            /// <param name="propertyInfo">The CLR property declaration.</param>
            /// <param name="resolvedEdmType">The resolved Microsoft.Spatial EDM CLR type.</param>
            public Capture(Type entityClrType, PropertyInfo propertyInfo, Type resolvedEdmType)
            {
                EntityClrType = entityClrType;
                PropertyInfo = propertyInfo;
                ResolvedEdmType = resolvedEdmType;
            }

            /// <summary>Gets the CLR entity type that owns the property.</summary>
            public Type EntityClrType { get; }

            /// <summary>Gets the CLR property declaration.</summary>
            public PropertyInfo PropertyInfo { get; }

            /// <summary>Gets the resolved Microsoft.Spatial EDM CLR type.</summary>
            public Type ResolvedEdmType { get; }
        }

        /// <summary>
        /// Phase 1: capture spatial-typed properties for phase 2 and ignore them in the
        /// underlying convention builder.
        /// </summary>
        /// <param name="builder">The convention model builder to mutate by adding <c>Ignore</c> calls.</param>
        /// <param name="entityClrTypes">The set of CLR entity types to inspect.</param>
        /// <param name="providerContext">Flavor-specific context handed to each provider (e.g. <c>DbContext</c> for EF Core).</param>
        /// <returns>The captures, one per spatial-typed property.</returns>
        public IReadOnlyList<Capture> CapturePhase(
            ODataConventionModelBuilder builder,
            IEnumerable<Type> entityClrTypes,
            object providerContext)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (!HasProviders)
            {
                return Array.Empty<Capture>();
            }

            var captures = new List<Capture>();

            foreach (var entityType in entityClrTypes ?? Array.Empty<Type>())
            {
                var typeConfig = builder.GetTypeConfigurationOrNull(entityType) as StructuralTypeConfiguration;

                foreach (var prop in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!IsAnyProviderSpatialStorageType(prop.PropertyType))
                    {
                        continue;
                    }

                    var resolved = ResolveEdmType(entityType, prop, providerContext);
                    captures.Add(new Capture(entityType, prop, resolved));

                    // Remove the property from the entity configuration so the convention builder
                    // doesn't try to emit it as a structural/navigation property using the storage type.
                    typeConfig?.RemoveProperty(prop);
                }
            }

            var allIgnored = providers.SelectMany(p => p.IgnoredStorageTypes).Distinct().ToArray();
            if (allIgnored.Length > 0)
            {
                builder.Ignore(allIgnored);
            }

            return captures;
        }

        private bool IsAnyProviderSpatialStorageType(Type clrType)
        {
            for (var i = 0; i < providers.Count; i++)
            {
                if (providers[i].IsSpatialStorageType(clrType))
                {
                    return true;
                }
            }

            return false;
        }

        private Type ResolveEdmType(Type entityClrType, PropertyInfo prop, object providerContext)
        {
            var spatial = prop.GetCustomAttribute<SpatialAttribute>();
            if (spatial is not null)
            {
                ValidateSpatialAttribute(entityClrType, prop, spatial, providerContext);
                return spatial.EdmType;
            }

            SpatialGenus? genus = null;
            for (var i = 0; i < providers.Count; i++)
            {
                if (providers[i].IsSpatialStorageType(prop.PropertyType))
                {
                    genus = providers[i].InferGenus(entityClrType, prop, providerContext);
                    if (genus.HasValue)
                    {
                        break;
                    }
                }
            }

            if (!genus.HasValue)
            {
                throw new EdmModelValidationException(
                    $"Cannot determine spatial genus (Geography vs Geometry) for property '{entityClrType.Name}.{prop.Name}'. " +
                    $"Annotate the property with [Spatial(typeof(GeographyPoint))] or configure HasColumnType.");
            }

            return MapGenusToAbstractEdmType(prop.PropertyType, genus.Value);
        }

        private static Type MapGenusToAbstractEdmType(Type storageType, SpatialGenus genus)
        {
            var name = storageType.Name;
            if (genus == SpatialGenus.Geography)
            {
                return name switch
                {
                    "Point" => typeof(GeographyPoint),
                    "LineString" => typeof(GeographyLineString),
                    "Polygon" => typeof(GeographyPolygon),
                    "MultiPoint" => typeof(GeographyMultiPoint),
                    "MultiLineString" => typeof(GeographyMultiLineString),
                    "MultiPolygon" => typeof(GeographyMultiPolygon),
                    "GeometryCollection" => typeof(GeographyCollection),
                    _ => typeof(Geography),
                };
            }

            return name switch
            {
                "Point" => typeof(GeometryPoint),
                "LineString" => typeof(GeometryLineString),
                "Polygon" => typeof(GeometryPolygon),
                "MultiPoint" => typeof(GeometryMultiPoint),
                "MultiLineString" => typeof(GeometryMultiLineString),
                "MultiPolygon" => typeof(GeometryMultiPolygon),
                "GeometryCollection" => typeof(GeometryCollection),
                _ => typeof(Geometry),
            };
        }

        /// <summary>
        /// Phase 2: after <c>builder.GetEdmModel()</c>, add the structural properties for the captured spatial
        /// properties to the corresponding <see cref="IEdmEntityType"/>s, applying the active naming convention
        /// and attaching <see cref="ClrPropertyInfoAnnotation"/> so Restier's CLR-name resolver works.
        /// </summary>
        /// <param name="model">The EDM model returned by <c>ODataConventionModelBuilder.GetEdmModel</c>.</param>
        /// <param name="captures">The captures produced by <see cref="CapturePhase"/>.</param>
        /// <param name="namingConvention">The active Restier naming convention.</param>
        public void AugmentPhase(
            EdmModel model,
            IReadOnlyList<Capture> captures,
            RestierNamingConvention namingConvention)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (captures is null || captures.Count == 0)
            {
                return;
            }

            foreach (var c in captures)
            {
                var entityEdmType = FindEntityType(model, c.EntityClrType);
                if (entityEdmType is null)
                {
                    continue;
                }

                var edmName = ApplyNamingConvention(c.PropertyInfo.Name, namingConvention);
                var primitiveKind = MapEdmTypeToPrimitiveKind(c.ResolvedEdmType);
                var primitiveType = EdmCoreModel.Instance.GetPrimitive(primitiveKind, isNullable: true);

                var added = entityEdmType.AddStructuralProperty(edmName, primitiveType);

                model.SetAnnotationValue(added, new ClrPropertyInfoAnnotation(c.PropertyInfo));
            }
        }

        private static EdmEntityType FindEntityType(EdmModel model, Type clrType)
        {
            foreach (var schemaElement in model.SchemaElements)
            {
                if (schemaElement is EdmEntityType edmEntity && string.Equals(edmEntity.Name, clrType.Name, StringComparison.Ordinal))
                {
                    return edmEntity;
                }
            }

            return null;
        }

        private static string ApplyNamingConvention(string clrName, RestierNamingConvention naming)
        {
            if (naming == RestierNamingConvention.LowerCamelCase
                || naming == RestierNamingConvention.LowerCamelCaseWithEnumMembers)
            {
                if (string.IsNullOrEmpty(clrName))
                {
                    return clrName;
                }

                return char.ToLowerInvariant(clrName[0]) + clrName.Substring(1);
            }

            return clrName;
        }

        private static EdmPrimitiveTypeKind MapEdmTypeToPrimitiveKind(Type microsoftSpatialType)
        {
            if (microsoftSpatialType == typeof(GeographyPoint))
            {
                return EdmPrimitiveTypeKind.GeographyPoint;
            }

            if (microsoftSpatialType == typeof(GeographyLineString))
            {
                return EdmPrimitiveTypeKind.GeographyLineString;
            }

            if (microsoftSpatialType == typeof(GeographyPolygon))
            {
                return EdmPrimitiveTypeKind.GeographyPolygon;
            }

            if (microsoftSpatialType == typeof(GeographyMultiPoint))
            {
                return EdmPrimitiveTypeKind.GeographyMultiPoint;
            }

            if (microsoftSpatialType == typeof(GeographyMultiLineString))
            {
                return EdmPrimitiveTypeKind.GeographyMultiLineString;
            }

            if (microsoftSpatialType == typeof(GeographyMultiPolygon))
            {
                return EdmPrimitiveTypeKind.GeographyMultiPolygon;
            }

            if (microsoftSpatialType == typeof(GeographyCollection))
            {
                return EdmPrimitiveTypeKind.GeographyCollection;
            }

            if (microsoftSpatialType == typeof(Geography))
            {
                return EdmPrimitiveTypeKind.Geography;
            }

            if (microsoftSpatialType == typeof(GeometryPoint))
            {
                return EdmPrimitiveTypeKind.GeometryPoint;
            }

            if (microsoftSpatialType == typeof(GeometryLineString))
            {
                return EdmPrimitiveTypeKind.GeometryLineString;
            }

            if (microsoftSpatialType == typeof(GeometryPolygon))
            {
                return EdmPrimitiveTypeKind.GeometryPolygon;
            }

            if (microsoftSpatialType == typeof(GeometryMultiPoint))
            {
                return EdmPrimitiveTypeKind.GeometryMultiPoint;
            }

            if (microsoftSpatialType == typeof(GeometryMultiLineString))
            {
                return EdmPrimitiveTypeKind.GeometryMultiLineString;
            }

            if (microsoftSpatialType == typeof(GeometryMultiPolygon))
            {
                return EdmPrimitiveTypeKind.GeometryMultiPolygon;
            }

            if (microsoftSpatialType == typeof(GeometryCollection))
            {
                return EdmPrimitiveTypeKind.GeometryCollection;
            }

            if (microsoftSpatialType == typeof(Geometry))
            {
                return EdmPrimitiveTypeKind.Geometry;
            }

            throw new ArgumentException(
                $"Type '{microsoftSpatialType.FullName}' is not a recognized Microsoft.Spatial EDM primitive type.",
                nameof(microsoftSpatialType));
        }

        private void ValidateSpatialAttribute(
            Type entityClrType,
            PropertyInfo prop,
            SpatialAttribute spatial,
            object providerContext)
        {
            if (spatial.EdmType is null
                || (!typeof(Geography).IsAssignableFrom(spatial.EdmType)
                    && !typeof(Geometry).IsAssignableFrom(spatial.EdmType)))
            {
                throw new EdmModelValidationException(
                    $"[Spatial] on '{entityClrType.Name}.{prop.Name}' specifies type '{spatial.EdmType?.FullName ?? "<null>"}' " +
                    $"which is not a Microsoft.Spatial primitive type (subclass of Microsoft.Spatial.Geography or Geometry).");
            }

            var attributeGenus = typeof(Geography).IsAssignableFrom(spatial.EdmType)
                ? SpatialGenus.Geography
                : SpatialGenus.Geometry;

            for (var i = 0; i < providers.Count; i++)
            {
                if (!providers[i].IsSpatialStorageType(prop.PropertyType))
                {
                    continue;
                }

                var inferred = providers[i].InferGenus(entityClrType, prop, providerContext);
                if (inferred.HasValue && inferred.Value != attributeGenus)
                {
                    throw new EdmModelValidationException(
                        $"[Spatial] on '{entityClrType.Name}.{prop.Name}' declares genus '{attributeGenus}' " +
                        $"but the storage property's inferred genus is '{inferred.Value}'.");
                }
            }
        }
    }
}
