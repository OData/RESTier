// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace System
{
    internal static partial class Ensure
    {
        public static void NotNull<T>([ValidatedNotNull]T? value, string paramName)
            where T : struct
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void NotNull<T>([ValidatedNotNull]T value, string paramName)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        private sealed class ValidatedNotNullAttribute : Attribute
        {
        }
    }
}
