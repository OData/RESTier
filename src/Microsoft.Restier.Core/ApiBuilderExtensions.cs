// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core.Conventions;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    [CLSCompliant(false)]
    public static class ApiBuilderExtensions
    {
        public static void DefaultInnerMost(IServiceCollection services)
        {
            services.AddSingleton<ApiTypeInfo>()
                .CutoffPrevious<IApiContextFactory, ApiContextFactory>()
                .CutoffPrevious<IQueryExecutor>(DefaultQueryExecutor.Instance);
        }

        public static void DefaultOuterMost(IServiceCollection services)
        {
            services.ChainPrevious<IApiContextFactory, ApiContextInitializer>();
            if (!services.HasService<ApiContext>())
            {
                services.AddScoped<ContextHolder>()
                    .AddScoped(sp => sp.GetService<ContextHolder>().Context);
            }
        }

        public static Action<IServiceCollection> AttributesConfiguration(Type apiType)
        {
            Ensure.NotNull(apiType, "type");

            Action<IServiceCollection> config = null;
            if (apiType.BaseType != null)
            {
                config = AttributesConfiguration(apiType.BaseType);
            }

            var attributes = apiType.GetCustomAttributes(
                typeof(IApiConfigurator), false);
            if (attributes.Length == 0)
            {
                return config;
            }

            config += services =>
            {
                foreach (IApiConfigurator e in attributes)
                {
                    e.Configure(services, apiType);
                }
            };
            return config;
        }

        public static ApiBuilder UseAttributes(this ApiBuilder obj, Type apiType)
        {
            Ensure.NotNull(obj, "obj");

            return obj.AddInnerTail(AttributesConfiguration(apiType));
        }

        public static ApiBuilder UseAttributes<TApi>(this ApiBuilder obj)
            where TApi : class
        {
            return obj.UseAttributes(typeof(TApi));
        }

        public static Action<IServiceCollection> ConventionsConfiguration(Type apiType)
        {
            Ensure.NotNull(apiType, "apiType");

            return services =>
            {
                ConventionBasedChangeSetAuthorizer.ApplyTo(services, apiType);
                ConventionBasedChangeSetEntryFilter.ApplyTo(services, apiType);
                services.CutoffPrevious<IChangeSetEntryValidator, ConventionBasedChangeSetEntryValidator>();
                ConventionBasedApiModelBuilder.ApplyTo(services, apiType);
                ConventionBasedOperationProvider.ApplyTo(services, apiType);
                ConventionBasedEntitySetFilter.ApplyTo(services, apiType);
            };
        }

        public static ApiBuilder UseConventions(this ApiBuilder obj, Type apiType)
        {
            Ensure.NotNull(obj, "obj");

            return obj.AddOuterMost(ConventionsConfiguration(apiType));
        }

        public static ApiBuilder UseConventions<TApi>(this ApiBuilder obj)
            where TApi : class
        {
            return obj.UseConventions(typeof(TApi));
        }

        public static IServiceCollection AddApiType<TApi>(this IServiceCollection obj)
            where TApi : class
        {
            obj.TryAddScoped<TApi>();
            return obj.AddInstance(new ApiTypeAdded(typeof(TApi)));
        }

        public static ApiConfiguration Build(
            this ApiBuilder obj,
            Action<IServiceCollection> innerMost,
            Action<IServiceCollection> outerMost,
            Func<IServiceCollection, IServiceProvider> serviceProviderFactory)
        {
            return obj.Build(innerMost, outerMost, new ServiceCollection(), serviceProviderFactory);
        }

        public static ApiConfiguration Build(
            this ApiBuilder obj,
            Func<IServiceCollection, IServiceProvider> serviceProviderFactory)
        {
            return obj.Build(DefaultInnerMost, DefaultOuterMost, serviceProviderFactory);
        }

        public static ApiConfiguration Build(this ApiBuilder obj)
        {
            return obj.Build(null);
        }

        private class ApiContextFactory : IApiContextFactory
        {
            public ApiContext CreateWithin(IServiceScope scope)
            {
                var context = new ApiContext(scope);
                var holder = context.GetApiService<ContextHolder>();
                if (holder != null)
                {
                    holder.Context = context;
                }

                return context;
            }
        }

        private class ApiContextInitializer : IApiContextFactory
        {
            public IApiContextFactory Inner { get; set; }

            public ApiContext CreateWithin(IServiceScope scope)
            {
                var context = Inner.CreateWithin(scope);
                foreach (var e in context.GetApiServices<IApiContextConfigurator>())
                {
                    e.Initialize(context);
                }

                return context;
            }
        }

        private class ContextHolder
        {
            public ApiContext Context { get; set; }
        }
    }
}
