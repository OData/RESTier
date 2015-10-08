// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

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

        private static MethodInfo countMethod =
            GenericMethodOf(_ => Queryable.Count(default(IQueryable<int>)));

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

        public static MethodInfo QueryableCountGeneric
        {
            get { return countMethod; }
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
