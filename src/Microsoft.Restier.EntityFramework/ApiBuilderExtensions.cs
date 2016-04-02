using System;
#if EF7
using Microsoft.Data.Entity;
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
    public static class ApiBuilderExtensions
    {
        public static IServiceCollection UseDbContext<T>(this IServiceCollection obj)
            where T : DbContext
        {
            obj.TryAddScoped<T>();
            obj.TryAddScoped(typeof(DbContext), sp => sp.GetService<T>());
            return obj
                .CutoffPrevious<IModelBuilder>(ModelProducer.Instance)
                .CutoffPrevious<IModelMapper>(new ModelMapper(typeof(T)))
                .CutoffPrevious<IQueryExpressionSourcer, QueryExpressionSourcer>()
                .ChainPrevious<IQueryExecutor, QueryExecutor>()
                .CutoffPrevious<IChangeSetPreparer, ChangeSetPreparer>()
                .CutoffPrevious<ISubmitExecutor>(SubmitExecutor.Instance);
        }
    }
}
