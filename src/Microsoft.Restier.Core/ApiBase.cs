// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public abstract class ApiBase : IApi
    {
        private static readonly IDictionary<Type, ApiConfiguration> Configurations =
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

        ApiContext IApi.Context
        {
            get
            {
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                return this.ApiContext;
            }
        }

        /// <summary>
        /// Gets the API configuration for this API.
        /// </summary>
        protected ApiConfiguration ApiConfiguration
        {
            get
            {
                if (this.apiConfiguration == null)
                {
                    var apiType = this.GetType();
                    ApiConfiguration configuration;
                    if (!Configurations.TryGetValue(apiType, out configuration))
                    {
                        var builder = this.ConfigureApi(new ApiBuilder());
                        ApiConfiguratorAttribute.ApplyApiBuilder(apiType, builder);

                        // Make sure that all convention-based handlers are outermost.
                        EnableConventions(builder, apiType);
                        builder.TryUseSharedApiScope(); // TODO: Maybe default to context scope?

                        configuration = this.CreateApiConfiguration(builder);
                        ApiConfiguratorAttribute.ApplyConfiguration(apiType, configuration);
                        Configurations[apiType] = configuration;
                    }

                    this.apiConfiguration = configuration;
                }

                return this.apiConfiguration;
            }
        }

        /// <summary>
        /// Gets the API context for this API.
        /// </summary>
        protected ApiContext ApiContext
        {
            get
            {
                if (this.apiContext == null)
                {
                    this.apiContext = this.CreateApiContext(
                        this.ApiConfiguration);
                    this.apiContext.SetProperty(typeof(Api).AssemblyQualifiedName, this);
                    ApiConfiguratorAttribute.ApplyInitialization(
                        this.GetType(), this, this.apiContext);
                }

                return this.apiContext;
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
            if (!this.IsDisposed && this.apiContext != null)
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
        /// <param name="builder">
        /// The <see cref="ApiBuilder"/> with which to create an <see cref="ApiConfiguration"/>.
        /// </param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        protected virtual ApiBuilder ConfigureApi(ApiBuilder builder)
        {
            return builder;
        }

        /// <summary>
        /// Creates the API configuration for this API.
        /// Descendants may override to use a customized DI container, or further configure the built
        /// <see cref="ApiConfiguration"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ApiBuilder"/> containing API service registrations.</param>
        /// <returns>
        /// An <see cref="ApiConfiguration"/> with which to create the API configuration for this API.
        /// </returns>
        protected virtual ApiConfiguration CreateApiConfiguration(ApiBuilder builder)
        {
            return builder.Build();
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
            if (this.apiContext != null)
            {
                this.apiContext.DisposeScope();
            }

            if (disposing)
            {
                this.apiContext = null;
                this.IsDisposed = true;
            }
        }

        /// <summary>
        /// Enables code-based conventions for an API.
        /// </summary>
        /// <param name="builder">
        /// An API configuration.
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
            ApiBuilder builder,
            Type targetType)
        {
            Ensure.NotNull(builder, "builder");
            Ensure.NotNull(targetType, "targetType");

            ConventionBasedChangeSetAuthorizer.ApplyTo(builder, targetType);
            ConventionBasedChangeSetEntryFilter.ApplyTo(builder, targetType);
            builder.CutoffPrevious<IChangeSetEntryValidator, ConventionBasedChangeSetEntryValidator>();
            ConventionBasedApiModelBuilder.ApplyTo(builder, targetType);
            ConventionBasedOperationProvider.ApplyTo(builder, targetType);
            ConventionBasedEntitySetFilter.ApplyTo(builder, targetType);
        }
    }
}
