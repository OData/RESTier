// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Helper methods to execute expressions.
    /// </summary>
    internal static class ExpressionHelpers
    {
        private const string MethodNameOfQueryTake = "Take";
        private const string MethodNameOfQuerySelect = "Select";
        private const string MethodNameOfQuerySkip = "Skip";
        private const string MethodNameOfQueryWhere = "Where";
        private const string MethodNameOfQueryOrderBy = "OrderBy";
        private const string InterfaceNameISelectExpandWrapper = "ISelectExpandWrapper";
        private const string ExpandClauseReflectedTypeName = "SelectExpandBinder";

        /// <summary>
        /// Executes <see cref="Queryable.Select{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, int, TResult}})"/> using the specified select expression.
        /// </summary>
        /// <param name="query">The <see cref="IQueryable"/>.</param>
        /// <param name="select">The select expression.</param>
        /// <returns>The new <see cref="IQueryable"/>.</returns>
        public static IQueryable Select(IQueryable query, LambdaExpression select)
        {
            var selectMethod =
                ExpressionHelperMethods.QueryableSelectGeneric.MakeGenericMethod(
                    query.ElementType,
                    select.Body.Type);
            return selectMethod.Invoke(null, new object[] { query, select }) as IQueryable;
        }

        /// <summary>
        /// Executes <see cref="Queryable.SelectMany{TSource, TCollection, TResult}(IQueryable{TSource}, Expression{Func{TSource, int, IEnumerable{TCollection}}}, Expression{Func{TSource, TCollection, TResult}})"/> using the specified select expression.
        /// </summary>
        /// <param name="query">The <see cref="IQueryable"/>.</param>
        /// <param name="selectMany">The select expression.</param>
        /// <param name="selectedElementType">The type of the element that select operates on.</param>
        /// <returns>The new <see cref="IQueryable"/>.</returns>
        public static IQueryable SelectMany(IQueryable query, LambdaExpression selectMany, Type selectedElementType)
        {
            var selectManyMethod =
                ExpressionHelperMethods.QueryableSelectManyGeneric.MakeGenericMethod(
                    query.ElementType,
                    selectedElementType);
            return selectManyMethod.Invoke(null, new object[] { query, selectMany }) as IQueryable;
        }

        /// <summary>
        /// Executes <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, int, bool}})"/> using the specified where expression.
        /// </summary>
        /// <param name="query">The <see cref="IQueryable"/>.</param>
        /// <param name="where">The select expression.</param>
        /// <param name="type">The type of the element.</param>
        /// <returns>The new <see cref="IQueryable"/>.</returns>
        public static IQueryable Where(IQueryable query, LambdaExpression where, Type type)
        {
            var whereMethod = ExpressionHelperMethods.QueryableWhereGeneric.MakeGenericMethod(type);
            return whereMethod.Invoke(null, new object[] { query, where }) as IQueryable;
        }

        /// <summary>
        /// Executes <see cref="Queryable.OfType{TResult}(IQueryable)"/> using the specified type.
        /// </summary>
        /// <param name="query">The <see cref="IQueryable"/>.</param>
        /// <param name="type">The desired type of the element.</param>
        /// <returns>The new <see cref="IQueryable"/>.</returns>
        public static IQueryable OfType(IQueryable query, Type type)
        {
            var ofTypeMethod = ExpressionHelperMethods.QueryableOfTypeGeneric.MakeGenericMethod(type);
            return ofTypeMethod.Invoke(null, new object[] { query }) as IQueryable;
        }

        /// <summary>
        /// creates a <see cref="Queryable.Count{TSource}(IQueryable{TSource})"/> expression using the specified <paramref name="elementType"/>.
        /// </summary>
        /// <param name="queryExpression">The query expression to use.</param>
        /// <param name="elementType">The type of the element.</param>
        /// <returns>The expression.</returns>
        public static Expression Count(Expression queryExpression, Type elementType)
        {
            var countMethod = ExpressionHelperMethods.QueryableCountGeneric.MakeGenericMethod(elementType);
            return Expression.Call(countMethod, queryExpression);
        }

        /// <summary>
        /// Get count IQueryable of the elements with $skip/$top ignored.
        /// </summary>
        /// <typeparam name="TElement">The type parameter for IQueryable.</typeparam>
        /// <param name="query">The input query.</param>
        /// <returns>The count IQueryable.</returns>
        public static IQueryable<object> GetCountableQuery<TElement>(IQueryable<TElement> query)
        {
            Ensure.NotNull(query, nameof(query));
            object countQuery = query;
            var expression = query.Expression;

            // This is stripping select, expand and top from input query
            expression = StripQueryMethod(expression, MethodNameOfQueryTake);
            expression = StripQueryMethod(expression, MethodNameOfQuerySkip);
            expression = StripQueryMethod(expression, MethodNameOfQuerySelect);

            if (expression != query.Expression)
            {
                // Don't need orderby for count query
                expression = StripQueryMethod(expression, MethodNameOfQueryOrderBy);

                // If Type is Type<GenericType> to then GenericType will be returned.
                // e.g. if type is SelectAllAndExpand<Namespace.Product>, then Namespace.Product will be returned.
                var elementType = GetSelectExpandElementType(typeof(TElement));

                var method = ExpressionHelperMethods.IQueryProviderCreateQueryGeneric;
                var generic = method.MakeGenericMethod(elementType);
                countQuery = generic.Invoke(query.Provider, new object[] { expression });
            }

            // This means there is no $expand/$skip/$top, return count directly
            return (IQueryable<object>)countQuery;
        }

        /// <summary>
        /// Create am empty Queryable of specified type.
        /// </summary>
        /// <param name="elementType">The element type of IQueryable.</param>
        /// <returns>The empty IQueryable.</returns>
        public static IQueryable CreateEmptyQueryable(Type elementType)
        {
            var constructor = typeof(List<>).MakeGenericType(elementType).GetConstructor(Type.EmptyTypes);
            var instance = constructor.Invoke(Array.Empty<object>());
            var emptyQuerable = ExpressionHelperMethods.QueryableAsQueryable
                .Invoke(null, new object[] { instance }) as IQueryable;
            return emptyQuerable;
        }

        /// <summary>
        /// Gets the item type for an enumberable type.
        /// </summary>
        /// <param name="enumerableType">The enumberable type.</param>
        /// <returns>The item type.</returns>
        internal static Type GetEnumerableItemType(this Type enumerableType)
        {
            var type = enumerableType.FindGenericType(typeof(IEnumerable<>));
            if (type is not null)
            {
                return type.GetGenericArguments()[0];
            }

            return enumerableType;
        }

        /// <summary>
        /// Removes UnneededStatement from a LinQ MethodCallExpression.
        /// </summary>
        /// <param name="methodCallExpression">The methodcall expression.</param>
        /// <returns>A new MethodCallExpression.</returns>
        internal static MethodCallExpression RemoveUnneededStatement(this MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression is null || methodCallExpression.Arguments.Count != 2)
            {
                return methodCallExpression;
            }

            if (methodCallExpression.Method.Name == MethodNameOfQuerySelect)
            {
                // Check where it is expand case or select, if yes, need to get rid of last select
                methodCallExpression = RemoveSelectExpandStatement(methodCallExpression);
                if (methodCallExpression is null || methodCallExpression.Arguments.Count != 2)
                {
                    return methodCallExpression;
                }
            }

            if (methodCallExpression.Method.Name == MethodNameOfQueryTake)
            {
                // Check where it is top query option, and if yes, remove it.
                methodCallExpression = methodCallExpression.Arguments[0] as MethodCallExpression;
                if (methodCallExpression is null || methodCallExpression.Arguments.Count != 2)
                {
                    return methodCallExpression;
                }
            }

            if (methodCallExpression.Method.Name == MethodNameOfQuerySkip)
            {
                // Check where it is skip query option, and if yes, remove it.
                methodCallExpression = methodCallExpression.Arguments[0] as MethodCallExpression;
                if (methodCallExpression is null || methodCallExpression.Arguments.Count != 2)
                {
                    return methodCallExpression;
                }
            }

            if (methodCallExpression.Method.Name == MethodNameOfQueryOrderBy)
            {
                // Check where it is orderby query option, and if yes, remove it.
                methodCallExpression = methodCallExpression.Arguments[0] as MethodCallExpression;
                if (methodCallExpression is null || methodCallExpression.Arguments.Count != 2)
                {
                    return methodCallExpression;
                }
            }

            return methodCallExpression;
        }

        /// <summary>
        /// Removes the Select call after an Expand Statement.
        /// </summary>
        /// <param name="methodCallExpression">The methodcall expression.</param>
        /// <returns>A new MethodCallExpression.</returns>
        internal static MethodCallExpression RemoveSelectExpandStatement(this MethodCallExpression methodCallExpression)
        {
            // This means a select for expand is appended, will remove it for resource existing check
            var expandSelect = methodCallExpression.Arguments[1] as UnaryExpression;
            if (!(expandSelect.Operand is LambdaExpression lambdaExpression))
            {
                return methodCallExpression;
            }

            if (!(lambdaExpression.Body is MemberInitExpression memberInitExpression))
            {
                return methodCallExpression;
            }

            var returnType = lambdaExpression.ReturnType;
            var wrapperInterface = returnType.GetInterface(InterfaceNameISelectExpandWrapper);
            if (wrapperInterface is not null)
            {
                methodCallExpression = methodCallExpression.Arguments[0] as MethodCallExpression;
            }

            return methodCallExpression;
        }

        /// <summary>
        /// Removes a where statement that checks a property for non-null.
        /// </summary>
        /// <param name="expression">The methodcall expression.</param>
        /// <returns>A new Expression.</returns>
        internal static Expression RemoveAppendWhereStatement(this Expression expression)
        {
            if (!(expression is MethodCallExpression methodCallExpression) || methodCallExpression.Method.Name != MethodNameOfQueryWhere)
            {
                return expression;
            }

            // This means there may be an appended statement Where(Param_0 => (Param_0.Prop is not null))
            var appendedWhere = methodCallExpression.Arguments[1] as UnaryExpression;
            if (!(appendedWhere.Operand is LambdaExpression lambdaExpression))
            {
                return expression;
            }

            if (lambdaExpression.Body is BinaryExpression binaryExpression && binaryExpression.NodeType == ExpressionType.NotEqual)
            {
                if (binaryExpression.Right is ConstantExpression rightExpression && rightExpression.Value is null)
                {
                    // remove statement like Where(Param_0 => (Param_0.Prop is not null))
                    expression = methodCallExpression.Arguments[0];
                }
            }

            return expression;
        }

        private static Expression StripQueryMethod(Expression expression, string methodName)
        {
            if (expression is MethodCallExpression methodCall &&
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
            if (typeInfo.IsGenericType && typeInfo.ReflectedType is not null
                && typeInfo.ReflectedType.Name == ExpandClauseReflectedTypeName)
            {
                elementType = typeInfo.GenericTypeArguments[0];
            }

            return elementType;
        }
    }
}
