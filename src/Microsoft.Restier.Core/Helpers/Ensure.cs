// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace System
{
    /// <summary>
    /// Ensures that values of parameters are not null.
    /// </summary>
    internal static partial class Ensure
    {
        /// <summary>
        /// Ensures that a value of a parameter is not null.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="paramName">The name of the parameter.</param>
        public static void NotNull<T>([ValidatedNotNull] T? value, string paramName)
            where T : struct
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Ensures that a value of a parameter is not null.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="paramName">The name of the parameter.</param>
        public static void NotNull<T>([ValidatedNotNull] T value, string paramName)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Ensures that a value of a parameter is not null or white space.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="paramName">The name of the parameter.</param>
        public static void NotNullOrWhiteSpace([ValidatedNotNull] string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(paramName);
            }
        }

        [AttributeUsage(AttributeTargets.Parameter)]
        private sealed class ValidatedNotNullAttribute : Attribute
        {
        }
    }
}
