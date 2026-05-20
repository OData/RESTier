// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Spatial;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// This class contains some common extension methods for Edm.
    /// </summary>
    public static class EdmHelpers
    {
        private const string DefaultEntityContainerName = "DefaultContainer";

        /// <summary>
        /// The type to get the primitive type reference. Nullability mirrors the CLR type:
        /// <c>Nullable&lt;T&gt;</c> emits nullable; plain value types emit non-nullable.
        /// </summary>
        /// <param name="type">The clr type to get edm type reference.</param>
        /// <returns>The edm type reference for the clr type.</returns>
        public static EdmTypeReference GetPrimitiveTypeReference(this Type type)
            => type.GetPrimitiveTypeReference(nullable: false);

        /// <summary>
        /// The type to get the primitive type reference with explicit nullability.
        /// </summary>
        /// <param name="type">The clr type to get edm type reference.</param>
        /// <param name="nullable">
        /// Whether the resulting type reference should be marked nullable. For <c>Nullable&lt;T&gt;</c>
        /// inputs the reference is always nullable regardless of this argument.
        /// </param>
        /// <returns>The edm type reference for the clr type.</returns>
        public static EdmTypeReference GetPrimitiveTypeReference(this Type type, bool nullable)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var primitiveTypeKind = EdmHelpers.GetPrimitiveTypeKind(type, out var isNullableValueType);

            if (!primitiveTypeKind.HasValue)
            {
                return null;
            }

            // Nullable<T> always emits nullable. Otherwise honor the caller's hint.
            return new EdmPrimitiveTypeReference(
                EdmCoreModel.Instance.GetPrimitiveType(primitiveTypeKind.Value),
                isNullableValueType || nullable);
        }

        /// <summary>
        /// Get the clr type for a specified edm type.
        /// </summary>
        /// <param name="edmType">The edm type to get clr type.</param>
        /// <param name="edmModel">The edm model.</param>
        /// <returns>The clr type.</returns>
        public static Type GetClrType(this IEdmType edmType, IEdmModel edmModel)
        {
            Ensure.NotNull(edmType, nameof(edmType));
            Ensure.NotNull(edmModel, nameof(edmModel));

            var annotation = edmModel.GetAnnotationValue<ClrTypeAnnotation>(edmType);
            if (annotation is not null)
            {
                return annotation.ClrType;
            }

            throw new NotSupportedException(string.Format(
                CultureInfo.InvariantCulture,
                Resources.ElementTypeNotFound,
                edmType.FullTypeName()));
        }

        /// <summary>
        /// Get the edm type reference for a clr type. Enum, complex, and entity types are
        /// emitted as nullable; primitive types mirror CLR nullability (<c>Nullable&lt;T&gt;</c>
        /// is nullable, plain value types are non-nullable).
        /// </summary>
        /// <param name="type">The clr type.</param>
        /// <param name="model">The Edm model.</param>
        /// <returns>The Edm type reference.</returns>
        public static IEdmTypeReference GetTypeReference(this Type type, IEdmModel model)
        {
            if (type is null || model is null)
            {
                return null;
            }

            // NOTE: This overload preserves the original (pre-vNext-magical-ops) behavior:
            // declared enum/complex/entity types are hardcoded nullable=true, primitives
            // follow CLR nullability via GetPrimitiveTypeReference(). The three-arg overload
            // (GetTypeReference(type, model, bool nullable)) supports explicit per-call
            // nullability and does NOT subsume this method — they intentionally diverge.

            if (type.TryGetElementType(out var elementType))
            {
                return EdmCoreModel.GetCollection(GetTypeReference(elementType, model));
            }

            var edmType = model.FindDeclaredType(type.FullName);

            if (edmType is IEdmEnumType enumType)
            {
                return new EdmEnumTypeReference(enumType, true);
            }

            if (edmType is IEdmComplexType complexType)
            {
                return new EdmComplexTypeReference(complexType, true);
            }

            if (edmType is IEdmEntityType entityType)
            {
                return new EdmEntityTypeReference(entityType, true);
            }

            return type.GetPrimitiveTypeReference();
        }

        /// <summary>
        /// Get the edm type reference for a clr type with explicit control over nullability.
        /// </summary>
        /// <param name="type">The clr type.</param>
        /// <param name="model">The Edm model.</param>
        /// <param name="nullable">
        /// Whether the resulting type reference should be marked nullable. For <c>Nullable&lt;T&gt;</c>
        /// inputs the reference is always nullable regardless of this argument.
        /// </param>
        /// <returns>The Edm type reference.</returns>
        public static IEdmTypeReference GetTypeReference(this Type type, IEdmModel model, bool nullable)
        {
            if (type is null || model is null)
            {
                return null;
            }

            if (type.TryGetElementType(out var elementType))
            {
                return EdmCoreModel.GetCollection(GetTypeReference(elementType, model, nullable));
            }

            // Nullable<T> implies a nullable reference no matter what the caller passed.
            var effectiveNullable = nullable || Nullable.GetUnderlyingType(type) is not null;

            var edmType = model.FindDeclaredType(type.FullName);

            if (edmType is IEdmEnumType enumType)
            {
                return new EdmEnumTypeReference(enumType, effectiveNullable);
            }

            if (edmType is IEdmComplexType complexType)
            {
                return new EdmComplexTypeReference(complexType, effectiveNullable);
            }

            if (edmType is IEdmEntityType entityType)
            {
                return new EdmEntityTypeReference(entityType, effectiveNullable);
            }

            return type.GetPrimitiveTypeReference(effectiveNullable);
        }

        /// <summary>
        /// Ensure that thereis an EntityContainer on the model.
        /// </summary>
        /// <param name="model">TThe <see cref="EdmModel"/>.</param>
        /// <param name="apiType">The type of the api.</param>
        /// <returns>An <see cref="EdmEntityContainer"/> instance.</returns>
        internal static EdmEntityContainer EnsureEntityContainer(this EdmModel model, Type apiType)
        {
            var container = (EdmEntityContainer)model.EntityContainer;
            if (container is null)
            {
                container = new EdmEntityContainer(apiType.Namespace, DefaultEntityContainerName);
                model.AddElement(container);
            }

            return container;
        }

        /// <summary>
        /// Tries to find EntitySets on the model by using a type reference of the elements.
        /// </summary>
        /// <param name="model">The model to use.</param>
        /// <param name="typeReference">The type reference to use.</param>
        /// <returns>An EntitySet if found, null otherwise.</returns>
        internal static IEnumerable<IEdmEntitySet> FindDeclaredEntitySetsByTypeReference(
            this IEdmModel model, IEdmTypeReference typeReference)
        {
            if (!typeReference.TryGetElementTypeReference(out var elementTypeReference))
            {
                elementTypeReference = typeReference;
            }

            if (!elementTypeReference.IsEntity())
            {
                return [];
            }

            return model.EntityContainer.EntitySets()
                .Where(e => e.EntityType.FullTypeName() == elementTypeReference.FullName());
        }

        private static bool TryGetElementTypeReference(
            this IEdmTypeReference typeReference, out IEdmTypeReference elementTypeReference)
        {
            if (!typeReference.IsCollection())
            {
                elementTypeReference = null;
                return false;
            }

            elementTypeReference = typeReference.AsCollection().ElementType();
            return true;
        }

        private static EdmPrimitiveTypeKind? GetPrimitiveTypeKind(Type type, out bool isNullable)
        {
            isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullable)
            {
                type = type.GetGenericArguments()[0];
            }

            if (type == typeof(string))
            {
                return EdmPrimitiveTypeKind.String;
            }

            if (type == typeof(byte[]))
            {
                return EdmPrimitiveTypeKind.Binary;
            }

            if (type == typeof(bool))
            {
                return EdmPrimitiveTypeKind.Boolean;
            }

            if (type == typeof(byte))
            {
                return EdmPrimitiveTypeKind.Byte;
            }

            if (type == typeof(DateTime))
            {
                // TODO GitHubIssue#49 : how to map DateTime's in OData v4?  there is no Edm.DateTime type anymore
                return null;
            }

            if (type == typeof(DateOnly))
            {
                return EdmPrimitiveTypeKind.Date;
            }

            if (type == typeof(DateTimeOffset))
            {
                return EdmPrimitiveTypeKind.DateTimeOffset;
            }

            if (type == typeof(decimal))
            {
                return EdmPrimitiveTypeKind.Decimal;
            }

            if (type == typeof(double))
            {
                return EdmPrimitiveTypeKind.Double;
            }

            if (type == typeof(Guid))
            {
                return EdmPrimitiveTypeKind.Guid;
            }

            if (type == typeof(short))
            {
                return EdmPrimitiveTypeKind.Int16;
            }

            if (type == typeof(int))
            {
                return EdmPrimitiveTypeKind.Int32;
            }

            if (type == typeof(long))
            {
                return EdmPrimitiveTypeKind.Int64;
            }

            if (type == typeof(sbyte))
            {
                return EdmPrimitiveTypeKind.SByte;
            }

            if (type == typeof(float))
            {
                return EdmPrimitiveTypeKind.Single;
            }

            if (type == typeof(TimeOnly))
            {
                return EdmPrimitiveTypeKind.TimeOfDay;
            }

            if (type == typeof(TimeSpan))
            {
                // TODO GitHubIssue#49 : this should really be TimeOfDay,
                // but EdmPrimitiveTypeKind doesn't support that type.
                ////return EdmPrimitiveTypeKind.TimeOfDay;
                return EdmPrimitiveTypeKind.Duration;
            }

            if (type == typeof(void))
            {
                return null;
            }

            if (type == typeof(GeographyPoint)) { return EdmPrimitiveTypeKind.GeographyPoint; }
            if (type == typeof(GeographyLineString)) { return EdmPrimitiveTypeKind.GeographyLineString; }
            if (type == typeof(GeographyPolygon)) { return EdmPrimitiveTypeKind.GeographyPolygon; }
            if (type == typeof(GeographyMultiPoint)) { return EdmPrimitiveTypeKind.GeographyMultiPoint; }
            if (type == typeof(GeographyMultiLineString)) { return EdmPrimitiveTypeKind.GeographyMultiLineString; }
            if (type == typeof(GeographyMultiPolygon)) { return EdmPrimitiveTypeKind.GeographyMultiPolygon; }
            if (type == typeof(GeographyCollection)) { return EdmPrimitiveTypeKind.GeographyCollection; }
            if (type == typeof(Geography)) { return EdmPrimitiveTypeKind.Geography; }
            if (type == typeof(GeometryPoint)) { return EdmPrimitiveTypeKind.GeometryPoint; }
            if (type == typeof(GeometryLineString)) { return EdmPrimitiveTypeKind.GeometryLineString; }
            if (type == typeof(GeometryPolygon)) { return EdmPrimitiveTypeKind.GeometryPolygon; }
            if (type == typeof(GeometryMultiPoint)) { return EdmPrimitiveTypeKind.GeometryMultiPoint; }
            if (type == typeof(GeometryMultiLineString)) { return EdmPrimitiveTypeKind.GeometryMultiLineString; }
            if (type == typeof(GeometryMultiPolygon)) { return EdmPrimitiveTypeKind.GeometryMultiPolygon; }
            if (type == typeof(GeometryCollection)) { return EdmPrimitiveTypeKind.GeometryCollection; }
            if (type == typeof(Geometry)) { return EdmPrimitiveTypeKind.Geometry; }

            throw new NotSupportedException(string.Format(
                CultureInfo.InvariantCulture, Resources.NotSupportedType, type.FullName));
        }
    }
}