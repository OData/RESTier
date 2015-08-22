// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if EF7
using Microsoft.Data.Entity;
#else
using System.Data.Entity;
#endif
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFramework.Model;
using Microsoft.Restier.EntityFramework.Query;
using Microsoft.Restier.EntityFramework.Submit;

namespace Microsoft.Restier.EntityFramework
{
    /// <summary>
    /// Represents a domain over a DbContext.
    /// </summary>
    /// <typeparam name="T">The DbContext type.</typeparam>
#if EF7
    [CLSCompliant(false)]
#endif
    public class DbDomain<T> : DomainBase
        where T : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbDomain{T}" /> class.
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
            configuration.AddHookHandler<ModelBuilderContext>(ModelProducer.Instance);

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
#if EF7
            // TODO GitHubIssue#58: Figure out the equivalent measurement to suppress proxy generation in EF7.
#else
            dbContext.Configuration.ProxyCreationEnabled = false;
#endif
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
