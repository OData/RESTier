// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
