// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions
{
    internal static class ExpressionHelpers
    {
        private const string MethodNameOfCreateQuery = "CreateQuery";
        private const string MethodNameOfQueryTake = "Take";
        private const string MethodNameOfQuerySelect = "Select";
        private const string MethodNameOfQuerySkip = "Skip";
        private const string ExpandClauseReflectedTypeName = "SelectExpandBinder";

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
        /// Get count IQueryable of the elements with $skip/$top ignored
        /// </summary>
        /// <typeparam name="TElement">The type parameter for IQueryable</typeparam>
        /// <param name="query">The input query.</param>
        /// <returns>The count IQueryable</returns>
        public static IQueryable<object> GetCountableQuery<TElement>(
           IQueryable<TElement> query)
        {
            Ensure.NotNull(query, "query");
            object countQuery = query;
            var expression = query.Expression;

            // This is stripping select of expand and top select method is Source
            expression = StripQueryMethod(expression, MethodNameOfQuerySelect);
            expression = StripQueryMethod(expression, MethodNameOfQueryTake);
            expression = StripQueryMethod(expression, MethodNameOfQuerySkip);

            if (expression != query.Expression)
            {
                // If Type is Type<GenericType> to then GenericType will be returned.
                // e.g. if type is SelectAllAndExpand<Namespace.Product>, then Namespace.Product will be returned.
                Type elementType = GetSelectExpandElementType(typeof(TElement));

                // Create IQueryable with target type, the type is not passed in TElement but new retrieved elementType
                Type thisType = query.Provider.GetType();

                // Get the CreateQuery method information who accepts generic type
                MethodInfo method = thisType.GetMethods()
                    .Single(m => m.Name == MethodNameOfCreateQuery && m.IsGenericMethodDefinition);

                // Replace method generic type with specified type.
                MethodInfo generic = method.MakeGenericMethod(elementType);
                countQuery = generic.Invoke(query.Provider, new object[] { expression });
            }

            // This means there is no $expand/$skip/$top, return count directly
            return (IQueryable<object>)countQuery;
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

        private static Type GetSelectExpandElementType(Type elementType)
        {
            // Get the generic type of a type. e.g. if type is SelectAllAndExpand<Namespace.Product>,
            // then type Namespace.Product will be returned.
            // Only generic type of expand clause will be retrieved to make the logic specified for $expand
            var typeInfo = elementType.GetTypeInfo();
            if (typeInfo.IsGenericType && typeInfo.ReflectedType != null
                && typeInfo.ReflectedType.Name == ExpandClauseReflectedTypeName)
            {
                elementType = typeInfo.GenericTypeArguments[0];
            }

            return elementType;
        }
    }
}
