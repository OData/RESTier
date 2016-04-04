// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Conventions;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents a base class for an API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An API configuration is intended to be long-lived, and can be
    /// statically cached according to an API type specified when the
    /// configuration is created. Additionally, the API model produced
    /// as a result of a particular configuration is cached under the same
    /// API type to avoid re-computing it on each invocation.
    /// </para>
    /// </remarks>
    public abstract class ApiBase : IDisposable
    {
        private static readonly ConcurrentDictionary<Type, ApiConfiguration> Configurations =
            new ConcurrentDictionary<Type, ApiConfiguration>();

        private ApiConfiguration apiConfiguration;
        private ApiContext apiContext;

        /// <summary>
        /// Finalizes an instance of the <see cref="ApiBase"/> class.
        /// </summary>
        ~ApiBase()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the API context for this API.
        /// </summary>
        public ApiContext Context
        {
            get
            {
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                if (this.apiContext == null)
                {
                    this.apiContext = this.CreateApiContext(
                        this.Configuration);
                    var apiScope = this.apiContext.GetApiService<ApiHolder>();
                    if (apiScope != null)
                    {
                        apiScope.Api = this;
                    }

                    ApiConfiguratorAttribute.ApplyInitialization(
                        this.GetType(), this, this.apiContext);
                }

                return this.apiContext;
            }
        }

        /// <summary>
        /// Gets the API configuration for this API.
        /// </summary>
        protected ApiConfiguration Configuration
        {
            get
            {
                if (this.apiConfiguration != null)
                {
                    return this.apiConfiguration;
                }

                return this.apiConfiguration = Configurations.GetOrAdd(
                    this.GetType(),
                    apiType =>
                    {
                        IServiceCollection services = new ServiceCollection()
                            .CutoffPrevious<IQueryExecutor>(DefaultQueryExecutor.Instance)
                            .AddSingleton<ApiConfigurationExtensions.PropertyBag>()
                            .AddScoped<ApiContextExtensions.PropertyBag>()
                            .AddScoped<InvocationContextExtensions.PropertyBag>();
                        services = this.ConfigureApi(services);
                        ApiConfiguratorAttribute.ApplyApiServices(apiType, services);

                        // Copy from pre-build registration.
                        ApiConfiguration.Configuration(apiType)(services);

                        // Make sure that all convention-based handlers are outermost.
                        EnableConventions(services, apiType);
                        if (!services.HasService<ApiBase>())
                        {
                            services.AddScoped<ApiHolder>()
                                .AddScoped(apiType, sp => sp.GetService<ApiHolder>().Api)
                                .AddScoped(sp => sp.GetService<ApiHolder>().Api)
                                .AddScoped(sp => sp.GetService<ApiHolder>().Api.Context);
                        }

                        var configuration = this.CreateApiConfiguration(services);
                        ApiConfiguratorAttribute.ApplyConfiguration(apiType, configuration);
                        return configuration;
                    });
            }
        }

        /// <summary>
        /// Gets a value indicating whether this API has been disposed.
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// Performs application-defined tasks associated with
        /// freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.apiContext != null)
            {
                ApiConfiguratorAttribute.ApplyDisposal(
                    this.GetType(), this, this.apiContext);
            }

            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Configure services for this API.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> with which to create an <see cref="ApiConfiguration"/>.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        [CLSCompliant(false)]
        protected virtual IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return services;
        }

        /// <summary>
        /// Creates the API configuration for this API.
        /// Descendants may override to use a customized DI container, or further configure the built
        /// <see cref="ApiConfiguration"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> containing API service registrations.</param>
        /// <returns>
        /// An <see cref="ApiConfiguration"/> with which to create the API configuration for this API.
        /// </returns>
        [CLSCompliant(false)]
        protected virtual ApiConfiguration CreateApiConfiguration(IServiceCollection services)
        {
            return services.BuildApiConfiguration();
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
        protected virtual ApiContext CreateApiContext(
            ApiConfiguration configuration)
        {
            return new ApiContext(configuration);
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
            this.IsDisposed = true;
            if (this.apiContext != null)
            {
                this.apiContext.DisposeScope();
                this.apiContext = null;
            }
        }

        /// <summary>
        /// Enables code-based conventions for an API.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> containing API service registrations.
        /// </param>
        /// <param name="targetType">
        /// The type of a class on which code-based conventions are used.
        /// </param>
        /// <remarks>
        /// This method adds hook points to the API configuration that
        /// inspect a target type for a variety of code-based conventions
        /// such as usage of specific attributes or members that follow
        /// certain naming conventions.
        /// </remarks>
        private static void EnableConventions(
            IServiceCollection services,
            Type targetType)
        {
            Ensure.NotNull(services, "services");
            Ensure.NotNull(targetType, "targetType");

            ConventionBasedChangeSetAuthorizer.ApplyTo(services, targetType);
            ConventionBasedChangeSetEntryFilter.ApplyTo(services, targetType);
            services.CutoffPrevious<IChangeSetEntryValidator, ConventionBasedChangeSetEntryValidator>();
            ConventionBasedApiModelBuilder.ApplyTo(services, targetType);
            ConventionBasedOperationProvider.ApplyTo(services, targetType);
            ConventionBasedEntitySetFilter.ApplyTo(services, targetType);
        }

        // Registered as a scoped service so that IApi and ApiContext could be exposed as scoped service.
        // If a descendant class wants to expose these 2 services in another way, it must ensure they could be
        // resolved after CreateApiContext call.
        private class ApiHolder
        {
            public ApiBase Api { get; set; }
        }
    }
}
