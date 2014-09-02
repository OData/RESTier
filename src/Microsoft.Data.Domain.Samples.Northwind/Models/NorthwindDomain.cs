using System;
using System.Collections.Generic;
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
    [Grant(DomainPermissionType.All, On = "Orders", To = "Manager")]
    public class NorthwindDomain : DbDomain<NorthwindContext>
    {
        [Assert("Manager")]
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