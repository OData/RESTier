// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions
{
    internal static class ExpressionHelpers
    {
        private const string MethodNameOfQueryTake = "Take";
        private const string MethodNameOfQuerySkip = "Skip";

        public static IQueryable Select(IQueryable query, LambdaExpression select)
        {
            MethodInfo selectMethod =
                ExpressionHelperMethods.QueryableSelectGeneric.MakeGenericMethod(
                    query.ElementType,
                    select.Body.Type);
            return selectMethod.Invoke(null, new object[] { query, select }) as IQueryable;
        }

        public static IQueryable SelectMany(IQueryable query, LambdaExpression selectMany, Type selectedElementType)
        {
            MethodInfo selectManyMethod =
                ExpressionHelperMethods.QueryableSelectManyGeneric.MakeGenericMethod(
                    query.ElementType,
                    selectedElementType);
            return selectManyMethod.Invoke(null, new object[] { query, selectMany }) as IQueryable;
        }

        public static IQueryable Where(IQueryable query, LambdaExpression where, Type type)
        {
            MethodInfo whereMethod = ExpressionHelperMethods.QueryableWhereGeneric.MakeGenericMethod(type);
            return whereMethod.Invoke(null, new object[] { query, where }) as IQueryable;
        }

        public static Expression Count(Expression queryExpression, Type elementType)
        {
            MethodInfo countMethod =
                ExpressionHelperMethods.QueryableCountGeneric.MakeGenericMethod(elementType);
            return Expression.Call(countMethod, queryExpression);
        }

        /// <summary>
        /// Remove paging methods for given IQueryable
        /// </summary>
        /// <typeparam name="TElement">The type parameter for IQueryable</typeparam>
        /// <param name="query">The input query.</param>
        /// <returns>The proceed query.</returns>
        public static IQueryable<TElement> StripPagingOperators<TElement>(
           IQueryable<TElement> query)
        {
            Ensure.NotNull(query, "query");
            var expression = query.Expression;
            expression = StripQueryMethod(expression, MethodNameOfQueryTake);
            expression = StripQueryMethod(expression, MethodNameOfQuerySkip);
            if (expression != query.Expression)
            {
                query = query.Provider.CreateQuery<TElement>(expression);
            }

            return query;
        }

        internal static Type GetEnumerableItemType(this Type enumerableType)
        {
            Type type = enumerableType.FindGenericType(typeof(IEnumerable<>));
            if (type != null)
            {
                return type.GetGenericArguments()[0];
            }

            return enumerableType;
        }

        private static Expression StripQueryMethod(Expression expression, string methodName)
        {
            var methodCall = expression as MethodCallExpression;
            if (methodCall != null &&
                methodCall.Method.DeclaringType == typeof(Queryable) &&
                methodCall.Method.Name.Equals(methodName, StringComparison.Ordinal))
            {
                expression = methodCall.Arguments[0];
            }

            return expression;
        }
    }
}
