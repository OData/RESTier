using System;
#if EF7
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFramework.Model;
using Microsoft.Restier.EntityFramework.Query;
using Microsoft.Restier.EntityFramework.Submit;

namespace Microsoft.Restier.EntityFramework
{
    [CLSCompliant(false)]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEfProviderServices<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            services.AddScoped<DbContext>(sp =>
            {
                var dbContext = Activator.CreateInstance<TDbContext>(); 
#if EF7
    // TODO GitHubIssue#58: Figure out the equivalent measurement to suppress proxy generation in EF7.
#else
                dbContext.Configuration.ProxyCreationEnabled = false;
#endif
                return dbContext;
            });

            return services
                .AddService<IModelBuilder, ModelProducer>()
                .AddService<IModelMapper>((sp, next) => new ModelMapper(typeof(TDbContext)))
                .AddService<IQueryExpressionSourcer, QueryExpressionSourcer>()
                .AddService<IQueryExecutor, QueryExecutor>()
                .AddService<IQueryExpressionProcessor, QueryExpressionProcessor>()
                .AddService<IChangeSetInitializer, ChangeSetInitializer>()
                .AddService<ISubmitExecutor, SubmitExecutor>();
        }
    }
}
