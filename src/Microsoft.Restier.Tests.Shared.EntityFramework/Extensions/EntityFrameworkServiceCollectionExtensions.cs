#if EF6
    using System.Data.Entity;
#endif
#if EFCore
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EFServiceCollectionExtensions
    {

#if EF6

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext => services.AddEF6ProviderServices<TDbContext>();

#endif

#if EFCore

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            services.AddEFCoreProviderServices<TDbContext>();

            if (typeof(TDbContext) == typeof(LibraryContext))
            {
                services.SeedDatabase<LibraryContext, LibraryTestInitializer>();
            }
            else if (typeof(TDbContext) == typeof(MarvelContext))
            {
                services.SeedDatabase<MarvelContext, MarvelTestInitializer>();
            }

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TInitializer"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static void SeedDatabase<TContext, TInitializer>(this IServiceCollection services)
            where TContext : DbContext
            where TInitializer : IDatabaseInitializer, new()
        {
            using var tempServices = services.BuildServiceProvider();

            var scopeFactory = tempServices.GetService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<TContext>();

            // EnsureCreated() returns false if the database already exists
            if (dbContext.Database.EnsureCreated())
            {
                var initializer = new TInitializer();
                initializer.Seed(dbContext);
            }

        }

#endif

    }

}
