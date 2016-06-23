// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace System.Linq.Expressions
{
    internal static class ExpressionHelperMethods
    {
        private static MethodInfo selectMethod =
            GenericMethodOf(_ => Queryable.Select(default(IQueryable<int>), i => i));

        private static MethodInfo selectManyMethod =
            GenericMethodOf(_ => Queryable.SelectMany(default(IQueryable<int>), i => default(IQueryable<int>)));

        private static MethodInfo whereMethod =
            GenericMethodOf(_ => Queryable.Where(default(IQueryable<int>), default(Expression<Func<int, bool>>)));

        private static MethodInfo ofTypeMethod =
            GenericMethodOf(_ => Queryable.OfType<int>(default(IQueryable)));

        private static MethodInfo countMethod =
            GenericMethodOf(_ => Queryable.LongCount(default(IQueryable<int>)));

        private static MethodInfo asQueryableMethod = typeof(Queryable).GetMethod(
                            "AsQueryable",
                            BindingFlags.Static | BindingFlags.Public,
                            null,
                            new[] { typeof(IEnumerable<>) },
                            null);

        private static MethodInfo asQueryableMethodGeneric =
            GenericMethodOf(_ => Queryable.AsQueryable<int>(default(IEnumerable<int>)));

        private static MethodInfo enumerableCastMethod = typeof(Enumerable).GetMethod("Cast");

        private static MethodInfo enumerableToArrayMethod = typeof(Enumerable).GetMethod("ToArray");

        private static MethodInfo enumerableToListMethod = typeof(Enumerable).GetMethod("ToList");

        public static MethodInfo QueryableSelectGeneric
        {
            get { return selectMethod; }
        }

        public static MethodInfo QueryableSelectManyGeneric
        {
            get { return selectManyMethod; }
        }

        public static MethodInfo QueryableWhereGeneric
        {
            get { return whereMethod; }
        }

        public static MethodInfo QueryableOfTypeGeneric
        {
            get { return ofTypeMethod; }
        }

        public static MethodInfo QueryableCountGeneric
        {
            get { return countMethod; }
        }

        public static MethodInfo QueryableAsQueryable
        {
            get { return asQueryableMethod; }
        }

        public static MethodInfo QueryableAsQueryableGeneric
        {
            get { return asQueryableMethodGeneric; }
        }

        public static MethodInfo EnumerableCastGeneric
        {
            get { return enumerableCastMethod; }
        }

        public static MethodInfo EnumerableToListGeneric
        {
            get { return enumerableToListMethod; }
        }

        public static MethodInfo EnumerableToArrayGeneric
        {
            get { return enumerableToArrayMethod; }
        }

        private static MethodInfo GenericMethodOf<TReturn>(Expression<Func<object, TReturn>> expression)
        {
            return GenericMethodOf(expression as Expression);
        }

        private static MethodInfo GenericMethodOf(Expression expression)
        {
            LambdaExpression lambdaExpression = expression as LambdaExpression;

            Contract.Assert(expression.NodeType == ExpressionType.Lambda);
            Contract.Assert(lambdaExpression != null);
            Contract.Assert(lambdaExpression.Body.NodeType == ExpressionType.Call);

            return (lambdaExpression.Body as MethodCallExpression).Method.GetGenericMethodDefinition();
        }
    }
}
