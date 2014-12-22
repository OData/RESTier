// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents a base class for a domain.
    /// </summary>
    public abstract class DomainBase : IExpandableDomain, IDisposable
    {
        private DomainConfiguration _domainConfiguration;
        private DomainContext _domainContext;

        /// <summary>
        /// Initializes a new domain.
        /// </summary>
        protected DomainBase()
        {
        }

        /// <summary>
        /// Finalizes this domain.
        /// </summary>
        ~DomainBase()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with
        /// freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!this.IsDisposed && this._domainContext != null)
            {
                DomainParticipantAttribute.ApplyDisposal(
                    this.GetType(), this, this._domainContext);
            }
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        DomainConfiguration IExpandableDomain.Configuration
        {
            get
            {
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
                return this.DomainConfiguration;
            }
        }

        bool IExpandableDomain.IsInitialized
        {
            get
            {
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
                return this.HasDomainContext;
            }
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

        void IExpandableDomain.Initialize(
            DomainConfiguration derivedConfiguration)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (this._domainContext != null)
            {
                throw new InvalidOperationException();
            }
            Ensure.NotNull(derivedConfiguration, "derivedConfiguration");
            var baseConfiguration = this.DomainConfiguration;
            var candidate = derivedConfiguration;
            while (candidate != baseConfiguration)
            {
                if (candidate.BaseConfiguration == null)
                {
                    // TODO: error message
                    throw new ArgumentException();
                }
                candidate = candidate.BaseConfiguration;
            }
            this._domainConfiguration = derivedConfiguration;
            this._domainContext = this.CreateDomainContext(derivedConfiguration);
            DomainParticipantAttribute.ApplyInitialization(
                this.GetType(), this, this._domainContext);
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
                if (this._domainConfiguration == null)
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
                        DomainParticipantAttribute.ApplyConfiguration(
                            this.GetType(), configuration);
                    }
                    configuration.EnsureCommitted();
                    this._domainConfiguration = configuration;
                }
                return this._domainConfiguration;
            }
        }

        /// <summary>
        /// Gets a value indicating if this domain
        /// has created its domain context.
        /// </summary>
        protected bool HasDomainContext
        {
            get
            {
                return this._domainContext != null;
            }
        }

        /// <summary>
        /// Gets the domain context for this domain.
        /// </summary>
        protected DomainContext DomainContext
        {
            get
            {
                if (this._domainContext == null)
                {
                    this._domainContext = this.CreateDomainContext(
                        this.DomainConfiguration);
                    DomainParticipantAttribute.ApplyInitialization(
                        this.GetType(), this, this._domainContext);
                }
                return this._domainContext;
            }
        }

        /// <summary>
        /// Gets a value indicating if this domain has been disposed.
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// Creates the domain configuration for this domain.
        /// </summary>
        /// <returns>
        /// The domain configuration for this domain.
        /// </returns>
        protected virtual DomainConfiguration CreateDomainConfiguration()
        {
            return new DomainConfiguration(this.DomainConfigurationKey);
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
                this._domainContext = null;
                this.IsDisposed = true;
            }
        }
    }
}
