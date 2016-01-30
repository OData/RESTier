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
    /// Represents an API over a DbContext.
    /// </summary>
    /// <typeparam name="T">The DbContext type.</typeparam>
#if EF7
    [CLSCompliant(false)]
#endif
    public class DbApi<T> : ApiBase
        where T : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbApi{T}" /> class.
        /// </summary>
        public DbApi()
        {
        }

        /// <summary>
        /// Gets the underlying DbContext for this API.
        /// </summary>
        protected T DbContext
        {
            get
            {
                return this.ApiContext.GetProperty<T>(DbApiConstants.DbContextKey);
            }
        }

        /// <summary>
        /// Creates the API configuration for this API.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ApiBuilder"/> with which to create an <see cref="ApiConfiguration"/>.
        /// </param>
        /// <returns>
        /// The API configuration for this API.
        /// </returns>
        [CLSCompliant(false)]
        protected override ApiBuilder ConfigureApiBuilder(ApiBuilder builder)
        {
            builder = base.ConfigureApiBuilder(builder);
            builder.AddHookHandler<IModelBuilder>(ModelProducer.Instance);
            builder.AddHookHandler<IModelMapper>(new ModelMapper(typeof(T)));
            builder.AddHookHandler<IQueryExpressionSourcer>(QueryExpressionSourcer.Instance);
            builder.AddHookHandler<IQueryExecutor>(QueryExecutor.Instance);
            builder.AddHookHandler<IChangeSetPreparer>(ChangeSetPreparer.Instance);
            builder.AddHookHandler<ISubmitExecutor>(SubmitExecutor.Instance);
            return builder;
        }

        /// <summary>
        /// Creates the API context for this API.
        /// </summary>
        /// <param name="configuration">
        /// The API configuration to use.
        /// </param>
        /// <returns>
        /// The API context for this API.
        /// </returns>
        protected override ApiContext CreateApiContext(
            ApiConfiguration configuration)
        {
            var context = base.CreateApiContext(configuration);
            var dbContext = this.CreateDbContext();
#if EF7
            // TODO GitHubIssue#58: Figure out the equivalent measurement to suppress proxy generation in EF7.
#else
            dbContext.Configuration.ProxyCreationEnabled = false;
#endif
            context.SetProperty(DbApiConstants.DbContextKey, dbContext);
            return context;
        }

        /// <summary>
        /// Creates the underlying DbContext used by this API.
        /// </summary>
        /// <returns>
        /// The underlying DbContext used by this API.
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
                var dbContext = this.ApiContext
                    .GetProperty<DbContext>(DbApiConstants.DbContextKey);
                if (dbContext != null)
                {
                    dbContext.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
