// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents the API engine and provides a set of static
    /// (Shared in Visual Basic) methods for interacting with objects
    /// that implement <see cref="ApiBase"/>.
    /// </summary>
    public static class ApiBaseExtensions // TODO GitHubIssue#25,#26 : transactions, exception filters
    {
        #region Model

        /// <summary>
        /// Asynchronously gets an API model for an API.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is the API model.
        /// </returns>
        public static Task<IEdmModel> GetModelAsync(
            this ApiBase api,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(api, "api");

            return api.Context.GetModelAsync(cancellationToken);
        }

        #endregion

        #region GetQueryableSource

        /// <summary>
        /// Gets a queryable source of data exposed by an API.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="name">
        /// The name of an entity set, singleton or composable function import.
        /// </param>
        /// <param name="arguments">
        /// If <paramref name="name"/> is a composable function import,
        /// the arguments to be passed to the composable function import.
        /// </param>
        /// <returns>
        /// A queryable source.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the name identifies a singleton or a composable function import
        /// whose result is a singleton, the resulting queryable source will
        /// be configured such that it represents exactly zero or one result.
        /// </para>
        /// <para>
        /// Note that the resulting queryable source cannot be synchronously
        /// enumerated as the API engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable GetQueryableSource(
            this ApiBase api,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(api, "api");

            return api.Context.GetQueryableSource(name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data exposed by an API.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="namespaceName">
        /// The name of a namespace containing a composable function.
        /// </param>
        /// <param name="name">
        /// The name of a composable function.
        /// </param>
        /// <param name="arguments">
        /// The arguments to be passed to the composable function.
        /// </param>
        /// <returns>
        /// A queryable source.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the name identifies a composable function whose result is a
        /// singleton, the resulting queryable source will be configured such
        /// that it represents exactly zero or one result.
        /// </para>
        /// <para>
        /// Note that the resulting queryable source cannot be synchronously
        /// enumerated, as the API engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable GetQueryableSource(
            this ApiBase api,
            string namespaceName,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(api, "api");

            return api.Context.GetQueryableSource(namespaceName, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data exposed by an API.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable source.
        /// </typeparam>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="name">
        /// The name of an entity set, singleton or composable function import.
        /// </param>
        /// <param name="arguments">
        /// If <paramref name="name"/> is a composable function import,
        /// the arguments to be passed to the composable function import.
        /// </param>
        /// <returns>
        /// A queryable source.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the name identifies a singleton or a composable function import
        /// whose result is a singleton, the resulting queryable source will
        /// be configured such that it represents exactly zero or one result.
        /// </para>
        /// <para>
        /// Note that the resulting queryable source cannot be synchronously
        /// enumerated, as the API engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable<TElement> GetQueryableSource<TElement>(
            this ApiBase api,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(api, "api");

            return api.Context.GetQueryableSource<TElement>(name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data exposed by an API.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable source.
        /// </typeparam>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="namespaceName">
        /// The name of a namespace containing a composable function.
        /// </param>
        /// <param name="name">
        /// The name of a composable function.
        /// </param>
        /// <param name="arguments">
        /// The arguments to be passed to the composable function.
        /// </param>
        /// <returns>
        /// A queryable source.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the name identifies a composable function whose result is a
        /// singleton, the resulting queryable source will be configured such
        /// that it represents exactly zero or one result.
        /// </para>
        /// <para>
        /// Note that the resulting queryable source cannot be synchronously
        /// enumerated, as the API engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable<TElement> GetQueryableSource<TElement>(
            this ApiBase api,
            string namespaceName,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(api, "api");

            return api.Context.GetQueryableSource<TElement>(namespaceName, name, arguments);
        }

        #endregion

        #region Query

        /// <summary>
        /// Asynchronously queries for data exposed by an API.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="request">
        /// A query request.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a query result.
        /// </returns>
        public static Task<QueryResult> QueryAsync(
            this ApiBase api,
            QueryRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(api, "api");

            return api.Context.QueryAsync(request, cancellationToken);
        }

        #endregion

        #region Submit

        /// <summary>
        /// Asynchronously submits changes made to an API.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="changeSet">
        /// A change set, or <c>null</c> to submit existing pending changes.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a submit result.
        /// </returns>
        public static Task<SubmitResult> SubmitAsync(
            this ApiBase api,
            ChangeSet changeSet = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(api, "api");

            return api.Context.SubmitAsync(changeSet, cancellationToken);
        }

        #endregion
    }
}
