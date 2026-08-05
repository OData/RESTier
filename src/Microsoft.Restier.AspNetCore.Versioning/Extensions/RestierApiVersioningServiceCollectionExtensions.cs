// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore.Versioning.Internal;

// IMPORTANT (registration ordering): if the consumer calls AddApiVersioning().AddApiExplorer()
// (the canonical setup), they MUST do so BEFORE calling AddRestierApiVersioning. The composite
// IApiVersionDescriptionProvider captures the prior registration as `inner` so MVC controller
// versions still surface.

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Registers Restier API-versioning services on an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class RestierApiVersioningServiceCollectionExtensions
    {

        /// <summary>
        /// Registers the <see cref="IRestierApiVersionRegistry"/>, the
        /// <see cref="Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider"/> adapter, and an
        /// <see cref="IConfigureOptions{ODataOptions}"/> that adds versioned Restier routes when
        /// <c>ODataOptions</c> materializes.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">A delegate that declares versions via the builder.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Multiple calls to this method append additional version registrations to a single
        /// shared <see cref="IRestierApiVersioningBuilder"/>. This method does NOT use
        /// <c>TryAddSingleton</c> for the builder — it locates the existing builder
        /// <see cref="ServiceDescriptor"/> in the collection (if any) and reuses its
        /// <see cref="ServiceDescriptor.ImplementationInstance"/>.
        /// </remarks>
        public static IServiceCollection AddRestierApiVersioning(
            this IServiceCollection services,
            Action<IRestierApiVersioningBuilder> configure)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = FindOrCreateBuilder(services);
            configure(builder);

            services.TryAddSingleton<RestierApiVersionRegistry>();
            services.TryAddSingleton<IRestierApiVersionRegistry>(sp => sp.GetRequiredService<RestierApiVersionRegistry>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ODataOptions>, RestierApiVersioningOptionsConfigurator>());

            ReplaceApiVersionDescriptionProviderWithComposite(services);

            return services;
        }

        private static RestierApiVersioningBuilder FindOrCreateBuilder(IServiceCollection services)
        {
            var existing = services.FirstOrDefault(d => d.ServiceType == typeof(RestierApiVersioningBuilder));
            if (existing is not null)
            {
                if (existing.ImplementationInstance is RestierApiVersioningBuilder b)
                {
                    return b;
                }

                throw new InvalidOperationException(
                    "A RestierApiVersioningBuilder service descriptor exists but does not have an ImplementationInstance. " +
                    "AddRestierApiVersioning must register the builder via instance registration.");
            }

            var created = new RestierApiVersioningBuilder();
            services.AddSingleton(created);
            return created;
        }

        /// <summary>
        /// Replace any existing <see cref="Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider"/>
        /// registration with a composite that wraps the prior provider as <c>inner</c>. The canonical
        /// setup runs <c>AddApiVersioning().AddApiExplorer()</c> first; if so, the prior registration
        /// is Asp.Versioning's <c>DefaultApiVersionDescriptionProvider</c>, and the composite merges
        /// MVC-controller descriptions with the Restier registry's descriptions. The presence of the
        /// concrete <see cref="RestierApiVersionDescriptionProvider"/> service registration acts as
        /// a "we've already replaced" marker so subsequent <c>AddRestierApiVersioning</c> calls don't
        /// re-replace and accidentally stack composites.
        /// </summary>
        private static void ReplaceApiVersionDescriptionProviderWithComposite(IServiceCollection services)
        {
            if (services.Any(d => d.ServiceType == typeof(RestierApiVersionDescriptionProvider)))
            {
                return;
            }

            var providerType = typeof(Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider);
            var prior = services.LastOrDefault(d => d.ServiceType == providerType);
            if (prior is not null)
            {
                services.Remove(prior);
            }

            var capturedPrior = prior;
            services.AddSingleton<RestierApiVersionDescriptionProvider>(sp =>
            {
                Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider inner = null;
                if (capturedPrior is not null)
                {
                    inner = capturedPrior.ImplementationInstance as Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider
                        ?? (capturedPrior.ImplementationFactory is { } factory
                            ? (Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider)factory(sp)
                            : capturedPrior.ImplementationType is { } implType
                                ? (Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider)ActivatorUtilities.CreateInstance(sp, implType)
                                : null);
                }

                return new RestierApiVersionDescriptionProvider(
                    sp.GetRequiredService<IOptions<ODataOptions>>(),
                    sp.GetRequiredService<IRestierApiVersionRegistry>(),
                    inner);
            });

            services.AddSingleton<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>(
                sp => sp.GetRequiredService<RestierApiVersionDescriptionProvider>());
        }

    }

}
