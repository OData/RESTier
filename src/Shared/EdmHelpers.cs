// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.OData.Edm
{
    internal static class EdmHelpers
    {
        private const string DefaultEntityContainerName = "DefaultContainer";

        public static EdmEntityContainer EnsureEntityContainer(this EdmModel model, Type apiType)
        {
            var container = (EdmEntityContainer)model.EntityContainer;
            if (container == null)
            {
                container = new EdmEntityContainer(apiType.Namespace, DefaultEntityContainerName);
                model.AddElement(container);
            }

            return container;
        }

        public static IEdmEntitySet FindDeclaredEntitySetByTypeReference(
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