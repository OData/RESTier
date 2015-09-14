// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Shared
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
            else if (type == typeof(byte[]))
            {
                return EdmPrimitiveTypeKind.Binary;
            }
            else if (type == typeof(bool))
            {
                return EdmPrimitiveTypeKind.Boolean;
            }
            else if (type == typeof(byte))
            {
                return EdmPrimitiveTypeKind.Byte;
            }
            else if (type == typeof(DateTime))
            {
                // TODO GitHubIssue#49 : how to map DateTime's in OData v4?  there is no Edm.DateTime type anymore
                return null;
            }
            else if (type == typeof(DateTimeOffset))
            {
                return EdmPrimitiveTypeKind.DateTimeOffset;
            }
            else if (type == typeof(decimal))
            {
                return EdmPrimitiveTypeKind.Decimal;
            }
            else if (type == typeof(double))
            {
                return EdmPrimitiveTypeKind.Double;
            }
            else if (type == typeof(Guid))
            {
                return EdmPrimitiveTypeKind.Guid;
            }
            else if (type == typeof(short))
            {
                return EdmPrimitiveTypeKind.Int16;
            }
            else if (type == typeof(int))
            {
                return EdmPrimitiveTypeKind.Int32;
            }
            else if (type == typeof(long))
            {
                return EdmPrimitiveTypeKind.Int64;
            }
            else if (type == typeof(sbyte))
            {
                return EdmPrimitiveTypeKind.SByte;
            }
            else if (type == typeof(float))
            {
                return EdmPrimitiveTypeKind.Single;
            }
            else if (type == typeof(TimeSpan))
            {
                // TODO GitHubIssue#49 : this should really be TimeOfDay,
                // but EdmPrimitiveTypeKind doesn't support that type.
                ////return EdmPrimitiveTypeKind.TimeOfDay;
                return EdmPrimitiveTypeKind.Duration;
            }
            else if (type == typeof(void))
            {
                return null;
            }

            throw new NotSupportedException("not supported type: " + type.FullName);
        }
    }
}