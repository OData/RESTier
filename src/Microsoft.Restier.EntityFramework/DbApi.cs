// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if EF7
using Microsoft.Data.Entity;
#else
using System.Data.Entity;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// <remarks>
    /// <para>
    /// This class tries to instantiate <typeparamref name="T"/> with the best matched constructor
    /// base on services configured. Descendants could override by registering <typeparamref name="T"/>
    /// as a scoped service. But in this case, proxy creation must be disabled in the constructors of
    /// <typeparamref name="T"/> under Entity Framework 6.
    /// </para>
    /// </remarks>
#if EF7
    [CLSCompliant(false)]
#endif
    public class DbApi<T> : ApiBase where T : DbContext
    {
        /// <summary>
        /// Gets the underlying DbContext for this API.
        /// </summary>
        protected T DbContext
        {
            get
            {
                return this.Context.GetApiService<T>();
            }
        }

        /// <summary>
        /// Configures the API services for this API. Descendants may override this method to register
        /// <typeparamref name="T"/> as a scoped service.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> with which to create an <see cref="ApiConfiguration"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        [CLSCompliant(false)]
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .CutoffPrevious<IModelBuilder>(ModelProducer.Instance)
                .CutoffPrevious<IModelMapper>(new ModelMapper(typeof(T)))
                .CutoffPrevious<IQueryExpressionSourcer, QueryExpressionSourcer>()
                .ChainPrevious<IQueryExecutor, QueryExecutor>()
                .CutoffPrevious<IChangeSetPreparer, ChangeSetPreparer>()
                .CutoffPrevious<ISubmitExecutor>(SubmitExecutor.Instance)
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
