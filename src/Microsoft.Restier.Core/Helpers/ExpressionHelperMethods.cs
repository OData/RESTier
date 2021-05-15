// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Helper methods for Expressions.
    /// </summary>
    internal static class ExpressionHelperMethods
    {
        private const string MethodNameOfCreateQuery = "CreateQuery";
        private const string MethodNameOfAsQueryable = "AsQueryable";

        /// <summary>
        /// Gets the MethodInfo for <see cref="Queryable.Select{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, int, TResult}})"/>.
        /// </summary>
        public static MethodInfo QueryableSelectGeneric { get; } = GenericMethodOf(_ => Queryable.Select(default(IQueryable<int>), i => i));

        /// <summary>
        /// Gets the MethodInfo for <see cref="Queryable.SelectMany{TSource, TCollection, TResult}(IQueryable{TSource}, Expression{Func{TSource, int, IEnumerable{TCollection}}}, Expression{Func{TSource, TCollection, TResult}})"/>.
        /// </summary>
        public static MethodInfo QueryableSelectManyGeneric { get; } = GenericMethodOf(_ => Queryable.SelectMany(default(IQueryable<int>), i => default(IQueryable<int>)));

        /// <summary>
        /// Gets the MethodInfo for <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, int, bool}})"/>.
        /// </summary>
        public static MethodInfo QueryableWhereGeneric { get; } = GenericMethodOf(_ => Queryable.Where(default, default(Expression<Func<int, bool>>)));

        /// <summary>
        /// Gets the MethodInfo for <see cref="Queryable.OfType{TResult}(IQueryable)"/>.
        /// </summary>
        public static MethodInfo QueryableOfTypeGeneric { get; } = GenericMethodOf(_ => Queryable.OfType<int>(default(IQueryable)));

        /// <summary>
        /// Gets the MethodInfo for <see cref="Queryable.LongCount{TSource}(IQueryable{TSource})"/>.
        /// </summary>
        public static MethodInfo QueryableCountGeneric { get; } = GenericMethodOf(_ => Queryable.LongCount(default(IQueryable<int>)));

        /// <summary>
        /// Gets the methodInfo for <see cref="Queryable.AsQueryable{TElement}(IEnumerable{TElement})"/>.
        /// </summary>
        public static MethodInfo QueryableAsQueryable { get; } = typeof(Queryable).GetMethod(
                            MethodNameOfAsQueryable,
                            BindingFlags.Static | BindingFlags.Public,
                            null,
                            new[] { typeof(IEnumerable<>) },
                            null);

        /// <summary>
        /// Gets the MethodInfo for <see cref="Queryable.AsQueryable{TElement}(IEnumerable{TElement})"/>.
        /// </summary>
        public static MethodInfo QueryableAsQueryableGeneric { get; } = GenericMethodOf(_ => Queryable.AsQueryable<int>(default));

        /// <summary>
        /// Gets the MethodInfo for <see cref="Enumerable.Cast{TResult}(Collections.IEnumerable)"/>.
        /// </summary>
        public static MethodInfo EnumerableCastGeneric { get; } = typeof(Enumerable).GetMethod("Cast");

        /// <summary>
        /// Gets the MethodInfo for <see cref="Enumerable.ToList{TSource}(IEnumerable{TSource})"/>.
        /// </summary>
        public static MethodInfo EnumerableToListGeneric { get; } = typeof(Enumerable).GetMethod("ToList");

        /// <summary>
        /// Gets the MethodInfo for <see cref="Enumerable.ToArray{TSource}(IEnumerable{TSource})"/>.
        /// </summary>
        public static MethodInfo EnumerableToArrayGeneric { get; } = typeof(Enumerable).GetMethod("ToArray");

        /// <summary>
        /// Gets the MethodInfo for <see cref="IQueryProvider.CreateQuery{TElement}(Expression)"/>.
        /// </summary>
        public static MethodInfo IQueryProviderCreateQueryGeneric { get; } = typeof(IQueryProvider).GetMethods()
            .Single(_ => _.Name == MethodNameOfCreateQuery && _.IsGenericMethodDefinition);

        /// <summary>
        /// Gets the generic method of a lambda expression.
        /// </summary>
        /// <param name="expression">The lamda expression to use.</param>
        /// <returns>The Method info of the generic method.</returns>
        private static MethodInfo GenericMethodOf<TReturn>(Expression<Func<object, TReturn>> expression) => GenericMethodOf(expression as Expression);

        /// <summary>
        /// Gets the generic method of a lambda expression.
        /// </summary>
        /// <param name="expression">The lamda expression to use.</param>
        /// <returns>The Method info of the generic method.</returns>
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
