// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents a configuration that defines an API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An API configuration defines the model and behavior of an API
    /// through a set of registered Api services in DI container.
    /// </para>
    /// <para>
    /// Api services may be singletons, meaning there is at most one instance,
    /// or scoped, in which case there will be one instances of the services for each scope.
    /// </para>
    /// </remarks>
    public class ApiConfiguration
    {
        private static ConcurrentDictionary<Type, Action<IServiceCollection>> publisherServicesCallback =
            new ConcurrentDictionary<Type, Action<IServiceCollection>>();

        private static Action<IServiceCollection> emptyConfig = _ => { };

        private IServiceProvider serviceProvider;

        private Task<IEdmModel> modelTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConfiguration" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        /// An <see cref="IServiceProvider"/> containing all services of this <see cref="ApiConfiguration"/>.
        /// </param>
        public ApiConfiguration(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this <see cref="ApiConfiguration"/>.
        /// </summary>
        internal IServiceProvider ServiceProvider
        {
            get { return serviceProvider; }
        }

        internal IEdmModel Model { get; private set; }

        /// <summary>
        /// Adds a configuration procedure for API type <typeparamref name="TApi"/>.
        /// This is expected to be called by publisher like WebApi to add services.
        /// </summary>
        /// <typeparam name="TApi">The API type.</typeparam>
        /// <param name="configurationCallback">
        /// An action that will be called during the configuration of <typeparamref name="TApi"/>.
        /// </param>
        [CLSCompliant(false)]
        public static void AddPublisherServices<TApi>(Action<IServiceCollection> configurationCallback)
             where TApi : ApiBase
        {
            publisherServicesCallback.AddOrUpdate(
                typeof(TApi),
                configurationCallback,
                (type, existing) => existing + configurationCallback);
        }

        /// <summary>
        /// Get publisher registering service callback for specified Api.
        /// </summary>
        /// <param name="apiType">
        /// The Api type of which to get the publisher registering service callback.
        /// </param>
        /// <returns>The service registering callback.</returns>
        [CLSCompliant(false)]
        public static Action<IServiceCollection> GetPublisherServiceCallback(Type apiType)
        {
            Action<IServiceCollection> val;
            if (publisherServicesCallback.TryGetValue(apiType, out val))
            {
                return val;
            }

            return emptyConfig;
        }

        internal TaskCompletionSource<IEdmModel> CompeteModelGeneration(out Task<IEdmModel> running)
        {
            var source = new TaskCompletionSource<IEdmModel>(TaskCreationOptions.AttachedToParent);
            var runningTask = Interlocked.CompareExchange(ref modelTask, source.Task, null);
            if (runningTask != null)
            {
                running = runningTask;
                source.SetCanceled();
                return null;
            }

            source.Task.ContinueWith(
                task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        Model = task.Result;
                    }
                    else
                    {
                        // Set modelTask null to allow retrying GetModelAsync.
                        Interlocked.Exchange(ref modelTask, null);
                    }
                },
                TaskContinuationOptions.ExecuteSynchronously);
            running = null;
            return source;
        }
    }
}
