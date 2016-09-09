// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Web.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Publishers.OData.Properties;

namespace Microsoft.Restier.Publishers.OData.Model
{
    /// <summary>
    /// This class contains some common extension methods for Edm
    /// </summary>
    public static class EdmHelpers
    {
        private const string DefaultEntityContainerName = "DefaultContainer";

        /// <summary>
        /// The type to get the primitive type reference
        /// </summary>
        /// <param name="type">The clr type to get edm type reference</param>
        /// <returns>The edm type reference for the clr type</returns>
        public static EdmTypeReference GetPrimitiveTypeReference(this Type type)
        {
            // Only handle primitive type right now
            bool isNullable;
            EdmPrimitiveTypeKind? primitiveTypeKind = EdmHelpers.GetPrimitiveTypeKind(type, out isNullable);

            if (!primitiveTypeKind.HasValue)
            {
                return null;
            }

            return new EdmPrimitiveTypeReference(
                EdmCoreModel.Instance.GetPrimitiveType(primitiveTypeKind.Value),
                isNullable);
        }

        /// <summary>
        /// Get the clr type for a specified edm type
        /// </summary>
        /// <param name="edmType">The edm type to get clr type</param>
        /// <param name="serviceProvider">The provider to get service from DI container</param>
        /// <returns>The clr type</returns>
        public static Type GetClrType(this IEdmType edmType, IServiceProvider serviceProvider)
        {
            IEdmModel edmModel = serviceProvider.GetService<IEdmModel>();

            ClrTypeAnnotation annotation = edmModel.GetAnnotationValue<ClrTypeAnnotation>(edmType);
            if (annotation != null)
            {
                return annotation.ClrType;
            }

            throw new NotSupportedException(string.Format(
                CultureInfo.InvariantCulture,
                Resources.ElementTypeNotFound,
                edmType.FullTypeName()));
        }

        /// <summary>
        /// Get the edm type reference for a clr type
        /// </summary>
        /// <param name="type">The clr type</param>
        /// <param name="model">The Edm model</param>
        /// <returns>The Edm type reference</returns>
        public static IEdmTypeReference GetTypeReference(this Type type, IEdmModel model)
        {
            if (type == null || model == null)
            {
                return null;
            }

            Type elementType;
            if (type.TryGetElementType(out elementType))
            {
                return EdmCoreModel.GetCollection(GetTypeReference(elementType, model));
            }

            var edmType = model.FindDeclaredType(type.FullName);

            var enumType = edmType as IEdmEnumType;
            if (enumType != null)
            {
                return new EdmEnumTypeReference(enumType, true);
            }

            var complexType = edmType as IEdmComplexType;
            if (complexType != null)
            {
                return new EdmComplexTypeReference(complexType, true);
            }

            var entityType = edmType as IEdmEntityType;
            if (entityType != null)
            {
                return new EdmEntityTypeReference(entityType, true);
            }

            return type.GetPrimitiveTypeReference();
        }

        internal static EdmEntityContainer EnsureEntityContainer(this EdmModel model, Type apiType)
        {
            var container = (EdmEntityContainer)model.EntityContainer;
            if (container == null)
            {
                container = new EdmEntityContainer(apiType.Namespace, DefaultEntityContainerName);
                model.AddElement(container);
            }

            return container;
        }

        internal static IEdmEntitySet FindDeclaredEntitySetByTypeReference(
            this IEdmModel model, IEdmTypeReference typeReference)
        {
            IEdmTypeReference elementTypeReference;
            if (!typeReference.TryGetElementTypeReference(out elementTypeReference))
            {
                elementTypeReference = typeReference;
            }

            if (!elementTypeReference.IsEntity())
            {
                return null;
            }

            return model.EntityContainer.EntitySets()
                .SingleOrDefault(e => e.EntityType().FullTypeName() == elementTypeReference.FullName());
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

            throw new NotSupportedException(string.Format(
                CultureInfo.InvariantCulture, Resources.NotSupportedType, type.FullName));
        }
    }
}