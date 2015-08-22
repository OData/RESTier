// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents the domain engine and provides a set of static
    /// (Shared in Visual Basic) methods for interacting with objects
    /// that implement <see cref="IDomain"/>.
    /// </summary>
    public static class Domain // TODO GitHubIssue#25,#26 : transactions, exception filters
    {
        private static readonly MethodInfo SourceCoreMethod = typeof(Domain)
            .GetMember("SourceCore", BindingFlags.NonPublic | BindingFlags.Static)
            .Cast<MethodInfo>().Single(m => m.IsGenericMethod);

        private static readonly MethodInfo Source2Method = typeof(DomainData)
            .GetMember("Source").Cast<MethodInfo>()
            .Single(m => m.GetParameters().Length == 2);

        private static readonly MethodInfo Source3Method = typeof(DomainData)
            .GetMember("Source").Cast<MethodInfo>()
            .Single(m => m.GetParameters().Length == 3);

        #region Model

        /// <summary>
        /// Asynchronously gets a domain model for a domain.
        /// </summary>
        /// <param name="domain">
        /// A domain.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is the domain model.
        /// </returns>
        public static Task<IEdmModel> GetModelAsync(
            this IDomain domain,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(domain, "domain");
            return Domain.GetModelAsync(domain.Context, cancellationToken);
        }

        /// <summary>
        /// Asynchronously gets a domain model using a domain context.
        /// </summary>
        /// <param name="context">
        /// A domain context.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is the domain model.
        /// </returns>
        public static async Task<IEdmModel> GetModelAsync(
            DomainContext context,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(context, "context");
            var configuration = context.Configuration;
            var model = configuration.Model;
            if (model == null)
            {
                var modelContext = new ModelBuilderContext(context);

                var producer = context.Configuration.GetHookHandler<ModelBuilderContext>();
                if (producer != null)
                {
                    await producer.HandleAsync(modelContext, cancellationToken);
                }

                configuration.Model = new DomainModel(configuration, modelContext.Model);
                model = configuration.Model;
            }

            return model;
        }

        #endregion

        #region Source

        /// <summary>
        /// Gets a queryable source of data exposed by a domain.
        /// </summary>
        /// <param name="domain">
        /// A domain.
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
        /// enumerated as the domain engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable Source(
            this IDomain domain,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(domain, "domain");
            return Domain.Source(domain.Context, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using a domain context.
        /// </summary>
        /// <param name="context">
        /// A domain context.
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
        /// enumerated as the domain engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable Source(
            DomainContext context,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(name, "name");
            return Domain.SourceCore(context, null, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data exposed by a domain.
        /// </summary>
        /// <param name="domain">
        /// A domain.
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
        /// enumerated, as the domain engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable Source(
            this IDomain domain,
            string namespaceName,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(domain, "domain");
            return Domain.Source(domain.Context, namespaceName, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using a domain context.
        /// </summary>
        /// <param name="context">
        /// A domain context.
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
        /// enumerated, as the domain engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable Source(
            DomainContext context,
            string namespaceName,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(namespaceName, "namespaceName");
            Ensure.NotNull(name, "name");
            return Domain.SourceCore(context, namespaceName, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data exposed by a domain.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable source.
        /// </typeparam>
        /// <param name="domain">
        /// A domain.
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
        /// enumerated, as the domain engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable<TElement> Source<TElement>(
            this IDomain domain,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(domain, "domain");
            return Domain.Source<TElement>(domain.Context, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using a domain context.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable source.
        /// </typeparam>
        /// <param name="context">
        /// A domain context.
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
        /// enumerated, as the domain engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable<TElement> Source<TElement>(
            DomainContext context,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(name, "name");
            var elementType = Domain.EnsureElementType(context, null, name);
            if (typeof(TElement) != elementType)
            {
                // TODO GitHubIssue#24 : error message
                throw new ArgumentException();
            }

            return Domain.SourceCore<TElement>(null, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data exposed by a domain.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable source.
        /// </typeparam>
        /// <param name="domain">
        /// A domain.
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
        /// enumerated, as the domain engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable<TElement> Source<TElement>(
            this IDomain domain,
            string namespaceName,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(domain, "domain");
            return Domain.Source<TElement>(
                domain.Context, namespaceName, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using a domain context.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable source.
        /// </typeparam>
        /// <param name="context">
        /// A domain context.
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
        /// enumerated, as the domain engine only operates asynchronously.
        /// </para>
        /// </remarks>
        public static IQueryable<TElement> Source<TElement>(
            DomainContext context,
            string namespaceName,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(namespaceName, "namespaceName");
            Ensure.NotNull(name, "name");
            var elementType = Domain.EnsureElementType(context, namespaceName, name);
            if (typeof(TElement) != elementType)
            {
                // TODO GitHubIssue#24 : error message
                throw new ArgumentException();
            }

            return Domain.SourceCore<TElement>(namespaceName, name, arguments);
        }
        #endregion

        #region Query

        /// <summary>
        /// Asynchronously queries for data exposed by a domain.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the query.
        /// </typeparam>
        /// <param name="domain">
        /// A domain.
        /// </param>
        /// <param name="query">
        /// A composed query that was derived from a queryable source.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// whose result is a sequence of the query results.
        /// </returns>
        public static async Task<IEnumerable<TElement>> QueryAsync<TElement>(
            this IDomain domain,
            IQueryable<TElement> query,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(domain, "domain");
            var request = new QueryRequest(query);
            var result = await Domain.QueryAsync(
                domain.Context, request, cancellationToken);
            return result.Results.Cast<TElement>();
        }

        /// <summary>
        /// Asynchronously queries for singular data exposed by a domain.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the query.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the result.
        /// </typeparam>
        /// <param name="domain">
        /// A domain.
        /// </param>
        /// <param name="query">
        /// A composed query that was derived from a queryable source.
        /// </param>
        /// <param name="singularExpression">
        /// An expression that when composed on top of
        /// the composed query produces a singular result.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is the singular result.
        /// </returns>
        public static async Task<TResult> QueryAsync<TElement, TResult>(
            this IDomain domain,
            IQueryable<TElement> query,
            Expression<Func<IQueryable<TElement>, TResult>> singularExpression,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(domain, "domain");
            var request = QueryRequest.Create(query, singularExpression);
            var result = await Domain.QueryAsync(
                domain.Context, request, cancellationToken);
            foreach (TResult first in result.Results)
            {
                return first;
            }

            return default(TResult);
        }

        /// <summary>
        /// Asynchronously queries for data exposed by a domain.
        /// </summary>
        /// <param name="domain">
        /// A domain.
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
            this IDomain domain,
            QueryRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(domain, "domain");
            return Domain.QueryAsync(domain.Context, request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously queries for data using a domain context.
        /// </summary>
        /// <param name="context">
        /// A domain context.
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
        public static async Task<QueryResult> QueryAsync(
            DomainContext context,
            QueryRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(request, "request");
            var queryContext = new QueryContext(context, request);
            var model = await Domain.GetModelAsync(context) as DomainModel;
            queryContext.Model = new DomainModel(queryContext, model.InnerModel);
            var handler = queryContext.GetHookPoint<IQueryHandler>();
            return await handler.QueryAsync(queryContext, cancellationToken);
        }

        #endregion

        #region Submit

        /// <summary>
        /// Asynchronously submits changes made to a domain.
        /// </summary>
        /// <param name="domain">
        /// A domain.
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
            this IDomain domain,
            ChangeSet changeSet = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(domain, "domain");
            return Domain.SubmitAsync(
                domain.Context,
                changeSet,
                cancellationToken);
        }

        /// <summary>
        /// Asynchronously submits changes made using a domain context.
        /// </summary>
        /// <param name="context">
        /// A domain context.
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
        public static async Task<SubmitResult> SubmitAsync(
            DomainContext context,
            ChangeSet changeSet = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(context, "context");
            if (context.IsSubmitting)
            {
                throw new InvalidOperationException();
            }

            var submitContext = new SubmitContext(context, changeSet);
            context.IsSubmitting = true;
            try
            {
                var model = await Domain.GetModelAsync(context) as DomainModel;
                submitContext.Model = new DomainModel(
                    submitContext, model.InnerModel);
                var handler = submitContext.GetHookPoint<ISubmitHandler>();
                return await handler.SubmitAsync(
                    submitContext, cancellationToken);
            }
            finally
            {
                context.IsSubmitting = false;
            }
        }

        #endregion

        #region Source Private
        private static IQueryable SourceCore(
            DomainContext context,
            string namespaceName,
            string name,
            object[] arguments)
        {
            var elementType = Domain.EnsureElementType(context, namespaceName, name);
            var method = SourceCoreMethod.MakeGenericMethod(elementType);
            var args = new object[] { namespaceName, name, arguments };
            return method.Invoke(null, args) as IQueryable;
        }

        private static IQueryable<TElement> SourceCore<TElement>(
            string namespaceName,
            string name,
            object[] arguments)
        {
            MethodInfo sourceMethod = null;
            Expression[] expressions = null;
            if (namespaceName == null)
            {
                sourceMethod = Source2Method;
                expressions = new Expression[]
                {
                    Expression.Constant(name),
                    Expression.Constant(arguments, typeof(object[]))
                };
            }
            else
            {
                sourceMethod = Source3Method;
                expressions = new Expression[]
                {
                    Expression.Constant(namespaceName),
                    Expression.Constant(name),
                    Expression.Constant(arguments, typeof(object[]))
                };
            }

            return new QueryableSource<TElement>(
                Expression.Call(
                    null,
                    sourceMethod.MakeGenericMethod(typeof(TElement)),
                    expressions));
        }

        private static Type EnsureElementType(
            DomainContext context,
            string namespaceName,
            string name)
        {
            Type elementType = null;
            bool hasElementType = false;
            var mappers = context.Configuration.GetHookPoints<IModelMapper>();
            foreach (var mapper in mappers.Reverse())
            {
                if (namespaceName == null)
                {
                    hasElementType = mapper.TryGetRelevantType(
                        context, name, out elementType);
                }
                else
                {
                    hasElementType = mapper.TryGetRelevantType(context, namespaceName, name, out elementType);
                }

                if (hasElementType)
                {
                    break;
                }
            }

            if (!hasElementType)
            {
                var mapper = context.Configuration.GetHookPoint<IModelMapper>();
                if (mapper != null)
                {
                    if (namespaceName == null)
                    {
                        hasElementType = mapper.TryGetRelevantType(
                            context, name, out elementType);
                    }
                    else
                    {
                        hasElementType = mapper.TryGetRelevantType(context, namespaceName, name, out elementType);
                    }
                }
            }

            if (elementType == null)
            {
                // TODO GitHubIssue#24 : error message
                throw new NotSupportedException();
            }

            return elementType;
        }
        #endregion
    }
}
