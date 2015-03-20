// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EF7
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Update;
#else
using System.Data.Entity.Infrastructure;
#endif
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;
using Microsoft.Restier.Samples.Northwind.Models;
using Microsoft.Restier.WebApi;

namespace Microsoft.Restier.Samples.Northwind.Controllers
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

        // OData Attribute Routing
        [ODataRoute("Customers({key})/CompanyName")]
        [ODataRoute("Customers({key})/CompanyName/$value")]
        public string GetCustomerCompanyName([FromODataUri]string key)
        {
            return DbContext.Customers.Where(c => c.CustomerID == key).Select(c => c.CompanyName).FirstOrDefault();
        }

        [ODataRoute("Products/$count")]
        public IHttpActionResult GetProductsCount()
        {
            return Ok(DbContext.Products.Count());
        }

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

        [HttpGet]
        [ODataRoute("Products/Microsoft.Restier.Samples.Northwind.Models.MostExpensive")]
        public IHttpActionResult MostExpensive()
        {
            var product = DbContext.Products.Max(p => p.UnitPrice);
            return Ok(product);
        }

        [HttpPost]
        [ODataRoute("Products({key})/Microsoft.Restier.Samples.Northwind.Models.IncreasePrice")]
        public IHttpActionResult IncreasePrice([FromODataUri] int key, ODataActionParameters parameters)
        {
            var entity = DbContext.Products.FirstOrDefault(e => e.ProductID == key);
            if (entity == null)
            {
                return NotFound();
            }
            entity.UnitPrice = entity.UnitPrice + (int)parameters["diff"];

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
            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}
