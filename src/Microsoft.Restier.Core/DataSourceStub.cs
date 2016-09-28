// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;

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
    }
}
