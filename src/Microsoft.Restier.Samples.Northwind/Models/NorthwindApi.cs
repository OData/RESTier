// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Security;

namespace Microsoft.Restier.Samples.Northwind.Models
{
    [EnableRoleBasedSecurity]
    [Grant(ApiPermissionType.All, On = "Customers")]
    [Grant(ApiPermissionType.All, On = "Products")]
    [Grant(ApiPermissionType.All, On = "CurrentOrders")]
    [Grant(ApiPermissionType.All, On = "ExpensiveProducts")]
    [Grant(ApiPermissionType.All, On = "Orders")]
    [Grant(ApiPermissionType.All, On = "Employees")]
    [Grant(ApiPermissionType.All, On = "Regions")]
    [Grant(ApiPermissionType.Inspect, On = "Suppliers")]
    [Grant(ApiPermissionType.Read, On = "Suppliers")]
    [Grant(ApiPermissionType.All, On = "ResetDataSource")]
    public class NorthwindApi : DbApi<NorthwindContext>
    {
        public NorthwindContext Context { get { return DbContext; } }

        // Imperative views. Currently CUD operations not supported
        public IQueryable<Product> ExpensiveProducts
        {
            get
            {
                return this.Source<Product>("Products")
                    .Where(c => c.UnitPrice > 50);
            }
        }

        public IQueryable<Order> CurrentOrders
        {
            get
            {
                return this.Source<Order>("Orders")
                    .Where(o => o.ShippedDate == null);
            }
        }

        [Action]
        public void IncreasePrice(Product bindingParameter, int diff)
        {
        }

        [Action]
        public void ResetDataSource()
        {
        }

        [Function]
        public double MostExpensive(IEnumerable<Product> bindingParameter)
        {
            return 0.0;
        }

        // Entity set filter
        protected IQueryable<Customer> OnFilterCustomers(IQueryable<Customer> customers)
        {
            return customers.Where(c => c.CountryRegion == "France");
        }

        // Submit logic
        protected void OnUpdatingProducts(Product product)
        {
            WriteLog(DateTime.Now.ToString() + product.ProductID + " is being updated");
        }

        protected void OnInsertedProducts(Product product)
        {
            WriteLog(DateTime.Now.ToString() + product.ProductID + " has been inserted");
        }

        private void WriteLog(string text)
        {
            // Fake writing log method for submit logic demo
        }
    }
}