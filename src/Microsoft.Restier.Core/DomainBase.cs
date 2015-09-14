// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Restier.Core.Conventions;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents a base class for a domain.
    /// </summary>
    public abstract class DomainBase : IDomain
    {
        private DomainConfiguration domainConfiguration;
        private DomainContext domainContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainBase"/> class.
        /// </summary>
        protected DomainBase()
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DomainBase"/> class.
        /// </summary>
        ~DomainBase()
        {
            this.Dispose(false);
        }

        DomainContext IDomain.Context
        {
            get
            {
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                return this.DomainContext;
            }
        }

        /// <summary>
        /// Gets the initial domain configuration key.
        /// </summary>
        /// <remarks>
        /// The domain configuration key specifies data that uniquely
        /// identifies the initial configuration. If multiple domain
        /// instances return the same key, the configuration is created
        /// once and all instances will re-use the same configuration.
        /// </remarks>
        protected virtual object DomainConfigurationKey
        {
            get
            {
                return this.GetType();
            }
        }

        /// <summary>
        /// Gets the domain configuration for this domain.
        /// </summary>
        protected DomainConfiguration DomainConfiguration
        {
            get
            {
                if (this.domainConfiguration == null)
                {
                    DomainConfiguration configuration = null;
                    var key = this.DomainConfigurationKey;
                    if (key != null)
                    {
                        configuration = DomainConfiguration.FromKey(key);
                    }

                    if (configuration == null)
                    {
                        configuration = this.CreateDomainConfiguration();
                        EnableConventions(configuration, this.GetType());
                        DomainParticipantAttribute.ApplyConfiguration(
                            this.GetType(), configuration);
                    }

                    configuration.EnsureCommitted();
                    this.domainConfiguration = configuration;
                }

                return this.domainConfiguration;
            }
        }

        /// <summary>
        /// Gets the domain context for this domain.
        /// </summary>
        protected DomainContext DomainContext
        {
            get
            {
                if (this.domainContext == null)
                {
                    this.domainContext = this.CreateDomainContext(
                        this.DomainConfiguration);
                    this.domainContext.SetProperty(typeof(Domain).AssemblyQualifiedName, this);
                    DomainParticipantAttribute.ApplyInitialization(
                        this.GetType(), this, this.domainContext);
                }

                return this.domainContext;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this domain has been disposed.
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// Performs application-defined tasks associated with
        /// freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!this.IsDisposed && this.domainContext != null)
            {
                DomainParticipantAttribute.ApplyDisposal(
                    this.GetType(), this, this.domainContext);
            }

            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Creates the domain configuration for this domain.
        /// </summary>
        /// <returns>
        /// The domain configuration for this domain.
        /// </returns>
        protected virtual DomainConfiguration CreateDomainConfiguration()
        {
            var config = new DomainConfiguration(this.DomainConfigurationKey);
            AddDefaultHooks(config);
            return config;
        }

        /// <summary>
        /// Creates the domain context for this domain.
        /// </summary>
        /// <param name="configuration">
        /// The domain configuration to use.
        /// </param>
        /// <returns>
        /// The domain context for this domain.
        /// </returns>
        protected virtual DomainContext CreateDomainContext(
            DomainConfiguration configuration)
        {
            return new DomainContext(configuration);
        }

        /// <summary>
        /// Releases the unmanaged resources that are used by the
        /// object and, optionally, releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.domainContext = null;
                this.IsDisposed = true;
            }
        }

        /// <summary>
        /// Enables code-based conventions for a domain.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="targetType">
        /// The type of a class on which code-based conventions are used.
        /// </param>
        /// <remarks>
        /// This method adds hook points to the domain configuration that
        /// inspect a target type for a variety of code-based conventions
        /// such as usage of specific attributes or members that follow
        /// certain naming conventions.
        /// </remarks>
        private static void EnableConventions(
            DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");

            ConventionalChangeSetAuthorizer.ApplyTo(configuration, targetType);
            ConventionalChangeSetEntryFilter.ApplyTo(configuration, targetType);
            configuration.AddHookPoint(typeof(IChangeSetEntryValidator), ConventionalChangeSetEntryValidator.Instance);
            ConventionalDomainModelBuilder.ApplyTo(configuration, targetType);
            ConventionalActionProvider.ApplyTo(configuration, targetType);
            ConventionalEntitySetFilter.ApplyTo(configuration, targetType);
        }

        private static void AddDefaultHooks(DomainConfiguration configuration)
        {
            configuration.AddHookHandler<IQueryExecutor>(DefaultQueryExecutor.Instance);
        }
    }
}
