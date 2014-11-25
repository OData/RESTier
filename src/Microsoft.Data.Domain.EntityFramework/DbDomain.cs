// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Entity;
using Microsoft.Data.Domain.Model;
using Microsoft.Data.Domain.Query;

namespace Microsoft.Data.Domain.EntityFramework
{
    using Microsoft.Data.Domain.EntityFramework.Submit;
    using Microsoft.Data.Domain.Submit;
    using Model;
    using Query;

    /// <summary>
    /// Represents a domain over a DbContext.
    /// </summary>
    public class DbDomain<T> : DomainBase
        where T : DbContext
    {
        /// <summary>
        /// Initializes a new domain.
        /// </summary>
        public DbDomain()
        {
        }

        /// <summary>
        /// Gets the underlying DbContext for this domain.
        /// </summary>
        protected T DbContext
        {
            get
            {
                return this.DomainContext.GetProperty<T>("DbContext");
            }
        }

        /// <summary>
        /// Creates the domain configuration for this domain.
        /// </summary>
        /// <returns>
        /// The domain configuration for this domain.
        /// </returns>
        protected override DomainConfiguration CreateDomainConfiguration()
        {
            var configuration = base.CreateDomainConfiguration();
            configuration.SetHookPoint(
                typeof(IModelProducer),
                ModelProducer.Instance);
            configuration.SetHookPoint(
                typeof(IModelMapper),
                new ModelMapper(typeof(T)));
            configuration.SetHookPoint(
                typeof(IQueryExpressionSourcer),
                QueryExpressionSourcer.Instance);
            configuration.SetHookPoint(
                typeof(IQueryExecutor),
                QueryExecutor.Instance);
            configuration.SetHookPoint(
                typeof(IChangeSetPreparer),
                ChangeSetPreparer.Instance);
            configuration.SetHookPoint(
                typeof(ISubmitExecutor),
                SubmitExecutor.Instance);
            return configuration;
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
            var context = base.CreateDomainContext(configuration);
            var dbContext = this.CreateDbContext();
            dbContext.Configuration.ProxyCreationEnabled = false;
            context.SetProperty("DbContext", dbContext);
            return context;
        }

        /// <summary>
        /// Creates the underlying DbContext used by this domain.
        /// </summary>
        /// <returns>
        /// The underlying DbContext used by this domain.
        /// </returns>
        protected virtual T CreateDbContext()
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
                var dbContext = this.DomainContext
                    .GetProperty<DbContext>("DbContext");
                if (dbContext != null)
                {
                    dbContext.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
