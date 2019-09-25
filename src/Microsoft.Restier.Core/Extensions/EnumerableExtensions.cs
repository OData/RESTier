// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace System.Collections
{
    internal static class EnumerableExtensions
    {
        public static object SingleOrDefault(this IEnumerable enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            var result = enumerator.MoveNext() ? enumerator.Current : null;

            if (enumerator.MoveNext())
            {
                throw new InvalidOperationException(Microsoft.Restier.Core.Resources.QueryShouldGetSingleRecord);
            }

            return result;
        }
    }
}
