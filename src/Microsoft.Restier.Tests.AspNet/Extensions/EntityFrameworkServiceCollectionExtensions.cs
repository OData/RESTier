#if EF6
    using System.Data.Entity;
#endif
#if EFCore
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EntityFrameworkServiceCollectionExtensions
    {

#if EF6
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext => services.AddEF6ProviderServices<TDbContext>();
#endif

#if EFCore
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            services.AddEFCoreProviderServices<TDbContext>();

            // initialize the context in a new scope
            using var tempServices = services.BuildServiceProvider();

            var scopeFactory = tempServices.GetService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            // JHC TODO: work out how to replace this with an interface instead of this ridiculousness

            if (typeof(TDbContext) == typeof(LibraryContext))
            {
                var dbContext = scope.ServiceProvider.GetService<LibraryContext>();

                // EnsureCreated() returns false if the database already exists
                if (dbContext.Database.EnsureCreated())
                {
                    var initializer = new LibraryTestInitializer();
                    initializer.Seed(dbContext);
                }

            }
            else if (typeof(TDbContext) == typeof(MarvelContext))
            {
                var dbContext = scope.ServiceProvider.GetService<MarvelContext>();

                // EnsureCreated() returns false if the database already exists
                if (dbContext.Database.EnsureCreated())
                {
                    var initializer = new MarvelTestInitializer();
                    initializer.Seed(dbContext);
                }

            }

            return services;
        }
#endif

    }
}
