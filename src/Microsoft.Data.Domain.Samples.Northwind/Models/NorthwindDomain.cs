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
            return customers.Where(c => c.Country == "France");
        }
    }
}