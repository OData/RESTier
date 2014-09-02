using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.OData.Domain;
using Microsoft.Data.Domain.EntityFramework;
using Microsoft.Data.Domain.Samples.Northwind.Models;

namespace Microsoft.Data.Domain.Samples.Northwind.Controllers
{
    public class NorthwindController :
        ODataDomainController<NorthwindDomain>
    {
    }
}
