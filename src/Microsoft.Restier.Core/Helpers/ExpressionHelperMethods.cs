// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace System.Linq.Expressions
{
    internal static class ExpressionHelperMethods
    {
        private const string MethodNameOfCreateQuery = "CreateQuery";
        private const string MethodNameOfAsQueryable = "AsQueryable";

        public static MethodInfo QueryableSelectGeneric { get; } = GenericMethodOf(_ => Queryable.Select(default(IQueryable<int>), i => i));

        public static MethodInfo QueryableSelectManyGeneric { get; } = GenericMethodOf(_ => Queryable.SelectMany(default(IQueryable<int>), i => default(IQueryable<int>)));

        public static MethodInfo QueryableWhereGeneric { get; } = GenericMethodOf(_ => Queryable.Where(default, default(Expression<Func<int, bool>>)));

        public static MethodInfo QueryableOfTypeGeneric { get; } = GenericMethodOf(_ => Queryable.OfType<int>(default(IQueryable)));

        public static MethodInfo QueryableCountGeneric { get; } = GenericMethodOf(_ => Queryable.LongCount(default(IQueryable<int>)));

        public static MethodInfo QueryableAsQueryable { get; } = typeof(Queryable).GetMethod(
                            MethodNameOfAsQueryable,
                            BindingFlags.Static | BindingFlags.Public,
                            null,
                            new[] { typeof(IEnumerable<>) },
                            null);

        public static MethodInfo QueryableAsQueryableGeneric { get; } = GenericMethodOf(_ => Queryable.AsQueryable<int>(default(IEnumerable<int>)));

        public static MethodInfo EnumerableCastGeneric { get; } = typeof(Enumerable).GetMethod("Cast");

        public static MethodInfo EnumerableToListGeneric { get; } = typeof(Enumerable).GetMethod("ToList");

        public static MethodInfo EnumerableToArrayGeneric { get; } = typeof(Enumerable).GetMethod("ToArray");

        private static MethodInfo GenericMethodOf<TReturn>(Expression<Func<object, TReturn>> expression) => GenericMethodOf(expression as Expression);

        public static MethodInfo IQueryProviderCreateQueryGeneric { get; } = typeof(IQueryProvider).GetMethods()
            .Single(_ => _.Name == MethodNameOfCreateQuery && _.IsGenericMethodDefinition);

        private static MethodInfo GenericMethodOf(Expression expression)
        {
            var lambdaExpression = expression as LambdaExpression;

            Contract.Assert(expression.NodeType == ExpressionType.Lambda);
            Contract.Assert(lambdaExpression != null);
            Contract.Assert(lambdaExpression.Body.NodeType == ExpressionType.Call);

            return (lambdaExpression.Body as MethodCallExpression).Method.GetGenericMethodDefinition();
        }
    }
}
