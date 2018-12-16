using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Samples.Northwind.AspNet.Data;

namespace Microsoft.Restier.Samples.Northwind.AspNet.Controllers
{
    public partial class NorthwindApi : EntityFrameworkApi<NorthwindEntities>
    {
        public NorthwindApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}