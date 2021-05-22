using System;
using System.Linq;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.Northwind.AspNetCore;

namespace Microsoft.Restier.Samples.Northwind.AspNet.Controllers
{

    /// <summary>
    /// 
    /// </summary>
    public partial class NorthwindApi : EntityFrameworkApi<NorthwindContext>
    {

        public NorthwindApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entitySet"></param>
        /// <returns></returns>
        protected internal IQueryable<Category> OnFilterCategories(IQueryable<Category> entitySet)
        {
            //TraceEvent("CompanyEmployee", RestierOperationTypes.Filtered);
            return entitySet.Take(1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        protected internal void OnInsertingCategory(Category entity)
        {
            //CompanyEmployeeManager.OnInserting(entity);
            //TrackEvent(entity, RestierOperationTypes.Inserting);
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Inserting Category...");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        }


    }
}