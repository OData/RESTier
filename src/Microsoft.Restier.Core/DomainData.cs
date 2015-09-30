// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents methods that identify API data.
    /// </summary>
    /// <remarks>
    /// The methods in this class are placeholders that identify API data
    /// in a normalized manner inside a query expression. This enables query
    /// hook points to identify and reason about the referenced API data.
    /// </remarks>
    public static class ApiData
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
        public static IQueryable<TElement> Source<TElement>(
            string name, params object[] arguments)
        {
            throw new InvalidOperationException();
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
        public static IQueryable<TElement> Source<TElement>(
            string namespaceName, string name, params object[] arguments)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Identifies an entity set or results of a call to a function import.
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
        public static IEnumerable<TElement> Results<TElement>(
            string name, params object[] arguments)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Identifies a singleton or result of a
        /// call to a singular function import.
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
        public static TResult Result<TResult>(
            string name, params object[] arguments)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Identifies the results of a call to a function.
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
        public static IEnumerable<TElement> Results<TElement>(
            string namespaceName, string name, params object[] arguments)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Identifies the result of a call to a singular function.
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
        public static TResult Result<TResult>(
            string namespaceName, string name, params object[] arguments)
        {
            throw new InvalidOperationException();
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
        public static TResult Value<TResult>(
            object source, string propertyName)
        {
            throw new InvalidOperationException();
        }
    }
}
