#if EF6
    using System.Data.Entity;
#endif
#if EFCore
    using Microsoft.EntityFrameworkCore;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EntityFrameworkServiceCollectionExtensions
    {

#if EF6
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext => services.AddEF6ProviderServices<TDbContext>();
#endif

#if EFCore
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext => services.AddEFCoreProviderServices<TDbContext>();
#endif

    }
}
