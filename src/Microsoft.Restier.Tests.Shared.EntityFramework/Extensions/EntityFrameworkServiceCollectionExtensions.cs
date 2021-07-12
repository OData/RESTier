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
    public static class EntityFrameworkServiceCollectionExtensions
    {

#if EF6
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext => services.AddEF6ProviderServices<TDbContext>();
#endif

#if EFCore
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            services.AddEFCoreProviderServices<TDbContext>();

            if (typeof(TDbContext) == typeof(LibraryContext) || typeof(TDbContext).BaseType == typeof(LibraryContext))
            {
                services.SeedDatabase<LibraryContext, LibraryTestInitializer>();
            }
            else if (typeof(TDbContext) == typeof(MarvelContext))
            {
                services.SeedDatabase<MarvelContext, MarvelTestInitializer>();
            }

            return services;
        }
#endif

    }
}
