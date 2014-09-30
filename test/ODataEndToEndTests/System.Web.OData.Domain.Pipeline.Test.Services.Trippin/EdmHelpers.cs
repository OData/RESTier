// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;

namespace Microsoft.OData.Edm
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