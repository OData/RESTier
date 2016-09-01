// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents the API engine and provides a set of static
    /// (Shared in Visual Basic) methods for interacting with objects
    /// that implement <see cref="ApiBase"/>.
    /// </summary>
    public static class ApiBaseExtensions
    {
        private static readonly MethodInfo SourceCoreMethod = typeof(ApiBaseExtensions)
            .GetMember("SourceCore", BindingFlags.NonPublic | BindingFlags.Static)
            .Cast<MethodInfo>().Single(m => m.IsGenericMethod);

        private static readonly MethodInfo Source2Method = typeof(DataSourceStub)
            .GetMember("GetQueryableSource").Cast<MethodInfo>()
            .Single(m => m.GetParameters().Length == 2);

        private static readonly MethodInfo Source3Method = typeof(DataSourceStub)
            .GetMember("GetQueryableSource").Cast<MethodInfo>()
            .Single(m => m.GetParameters().Length == 3);

        #region GetApiService<T>

        /// <summary>
        /// Gets a service instance.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance.</returns>
        public static T GetApiService<T>(this ApiBase api) where T : class
        {
            Ensure.NotNull(api, "api");
            return api.ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Gets all registered service instances.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The ordered collection of service instances.</returns>
        public static IEnumerable<T> GetApiServices<T>(this ApiBase api) where T : class
        {
            Ensure.NotNull(api, "api");
            return api.ServiceProvider.GetServices<T>();
        }

        #endregion

        #region PropertyBag

        /// <summary>
        /// Indicates if this object has a property.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// <c>true</c> if this object has the
        /// property; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasProperty(this ApiBase api, string name)
        {
            return api.GetPropertyBag().HasProperty(name);
        }

        /// <summary>
        /// Gets a property.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the property.
        /// </typeparam>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// The value of the property.
        /// </returns>
        public static T GetProperty<T>(this ApiBase api, string name)
        {
            return api.GetPropertyBag().GetProperty<T>(name);
        }

        /// <summary>
        /// Gets a property.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// The value of the property.
        /// </returns>
        public static object GetProperty(this ApiBase api, string name)
        {
            return api.GetPropertyBag().GetProperty(name);
        }

        /// <summary>
        /// Sets a property.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <param name="value">
        /// A value for the property.
        /// </param>
        public static void SetProperty(this ApiBase api, string name, object value)
        {
            api.GetPropertyBag().SetProperty(name, value);
        }

        /// <summary>
        /// Removes a property.
        /// </summary>
        /// <param name="api">
        /// An API.
        /// </param>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        public static void RemoveProperty(this ApiBase api, string name)
        {
            api.GetPropertyBag().RemoveProperty(name);
        }

        #endregion

        #region Model

        /// <summary>
        /// Asynchronously gets an API model using an API context.
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
        public static async Task<IEdmModel> GetModelAsync(
            this ApiBase api,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(api, "api");

            var config = api.Configuration;
            if (config.Model != null)
            {
                return config.Model;
            }

            var builder = api.GetApiService<IModelBuilder>();
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
                var buildContext = new ModelContext(api.ServiceProvider);
                var model = await builder.GetModelAsync(buildContext, cancellationToken);
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

        #region GetQueryableSource

        /// <summary>
        /// Gets a queryable source of data using an API context.
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
            Ensure.NotNull(name, "name");

            return api.SourceCore(null, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using an API context.
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
            Ensure.NotNull(namespaceName, "namespaceName");
            Ensure.NotNull(name, "name");

            return SourceCore(api, namespaceName, name, arguments);
        }

        /// <summary>
        /// Gets a queryable source of data using an API context.
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
            Ensure.NotNull(name, "name");

            var elementType = api.EnsureElementType(null, name);
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
            Ensure.NotNull(namespaceName, "namespaceName");
            Ensure.NotNull(name, "name");

            var elementType = api.EnsureElementType(namespaceName, name);
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
        public static async Task<QueryResult> QueryAsync(
            this ApiBase api,
            QueryRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(api, "api");
            Ensure.NotNull(request, "request");

            var queryContext = new QueryContext(api.ServiceProvider, request);
            var model = await api.GetModelAsync(cancellationToken);
            queryContext.Model = model;
            return await DefaultQueryHandler.QueryAsync(queryContext, cancellationToken);
        }

        #endregion

        #region Submit

        /// <summary>
        /// Asynchronously submits changes made using an API context.
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
        public static async Task<SubmitResult> SubmitAsync(
            this ApiBase api,
            ChangeSet changeSet = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(api, "api");

            var submitContext = new SubmitContext(api.ServiceProvider, changeSet);
            return await DefaultSubmitHandler.SubmitAsync(submitContext, cancellationToken);
        }

        #endregion

        #region GetQueryableSource Private

        private static IQueryable SourceCore(
            this ApiBase api,
            string namespaceName,
            string name,
            object[] arguments)
        {
            var elementType = api.EnsureElementType(namespaceName, name);
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
            this ApiBase api,
            string namespaceName,
            string name)
        {
            Type elementType = null;

            var mapper = api.GetApiService<IModelMapper>();
            if (mapper != null)
            {
                var modelContext = new ModelContext(api.ServiceProvider);
                if (namespaceName == null)
                {
                    mapper.TryGetRelevantType(modelContext, name, out elementType);
                }
                else
                {
                    mapper.TryGetRelevantType(modelContext, namespaceName, name, out elementType);
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

        #region PropertyBag Private

        private static PropertyBag GetPropertyBag(this ApiBase api)
        {
            Ensure.NotNull(api, "api");
            return api.GetApiService<PropertyBag>();
        }

        #endregion
    }
}
