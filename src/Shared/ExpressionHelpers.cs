// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
