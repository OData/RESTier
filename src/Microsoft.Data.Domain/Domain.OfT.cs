using System;

namespace Microsoft.Data.Domain
{
    /// <summary>
    /// Represents a domain that expands upon on another domain.
    /// </summary>
    public class Domain<T> : DomainBase
        where T : IExpandableDomain
    {
        private T _expandableDomain;

        /// <summary>
        /// Initializes a new domain.
        /// </summary>
        public Domain()
        {
        }

        /// <summary>
        /// Creates the domain configuration for this domain.
        /// </summary>
        /// <returns>
        /// The domain configuration for this domain.
        /// </returns>
        protected override DomainConfiguration CreateDomainConfiguration()
        {
            return new DomainConfiguration(
                this.DomainConfigurationKey,
                this.ExpandableDomain.Configuration);
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
        protected override DomainContext CreateDomainContext(
            DomainConfiguration configuration)
        {
            this.ExpandableDomain.Initialize(configuration);
            return this.ExpandableDomain.Context;
        }

        /// <summary>
        /// Creates the expandable domain.
        /// </summary>
        /// <returns>
        /// The expandable domain.
        /// </returns>
        protected virtual T CreateExpandableDomain()
        {
            return Activator.CreateInstance<T>();
        }

        /// <summary>
        /// Releases the unmanaged resources that are used by the
        /// object and, optionally, releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var disposable = this._expandableDomain as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private T ExpandableDomain
        {
            get
            {
                if (this._expandableDomain == null)
                {
                    this._expandableDomain = this.CreateExpandableDomain();
                }
                return this._expandableDomain;
            }
        }
    }
}
