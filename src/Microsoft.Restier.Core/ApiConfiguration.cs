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

        private Task<IEdmModel> modelTask;

        internal IEdmModel Model { get; private set; }

        /// <summary>
        /// Adds a configuration procedure for apiType.
        /// This is expected to be called by publisher like WebApi to add services.
        /// </summary>
        /// <param name="apiType">
        /// The Api Type.
        /// </param>
        /// <param name="configurationCallback">
        /// An action that will be called during the configuration of apiType.
        /// </param>
        [CLSCompliant(false)]
        public static void AddPublisherServices(Type apiType, Action<IServiceCollection> configurationCallback)
        {
            publisherServicesCallback.AddOrUpdate(
                apiType,
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
