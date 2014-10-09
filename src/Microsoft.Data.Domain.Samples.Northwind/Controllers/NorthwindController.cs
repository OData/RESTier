// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Http;
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
            var entity = DbContext.Products.Find(key);
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
    }
}
