// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System.Linq.Expressions
{
    internal static class ExpressionHelpers
    {
        public static IQueryable Select(IQueryable query, LambdaExpression select)
        {
            MethodInfo selectMethod = ExpressionHelperMethods.QueryableSelectGeneric.MakeGenericMethod(query.ElementType, select.Body.Type);
            return selectMethod.Invoke(null, new object[] { query, select }) as IQueryable;
        }

        public static IQueryable SelectMany(IQueryable query, LambdaExpression selectMany, Type selectedElementType)
        {
            MethodInfo selectManyMethod = ExpressionHelperMethods.QueryableSelectManyGeneric.MakeGenericMethod(query.ElementType, selectedElementType);
            return selectManyMethod.Invoke(null, new object[] { query, selectMany }) as IQueryable;
        }

        public static IQueryable Where(IQueryable query, LambdaExpression where, Type type)
        {
            MethodInfo whereMethod = ExpressionHelperMethods.QueryableWhereGeneric.MakeGenericMethod(type);
            return whereMethod.Invoke(null, new object[] { query, where }) as IQueryable;
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
    }
}
