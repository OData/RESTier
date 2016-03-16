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
    /// Represents an API over a DbContext, which will be instantiated with a parameter-less constructor.
    /// </summary>
    /// <typeparam name="T">The DbContext type, which has a parameter-less constructor.</typeparam>
#if EF7
    [CLSCompliant(false)]
#endif
    public class DbApi<T> : DbApiBase<T>
        where T : DbContext, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbApi{T}" /> class.
        /// </summary>
        public DbApi()
        {
        }

        /// <summary>
        /// Configures <typeparamref name="T"/> to be instantiated with its parameter-less constructor,
        /// and ensures that proxy creation is suppressed under Entity Framework 6.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ApiBuilder"/> with which to create an <see cref="ApiConfiguration"/>.
        /// </param>
        /// <returns>
        /// The <see cref="ApiBuilder"/>.
        /// </returns>
        protected override ApiBuilder ConfigureApi(ApiBuilder builder)
        {
            Ensure.NotNull(builder, "builder");

            builder.Services.AddScoped(_ =>
            {
                var value = new T();
#if EF7
                // TODO GitHubIssue#58: Figure out the equivalent measurement to suppress proxy generation in EF7.
#else
                value.Configuration.ProxyCreationEnabled = false;
#endif
                return value;
            });
            return base.ConfigureApi(builder);
        }
    }

    /// <summary>
    /// Represents an API over a DbContext.
    /// </summary>
    /// <typeparam name="T">The DbContext type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class tries to instantiate <typeparamref name="T"/> with the best matched constructor
    /// base on services configured. Descendants could override by registering <typeparamref name="T"/>
    /// as a scoped service.
    /// </para>
    /// <para>
    /// To better work with <see cref="DbApiBase{T}"/>, it's recommended to suppress proxy creation in
    /// constructors of <typeparamref name="T"/>, under Entity Framework 6.
    /// </para>
    /// </remarks>
#if EF7
    [CLSCompliant(false)]
#endif
    public class DbApiBase<T> : ApiBase
        where T : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbApiBase{T}" /> class.
        /// </summary>
        public DbApiBase()
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
        /// Configures the API services for this API. Descendants may override this method to register
        /// <typeparamref name="T"/> as a scoped service.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ApiBuilder"/> with which to create an <see cref="ApiConfiguration"/>.
        /// </param>
        /// <returns>
        /// The <see cref="ApiBuilder"/>.
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
                .AddScoped<DbContext>(sp => sp.GetService<T>())
                .TryAddScoped<T>();
            return builder;
        }
    }
}
