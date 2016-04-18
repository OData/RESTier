using System;
using System.Data.Entity;
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
        public static IServiceCollection AddDbContextServices<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            services.TryAddScoped<TDbContext>();
            services.TryAddScoped(typeof(DbContext), sp => sp.GetService<TDbContext>());

            services.AddScoped<TDbContext>(sp =>
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
                .CutoffPrevious<IModelBuilder>(ModelProducer.Instance)
                .CutoffPrevious<IModelMapper>(new ModelMapper(typeof(TDbContext)))
                .CutoffPrevious<IQueryExpressionSourcer, QueryExpressionSourcer>()
                .ChainPrevious<IQueryExecutor, QueryExecutor>()
                .ChainPrevious<IQueryExpressionFilter, QueryExpressionFilter>()
                .CutoffPrevious<IChangeSetPreparer, ChangeSetPreparer>()
                .CutoffPrevious<ISubmitExecutor>(SubmitExecutor.Instance);
        }
    }
}
