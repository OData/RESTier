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
    /// through a set of registered hook points.
    /// </para>
    /// <para>
    /// Hook points may be singletons, meaning there is at most one instance of
    /// the hook point registered, or multi-cast, in which case there can be
    /// zero or more instances of the hook point that are registered. In the
    /// multi-cast case, registration order is maintained, and such hook points
    /// are normally used in the original or reverse order of registration.
    /// </para>
    /// <para>
    /// In order to use an API configuration, it must first be committed.
    /// This fixes the configuration so that its set of hook points are
    /// immutable, ensuring that any active use of the configuration sees a
    /// consistent set of hook points throughout a particular API flow.
    /// </para>
    /// </remarks>
    public class ApiConfiguration
    {
        private static ConcurrentDictionary<Type, Customizer> configurations =
            new ConcurrentDictionary<Type, Customizer>();

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
        public IServiceProvider ServiceProvider
        {
            get { return serviceProvider; }
        }

        internal IEdmModel Model { get; private set; }

        public static Customizer Customize<TKey>()
        {
            return Customize(typeof(TKey));
        }

        public static Customizer Customize(Type keyType)
        {
            return configurations.GetOrAdd(keyType, _ => new Customizer());
        }

        [CLSCompliant(false)]
        public static ApiConfiguration Create(Action<IServiceCollection> configurationCall)
        {
            return new ServiceCollection()
                .DefaultInnerMost()
                .Apply(configurationCall)
                .DefaultOuterMost()
                .BuildApiConfiguration();
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

        public sealed class Customizer
        {
            public Customizer()
            {
                InnerMost = new Anchor();
                OuterMost = new Anchor();
                Overrides = new Anchor();
                PrivateApi = new Anchor();
            }

            public Anchor InnerMost { get; private set; }

            public Anchor OuterMost { get; private set; }

            public Anchor PrivateApi { get; private set; }

            public Anchor Overrides { get; private set; }
        }

        public sealed class Anchor
        {
            private static Action<IServiceCollection> emptyConfig = _ => { };

            private Action<IServiceCollection> call;

            [CLSCompliant(false)]
            public Action<IServiceCollection> Configuration
            {
                get { return call ?? emptyConfig; }
            }

            [CLSCompliant(false)]
            public Anchor Append(Action<IServiceCollection> configurationCall)
            {
                call += configurationCall;
                return this;
            }

            [CLSCompliant(false)]
            public Anchor Prepend(Action<IServiceCollection> configurationCall)
            {
                call = configurationCall + call;
                return this;
            }
        }
    }
}
