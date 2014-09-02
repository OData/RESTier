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
                    "A query for a single entity resulted in more than one record.");
            }

            return result;
        }
    }
}
