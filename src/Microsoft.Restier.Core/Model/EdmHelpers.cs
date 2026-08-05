// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Validation;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// An utility class to operate with Edm model.
    /// </summary>
    internal static class EdmHelpers
    {
        /// <summary>
        /// Converts an Edm Type to Edm type reference.
        /// </summary>
        /// <param name="edmType">The Edm type.</param>
        /// <param name="isNullable">Nullable value.</param>
        /// <returns>The Edm type reference.</returns>
        internal static IEdmTypeReference ToEdmTypeReference(this IEdmType edmType, bool isNullable = false)
        {
            Ensure.NotNull(edmType, nameof(edmType));

            switch (edmType.TypeKind)
            {
                case EdmTypeKind.Collection:
                    return new EdmCollectionTypeReference((IEdmCollectionType)edmType);

                case EdmTypeKind.Complex:
                    return new EdmComplexTypeReference((IEdmComplexType)edmType, isNullable);

                case EdmTypeKind.Entity:
                    return new EdmEntityTypeReference((IEdmEntityType)edmType, isNullable);

                case EdmTypeKind.EntityReference:
                    return new EdmEntityReferenceTypeReference((IEdmEntityReferenceType)edmType, isNullable);

                case EdmTypeKind.Enum:
                    return new EdmEnumTypeReference((IEdmEnumType)edmType, isNullable);

                case EdmTypeKind.Primitive:
                    return EdmCoreModel.Instance.GetPrimitive(((IEdmPrimitiveType)edmType).PrimitiveKind, isNullable);

                case EdmTypeKind.Path:
                    return new EdmPathTypeReference((IEdmPathType)edmType, isNullable);

                case EdmTypeKind.TypeDefinition:
                    return new EdmTypeDefinitionReference((IEdmTypeDefinition)edmType, isNullable);

                default:
                    var message = string.Format(CultureInfo.CurrentCulture, Resources.EdmTypeNotSupported, edmType.ToTraceString());
                    throw new NotSupportedException(message);
            }
        }


        /// <summary>
        /// Converts the <see cref="IEdmType"/> to <see cref="IEdmCollectionType"/>.
        /// </summary>
        /// <param name="edmType">The given Edm type.</param>
        /// <param name="isNullable">Nullable or not.</param>
        /// <returns>The collection type.</returns>
        internal static IEdmCollectionType ToCollection(this IEdmType edmType, bool isNullable)
        {
            Ensure.NotNull(edmType, nameof(edmType));
            return new EdmCollectionType(edmType.ToEdmTypeReference(isNullable));
        }

        internal static IEdmEntitySetBase GetTargetEntitySet(this IEdmOperation operation, IEdmNavigationSource source, IEdmModel model)
        {
            if (source == null)
            {
                return null;
            }

            if (operation.IsBound && operation.Parameters.Any())
            {
                IEdmOperationParameter parameter;
                Dictionary<IEdmNavigationProperty, IEdmPathExpression> path;
                IEdmEntityType lastEntityType;

                if (operation.TryGetRelativeEntitySetPath(model, out parameter, out path, out lastEntityType, out IEnumerable<EdmError> _))
                {
                    IEdmNavigationSource target = source;

                    foreach (var navigation in path)
                    {
                        target = target.FindNavigationTarget(navigation.Key, navigation.Value);
                    }

                    return target as IEdmEntitySetBase;
                }
            }

            return null;
        }
    }
}
