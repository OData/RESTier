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

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Microsoft.Data.Domain.EntityFramework;
using Microsoft.Data.Domain.Security;

namespace Microsoft.Data.Domain.Samples.Northwind.Models
{
    [EnableConventions]
    [EnableRoleBasedSecurity]
    [Grant(DomainPermissionType.Inspect)]
    [Grant(DomainPermissionType.All, On = "Customers")]
    [Grant(DomainPermissionType.All, On = "Employees")]
    [Grant(DomainPermissionType.All, On = "CurrentOrders")]
    [Grant(DomainPermissionType.All, On = "Orders")] //, To = "Manager")]
    [Grant(DomainPermissionType.All, On = "Products")]
    public class NorthwindDomain : DbDomain<NorthwindContext>
    {
        public NorthwindContext Context { get { return DbContext; } }

        // [Assert("Manager")]
        protected IQueryable<Order> CurrentOrders
        {
            get
            {
                return this.Source<Order>("Orders")
                    .Where(o => o.ShippedDate == null);
            }
        }

        private IQueryable<Customer> OnFilterCustomers(IQueryable<Customer> customers)
        {
            return customers.Where(c => c.CountryRegion == "France");
        }
    }
}