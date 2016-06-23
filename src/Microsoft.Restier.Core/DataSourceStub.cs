// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents method stubs that identify API data source.
    /// </summary>
    /// <remarks>
    /// The methods in this class are stubs that identify API data source
    /// inside a query expression. This is a generic way to reference a
    /// data source in API. Later in the query pipeline the sourcer from
    /// the data provider will replace the stub with the actual data source.
    /// </remarks>
    public static class DataSourceStub
    {
        /// <summary>
        /// Identifies an entity set, singleton or queryable data
        /// resulting from a call to a composable function import.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable data.
        /// </typeparam>
        /// <param name="name">
        /// The name of an entity set, singleton or composable function import.
        /// </param>
        /// <param name="arguments">
        /// If <paramref name="name"/> is a composable function import,
        /// the arguments to be passed to the composable function import.
        /// </param>
        /// <returns>
        /// A representation of the entity set, singleton or queryable
        /// data resulting from a call to the composable function import.
        /// </returns>
        public static IQueryable<TElement> GetQueryableSource<TElement>(
            string name, params object[] arguments)
        {
            throw new InvalidOperationException(Resources.DoNotCallDataSourceStubMethodDirectly);
        }

        /// <summary>
        /// Identifies queryable data resulting
        /// from a call to a composable function.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable data.
        /// </typeparam>
        /// <param name="namespaceName">
        /// The name of a namespace containing the composable function.
        /// </param>
        /// <param name="name">
        /// The name of a composable function.
        /// </param>
        /// <param name="arguments">
        /// The arguments to be passed to the composable function.
        /// </param>
        /// <returns>
        /// A representation of the queryable data resulting
        /// from a call to the composable function.
        /// </returns>
        public static IQueryable<TElement> GetQueryableSource<TElement>(
            string namespaceName, string name, params object[] arguments)
        {
            throw new InvalidOperationException(Resources.DoNotCallDataSourceStubMethodDirectly);
        }

        /// <summary>
        /// Identifies the value of an extended property of an object.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result.
        /// </typeparam>
        /// <param name="source">
        /// A source object.
        /// </param>
        /// <param name="propertyName">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// A representation of the value of the
        /// extended property of the object.
        /// </returns>
        public static TResult GetPropertyValue<TResult>(
            object source, string propertyName)
        {
            throw new InvalidOperationException(Resources.DoNotCallDataSourceStubMethodDirectly);
        }

        /// <summary>
        /// Identifies an entity set or results of a call to a function import.
        /// TODO reserve for function/action supports
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the results.
        /// </typeparam>
        /// <param name="name">
        /// The name of an entity set or function import.
        /// </param>
        /// <param name="arguments">
        /// If <paramref name="name"/> is a function import,
        /// the arguments to be passed to the function import.
        /// </param>
        /// <returns>
        /// A representation of the entity set or
        /// results of a call to the function import.
        /// </returns>
        internal static IEnumerable<TElement> Results<TElement>(
            string name, params object[] arguments)
        {
            throw new InvalidOperationException(Resources.DoNotCallDataSourceStubMethodDirectly);
        }

        /// <summary>
        /// Identifies a singleton or result of a
        /// call to a singular function import.
        /// TODO reserve for function/action supports
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result.
        /// </typeparam>
        /// <param name="name">
        /// The name of a singleton or singular function import.
        /// </param>
        /// <param name="arguments">
        /// If <paramref name="name"/> is a singular function import,
        /// the arguments to be passed to the singular function import.
        /// </param>
        /// <returns>
        /// A representation of the singleton or result
        /// of a call to the singular function import.
        /// </returns>
        internal static TResult Result<TResult>(
            string name, params object[] arguments)
        {
            throw new InvalidOperationException(Resources.DoNotCallDataSourceStubMethodDirectly);
        }

        /// <summary>
        /// Identifies the results of a call to a function.
        /// TODO reserve for function/action supports
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the results.
        /// </typeparam>
        /// <param name="namespaceName">
        /// The name of a namespace containing the function.
        /// </param>
        /// <param name="name">
        /// The name of a function.
        /// </param>
        /// <param name="arguments">
        /// The arguments to be passed to the function.
        /// </param>
        /// <returns>
        /// A representation of the results of a call to the function.
        /// </returns>
        internal static IEnumerable<TElement> Results<TElement>(
            string namespaceName, string name, params object[] arguments)
        {
            throw new InvalidOperationException(Resources.DoNotCallDataSourceStubMethodDirectly);
        }

        /// <summary>
        /// Identifies the result of a call to a singular function.
        /// TODO reserve for function/action supports
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result.
        /// </typeparam>
        /// <param name="namespaceName">
        /// The name of a namespace containing the singular function.
        /// </param>
        /// <param name="name">
        /// The name of a singular function.
        /// </param>
        /// <param name="arguments">
        /// The arguments to be passed to the singular function.
        /// </param>
        /// <returns>
        /// A representation of the result of a call to the singular function.
        /// </returns>
        internal static TResult Result<TResult>(
            string namespaceName, string name, params object[] arguments)
        {
            throw new InvalidOperationException(Resources.DoNotCallDataSourceStubMethodDirectly);
        }
    }
}
