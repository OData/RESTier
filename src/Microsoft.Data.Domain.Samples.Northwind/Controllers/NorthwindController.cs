using System.Linq;
using System.Web.OData;
using System.Web.OData.Domain;
using System.Web.OData.Routing;
using Microsoft.Data.Domain.Samples.Northwind.Models;

namespace Microsoft.Data.Domain.Samples.Northwind.Controllers
{
    public class NorthwindController :
        ODataDomainController<NorthwindDomain>
    {
        private NorthwindContext DbContext
        {
            get
            {
                return Domain.Context;
            }
        }

        [ODataRoute("Customers({key})/CompanyName")]
        public string GetCustomerCompanyName([FromODataUri]string key)
        {
            return DbContext.Customers.Where(c => c.CustomerID == key).Select(c => c.CompanyName).FirstOrDefault();
        }
    }
}
