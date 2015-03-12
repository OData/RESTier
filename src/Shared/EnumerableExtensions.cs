// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace System.Collections
{
    internal static class EnumerableExtensions
    {
        public static object SingleOrDefault(this IEnumerable enumerable)
        {
            IEnumerator enumerator = enumerable.GetEnumerator();
            object result = enumerator.MoveNext() ? enumerator.Current : null;

            if (enumerator.MoveNext())
            {
                throw new InvalidOperationException(
                    Microsoft.Restier.Shared.SharedResources.QueryShouldGetSingleRecord);
            }

            return result;
        }
    }
}
