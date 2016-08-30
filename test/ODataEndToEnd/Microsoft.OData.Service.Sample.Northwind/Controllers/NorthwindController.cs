// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EF7
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
#else
using System.Data.Entity.Infrastructure;
#endif
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Service.Sample.Northwind.Models;
using Microsoft.Restier.Core;

namespace Microsoft.OData.Service.Sample.Northwind.Controllers
{
    public class NorthwindController : ODataController
    {
        private NorthwindContext DbContext
        {
            get
            {
                var api =(NorthwindApi)this.Request.GetRequestContainer().GetService<ApiBase>();
                return api.ModelContext;
            }
        }

        // OData Attribute Routing
        [HttpPut]
        [ODataRoute("Products({key})/UnitPrice")]
        public IHttpActionResult UpdateProductUnitPrice(int key, [FromBody]decimal price)
        {
            var entity = DbContext.Products.FirstOrDefault(e => e.ProductID == key);
            if (entity == null)
            {
                return NotFound();
            }
            entity.UnitPrice = price;

            try
            {
                DbContext.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DbContext.Products.Any(p => p.ProductID == key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Ok(price);
        }

        [HttpPost]
        [ODataRoute("ResetDataSource")]
        public IHttpActionResult ResetDataSource()
        {
            DbContext.ResetDataSource();
            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}
