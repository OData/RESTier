// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin
{
    internal static class EdmHelpers
    {
        public static EdmPrimitiveTypeKind? GetPrimitiveTypeKind(Type type, out bool isNullable)
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
                // TODO: how to map DateTime's in OData v4?  there is no Edm.DateTime type anymore
                return null;
            }
            if (type == typeof(DateTimeOffset))
            {
                return EdmPrimitiveTypeKind.DateTimeOffset;
            }
            if (type == typeof(Decimal))
            {
                return EdmPrimitiveTypeKind.Decimal;
            }
            if (type == typeof(Double))
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
                return EdmPrimitiveTypeKind.Duration;
                // TODO: this should really be TimeOfDay, but EdmPrimitiveTypeKind doesn't support that type
                // return EdmPrimitiveTypeKind.TimeOfDay;
            }
            if (type == typeof(void))
            {
                return null;
            }

            throw new Exception("not supported type: " + type.FullName);
        }
    }
}