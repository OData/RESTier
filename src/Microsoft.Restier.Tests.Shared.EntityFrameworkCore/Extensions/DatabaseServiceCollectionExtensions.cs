using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Tests.Shared.EntityFrameworkCore
{

    /// <summary>
    /// 
    /// </summary>
    public static class DatabaseServiceCollectionExtensions
    {

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

    }

}
