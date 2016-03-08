// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if EF7
using Microsoft.Data.Entity;
#else
using System.Data.Entity;
#endif
using Microsoft.Extensions.DependencyInjection;
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
                return this.ApiContext.GetApiService<T>();
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
        protected override ApiBuilder ConfigureApi(ApiBuilder builder)
        {
            builder = base.ConfigureApi(builder)
                .CutoffPrevious<IModelBuilder>(ModelProducer.Instance)
                .CutoffPrevious<IModelMapper>(new ModelMapper(typeof(T)))
                .CutoffPrevious<IQueryExpressionSourcer, QueryExpressionSourcer>()
                .CutoffPrevious<IQueryExecutor>(QueryExecutor.Instance)
                .CutoffPrevious<IChangeSetPreparer, ChangeSetPreparer>()
                .CutoffPrevious<ISubmitExecutor>(SubmitExecutor.Instance);
            builder.Services
                .AddScoped<T>(sp =>
                {
                    var dbContext = this.CreateDbContext(sp);
#if EF7
                    // TODO GitHubIssue#58: Figure out the equivalent measurement to suppress proxy generation in EF7.
#else
                    dbContext.Configuration.ProxyCreationEnabled = false;
#endif
                    return dbContext;
                })
                .AddScoped<DbContext>(sp => sp.GetService<T>());
            return builder;
        }

        /// <summary>
        /// Creates the underlying DbContext used by this API.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service container of the currently being created <see cref="ApiContext"/>.
        /// </param>
        /// <returns>
        /// The underlying DbContext used by this API.
        /// </returns>
        protected virtual T CreateDbContext(IServiceProvider serviceProvider)
        {
            return Activator.CreateInstance<T>();
        }
    }
}
