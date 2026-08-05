// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{

    /// <summary>
    /// Contains extension methods of <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Returns the number of services that match the given <see cref="ServiceDescriptor.ServiceType"/> in a given <see cref="ServiceCollection"/>.
        /// </summary>
        /// <typeparam name="TService">The service type to register with the <see cref="IServiceCollection"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to register the <typeparamref name="TService"/> with.</param>
        /// <returns>
        /// An <see cref="int"/> representing the number of Services that match the given ServiceType.
        /// </returns>
        public static int HasServiceCount<TService>(this IServiceCollection services) where TService : class
        {
            Ensure.NotNull(services, nameof(services));
            return services.Count(sd => sd.ServiceType == typeof(TService));
        }

        /// <summary>
        /// Registers a chained service implementation with the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to register the service with.</param>
        /// <param name="factory">A factory that creates the service instance. The first parameter is the <see cref="IServiceProvider"/>,
        /// the second is the next (inner) service in the chain, which may be <c>null</c>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddChainedService<TService>(this IServiceCollection services,
            Func<IServiceProvider, TService, TService> factory)
            where TService : class, IChainedService<TService>
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(factory, nameof(factory));

            services.AddSingleton<IChainedService<TService>>(sp => factory(sp, default));
            return services;
        }

        internal static IServiceCollection AddRestierCoreServices(this IServiceCollection services)
        {
            Ensure.NotNull(services, nameof(services));

            services.TryAddSingleton<IChainOfResponsibilityFactory<IQueryExecutor>, DefaultChainOfResponsibilityFactory<IQueryExecutor>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IModelBuilder>, DefaultChainOfResponsibilityFactory<IModelBuilder>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IModelMapper>, DefaultChainOfResponsibilityFactory<IModelMapper>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IQueryExpressionAuthorizer>, DefaultChainOfResponsibilityFactory<IQueryExpressionAuthorizer>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IQueryExpressionSourcer>, DefaultChainOfResponsibilityFactory<IQueryExpressionSourcer>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IQueryExpressionExpander>, DefaultChainOfResponsibilityFactory<IQueryExpressionExpander>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IQueryExpressionProcessor>, DefaultChainOfResponsibilityFactory<IQueryExpressionProcessor>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IChangeSetItemAuthorizer>, DefaultChainOfResponsibilityFactory<IChangeSetItemAuthorizer>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IChangeSetItemFilter>, DefaultChainOfResponsibilityFactory<IChangeSetItemFilter>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IChangeSetItemValidator>, DefaultChainOfResponsibilityFactory<IChangeSetItemValidator>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IOperationAuthorizer>, DefaultChainOfResponsibilityFactory<IOperationAuthorizer>>();
            services.TryAddSingleton<IChainOfResponsibilityFactory<IOperationFilter>, DefaultChainOfResponsibilityFactory<IOperationFilter>>();
            services.TryAddSingleton<IChainedService<IQueryExecutor>, DefaultQueryExecutor>();
            services.TryAddSingleton<ISubmitHandler, DefaultSubmitHandler>();
            services.TryAddSingleton<IQueryHandler, DefaultQueryHandler>();
            services.TryAddSingleton<IExpandCycleDetector, DefaultExpandCycleDetector>();

            return services;
        }

        /// <summary>
        /// Enables code-based conventions for an API.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> containing API service registrations.</param>
        /// <param name="apiType">The type of a class on which code-based conventions are used.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        internal static IServiceCollection AddRestierConventionBasedServices(this IServiceCollection services, Type apiType)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(apiType, nameof(apiType));

            services.AddSingleton<IChainedService<IChangeSetItemAuthorizer>>(sp => new ConventionBasedChangeSetItemAuthorizer(apiType));
            services.AddSingleton<IChainedService<IChangeSetItemFilter>>(sp => new ConventionBasedChangeSetItemFilter(apiType));
            services.AddSingleton<IChainedService<IChangeSetItemValidator>, ConventionBasedChangeSetItemValidator>();
            services.AddSingleton<IChainedService<IQueryExpressionProcessor>>(sp => new ConventionBasedQueryExpressionProcessor(apiType));
            services.AddSingleton<IChainedService<IOperationAuthorizer>>(sp => new ConventionBasedOperationAuthorizer(apiType));
            services.AddSingleton<IChainedService<IOperationFilter>>(sp => new ConventionBasedOperationFilter(apiType));
            return services;
        }
    }
}
