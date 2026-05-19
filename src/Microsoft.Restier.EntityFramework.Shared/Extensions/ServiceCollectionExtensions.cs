// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

#if EFCore
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif

#if EFCore
namespace Microsoft.Restier.EntityFrameworkCore;
#else
namespace Microsoft.Restier.EntityFramework;
#endif

/// <summary>
/// Contains extension methods of <see cref="IServiceCollection"/>.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// This method is used to add entity framework providers service into container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>Current <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection AddEFProviderServices<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.TryAddSingleton(new RestierEFOptions());

        services.AddSingleton<IChainedService<IModelBuilder>, EFModelBuilder<TDbContext>>()
            .AddSingleton<IChainedService<IModelMapper>, EFModelMapper>()
            .AddSingleton<IChainedService<IQueryExpressionSourcer>>(sp =>
                new EFQueryExpressionSourcer(sp.GetRequiredService<RestierEFOptions>()))
            .AddSingleton<IChainedService<IQueryExecutor>, EFQueryExecutor>()
            .AddSingleton<IChainedService<IQueryExpressionProcessor>, EFQueryExpressionProcessor>()
            .AddSingleton<IChangeSetInitializer, EFChangeSetInitializer>()
            .AddSingleton<ISubmitExecutor, EFSubmitExecutor>();

        return services;
    }
}