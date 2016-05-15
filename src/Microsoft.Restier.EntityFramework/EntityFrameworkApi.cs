﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if EF7
using Microsoft.Data.Entity;
#else
using System.Data.Entity;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

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
    public class EntityFrameworkApi<T> : ApiBase where T : DbContext
    {
        /// <summary>
        /// Gets the underlying DbContext for this API.
        /// </summary>
        protected T DbContext
        {
            get
            {
                return (T)this.Context.GetApiService<DbContext>();
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
            Type apiType = this.GetType();
            // Add core and convention's services
            services = services.AddCoreServices(apiType)
                .AddAttributeServices(apiType)
                .AddConventionBasedServices(apiType);

            // Add EF related services
            services.AddEfProviderServices<T>();

            // This is used to add the publisher's services
            ApiConfiguration.GetPublisherServiceCallback(apiType)(services);

            return services;
        }
    }
}
