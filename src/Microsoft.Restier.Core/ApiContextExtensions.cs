// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Properties;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents the API engine and provides a set of static
    /// (Shared in Visual Basic) methods for interacting with objects
    /// that implement <see cref="ApiContext"/>.
    /// </summary>
    public static class ApiContextExtensions
    {
        private static readonly MethodInfo SourceCoreMethod = typeof(ApiContextExtensions)
            .GetMember("SourceCore", BindingFlags.NonPublic | BindingFlags.Static)
            .Cast<MethodInfo>().Single(m => m.IsGenericMethod);

        private static readonly MethodInfo Source2Method = typeof(DataSourceStubs)
            .GetMember("Source").Cast<MethodInfo>()
            .Single(m => m.GetParameters().Length == 2);

        private static readonly MethodInfo Source3Method = typeof(DataSourceStubs)
            .GetMember("Source").Cast<MethodInfo>()
            .Single(m => m.GetParameters().Length == 3);

        public static ApiContext CreateNew(this ApiContext obj)
        {
            var sp = obj.ServiceProvider;
            return sp.GetService<IApiContextFactory>().CreateWithin(
                sp.GetService<IServiceScopeFactory>().CreateScope());
        }

        #region Model

        /// <summary>
        /// Asynchronously gets an API model using an API context.
        /// </summary>
        /// <param name="context">
        /// An API context.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is the API model.
        /// </returns>
        public static async Task<IEdmModel> GetModelAsync(
            this ApiContext context,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(context, "context");

            var config = context.Configuration;
            if (config.Model != null)
            {
                return config.Model;
            }

            var builder = context.GetApiService<IModelBuilder>();
            if (builder == null)
            {
                throw new InvalidOperationException(Resources.ModelBuilderNotRegistered);
            }

            Task<IEdmModel> running;
            var source = config.CompeteModelGeneration(out running);
            if (source == null)
            {
                return await running;
            }

            try
            {
                var model = await builder.GetModelAsync(new InvocationContext(context), cancellationToken);
                source.SetResult(model);
                return model;
            }
            catch (AggregateException e)
            {
                source.SetException(e.InnerExceptions);
                throw;
            }
            catch (Exception e)
            {
                source.SetException(e);
                throw;
            }
        }

        #endregion

        #region Source

        /// <summary>
        /// Gets a queryable source of data using an API context.
        /// </summary>
        /// <param name="context">
        /// An API context.
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
        public static IQueryable Source(
            this ApiContext context,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(name, "name");

            return context.SourceCore(null, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using an API context.
        /// </summary>
        /// <param name="context">
        /// An API context.
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
        public static IQueryable Source(
            this ApiContext context,
            string namespaceName,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(namespaceName, "namespaceName");
            Ensure.NotNull(name, "name");

            return SourceCore(context, namespaceName, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using an API context.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable source.
        /// </typeparam>
        /// <param name="context">
        /// An API context.
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
        public static IQueryable<TElement> Source<TElement>(
            this ApiContext context,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(name, "name");

            var elementType = context.EnsureElementType(null, name);
            if (typeof(TElement) != elementType)
            {
                throw new ArgumentException(Resources.ElementTypeNotMatch);
            }

            return SourceCore<TElement>(null, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using an API context.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the queryable source.
        /// </typeparam>
        /// <param name="context">
        /// An API context.
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
        public static IQueryable<TElement> Source<TElement>(
            this ApiContext context,
            string namespaceName,
            string name,
            params object[] arguments)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(namespaceName, "namespaceName");
            Ensure.NotNull(name, "name");

            var elementType = context.EnsureElementType(namespaceName, name);
            if (typeof(TElement) != elementType)
            {
                throw new ArgumentException(Resources.ElementTypeNotMatch);
            }

            return SourceCore<TElement>(namespaceName, name, arguments);
        }

        #endregion

        #region Query

        /// <summary>
        /// Asynchronously queries for data using an API context.
        /// </summary>
        /// <param name="context">
        /// An API context.
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
            this ApiContext context,
            QueryRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(request, "request");

            var queryContext = new QueryContext(context, request);
            var model = await context.GetModelAsync(cancellationToken);
            queryContext.Model = model;
            return await DefaultQueryHandler.QueryAsync(queryContext, cancellationToken);
        }

        #endregion

        #region Submit

        /// <summary>
        /// Asynchronously submits changes made using an API context.
        /// </summary>
        /// <param name="context">
        /// An API context.
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
            this ApiContext context,
            ChangeSet changeSet = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(context, "context");

            var submitContext = new SubmitContext(context, changeSet);
            var model = await context.GetModelAsync(cancellationToken);
            submitContext.Model = model;
            return await DefaultSubmitHandler.SubmitAsync(
                submitContext, cancellationToken);
        }

        #endregion

        #region Source Private

        private static IQueryable SourceCore(
            this ApiContext context,
            string namespaceName,
            string name,
            object[] arguments)
        {
            var elementType = context.EnsureElementType(namespaceName, name);
            var method = SourceCoreMethod.MakeGenericMethod(elementType);
            var args = new object[] { namespaceName, name, arguments };
            return method.Invoke(null, args) as IQueryable;
        }

        private static IQueryable<TElement> SourceCore<TElement>(
            string namespaceName,
            string name,
            object[] arguments)
        {
            MethodInfo sourceMethod;
            Expression[] expressions;
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
            this ApiContext context,
            string namespaceName,
            string name)
        {
            Type elementType = null;

            var mapper = context.GetApiService<IModelMapper>();
            if (mapper != null)
            {
                if (namespaceName == null)
                {
                    mapper.TryGetRelevantType(context, name, out elementType);
                }
                else
                {
                    mapper.TryGetRelevantType(context, namespaceName, name, out elementType);
                }
            }

            if (elementType == null)
            {
                throw new NotSupportedException(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.ElementTypeNotFound,
                    name));
            }

            return elementType;
        }

        #endregion
    }
}
