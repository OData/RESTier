// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    /// <para>
    /// A domain configuration is intended to be long-lived, and can be
    /// statically cached according to a domain configuration key specified
    /// when the configuration is created. Additionally, the domain model
    /// produced as a result of a particular configuration is cached under
    /// the same key to avoid re-computing it on each invocation.
    /// </para>
    /// </remarks>
    public class DomainConfiguration : PropertyBag
    {
        private static readonly IDictionary<object, DomainConfiguration> Configurations =
            new ConcurrentDictionary<object, DomainConfiguration>();

        private readonly IDictionary<Type, object> hookHandlers = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainConfiguration" /> class.
        /// </summary>
        public DomainConfiguration()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainConfiguration" /> class
        /// that is based on an existing configuration.
        /// </summary>
        /// <param name="key">
        /// A domain configuration key.
        /// </param>
        public DomainConfiguration(object key)
        {
            this.Key = key;
            if (key != null)
            {
                DomainConfiguration.Configurations[key] = this;
            }
        }

        /// <summary>
        /// Gets the domain configuration key, if any.
        /// </summary>
        public object Key { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this domain configuration has been committed.
        /// </summary>
        public bool IsCommitted { get; private set; }

        internal IEdmModel Model { get; set; }

        /// <summary>
        /// Gets an existing domain configuration from a key.
        /// </summary>
        /// <param name="key">
        /// A key.
        /// </param>
        /// <returns>
        /// The existing domain configuration, or <c>null</c> if
        /// no configuration with the key was previously created.
        /// </returns>
        public static DomainConfiguration FromKey(object key)
        {
            Ensure.NotNull(key, "key");
            DomainConfiguration configuration = null;
            DomainConfiguration.Configurations
                .TryGetValue(key, out configuration);
            return configuration;
        }

        /// <summary>
        /// Invalidates an existing domain configuration given a key.
        /// </summary>
        /// <param name="key">
        /// A key.
        /// </param>
        public static void Invalidate(object key)
        {
            Ensure.NotNull(key, "key");
            DomainConfiguration.Configurations.Remove(key);
        }

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
            object value;
            this.hookHandlers.TryGetValue(typeof(T), out value);
            return value as T;
        }
        #endregion
    }
}
