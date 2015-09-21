// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents a configuration that defines a domain.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A domain configuration defines the model and behavior of a domain
    /// through a set of registered hook points. It also maintains a set of
    /// properties that can be used to share static data between hook points.
    /// </para>
    /// <para>
    /// Hook points may be singletons, meaning there is at most one instance of
    /// the hook point registered, or multi-cast, in which case there can be
    /// zero or more instances of the hook point that are registered. In the
    /// multi-cast case, registration order is maintained, and such hook points
    /// are normally used in the original or reverse order of registration.
    /// </para>
    /// <para>
    /// In order to use a domain configuration, it must first be committed.
    /// This fixes the configuration so that its set of hook points are
    /// immutable, ensuring that any active use of the configuration sees a
    /// consistent set of hook points throughout a particular domain flow.
    /// </para>
    /// </remarks>
    public class DomainConfiguration : PropertyBag
    {
        private readonly IDictionary<Type, IHookHandler> hookHandlers =
            new ConcurrentDictionary<Type, IHookHandler>();

        /// <summary>
        /// Gets a value indicating whether this domain configuration has been committed.
        /// </summary>
        public bool IsCommitted { get; private set; }

        internal IEdmModel Model { get; set; }

        /// <summary>
        /// Ensures this domain configuration has been committed.
        /// </summary>
        public void EnsureCommitted()
        {
            this.IsCommitted = true;
        }

        #region HookHandler
        /// <summary>
        /// Add an hook handler instance.
        /// </summary>
        /// <typeparam name="T">The context class.</typeparam>
        /// <param name="handler">An instance of hook handler for TContext.</param>
        /// <returns>Current <see cref="DomainConfiguration"/></returns>
        public DomainConfiguration AddHookHandler<T>(T handler) where T : class, IHookHandler
        {
            Ensure.NotNull(handler, "handler");

            if (this.IsCommitted)
            {
                throw new InvalidOperationException();
            }

            if (!typeof(T).IsInterface)
            {
                throw new InvalidOperationException("Should specify an interface type T for the handler.");
            }

            var delegateHandler = handler as IDelegateHookHandler<T>;
            if (delegateHandler != null)
            {
                delegateHandler.InnerHandler = this.GetHookHandler<T>();
            }

            this.hookHandlers[typeof(T)] = handler;
            return this;
        }

        internal T GetHookHandler<T>() where T : class, IHookHandler
        {
            IHookHandler value;
            this.hookHandlers.TryGetValue(typeof(T), out value);
            return value as T;
        }
        #endregion
    }
}
