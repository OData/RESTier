// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace System
{

    /// <summary>
    /// 
    /// </summary>
    internal static partial class Ensure
    {

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        public static void NotNull<T>([ValidatedNotNull]T? value, string paramName)
            where T : struct
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        public static void NotNull<T>([ValidatedNotNull]T value, string paramName)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        [AttributeUsage(AttributeTargets.Parameter)]
        private sealed class ValidatedNotNullAttribute : Attribute
        {
        }
    }
}
