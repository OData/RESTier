// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Data.Domain.EntityFramework;
using Microsoft.Data.Domain.Security;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;

namespace Microsoft.Data.Domain.Samples.Northwind.Models
{
    [EnableConventions]
    [EnableRoleBasedSecurity]
    [Grant(DomainPermissionType.All, On = "Customers")]
    [Grant(DomainPermissionType.All, On = "Products")]
    [Grant(DomainPermissionType.All, On = "CurrentOrders")]
    [Grant(DomainPermissionType.All, On = "ExpensiveProducts")]
    [Grant(DomainPermissionType.All, On = "Orders")]
    [Grant(DomainPermissionType.All, On = "Employees")]
    [Grant(DomainPermissionType.All, On = "Regions")]
    [Grant(DomainPermissionType.Inspect, On = "Suppliers")]
    [Grant(DomainPermissionType.Read, On = "Suppliers")]
    public class NorthwindDomain : DbDomain<NorthwindContext>
    {
        public NorthwindContext Context { get { return DbContext; } }

        protected EdmModel OnModelExtending(EdmModel model)
        {
            var ns = model.DeclaredNamespaces.First();
            var product = model.FindDeclaredType(ns + "." + "Product");
            var products = EdmCoreModel.GetCollection(product.GetEdmTypeReference(isNullable: false));
            var mostExpensive = new EdmFunction(ns, "MostExpensive",
                EdmCoreModel.Instance.GetPrimitive(EdmPrimitiveTypeKind.Double, isNullable: false), isBound: true,
                entitySetPathExpression: null, isComposable: false);
            mostExpensive.AddParameter("bindingParameter", products);
            model.AddElement(mostExpensive);
            return model;
        }

        // Imperative views. Currently CUD operations not supported
        protected IQueryable<Product> ExpensiveProducts
        {
            get
            {
                return this.Source<Product>("Products")
                    .Where(c => c.UnitPrice > 50);
            }
        }

        protected IQueryable<Order> CurrentOrders
        {
            get
            {
                return this.Source<Order>("Orders")
                    .Where(o => o.ShippedDate == null);
            }
        }

        // Entity set filter
        private IQueryable<Customer> OnFilterCustomers(IQueryable<Customer> customers)
        {
            return customers.Where(c => c.CountryRegion == "France");
        }

        // Submit logic
        private void OnUpdatingProducts(Product product)
        {
            WriteLog(DateTime.Now.ToString() + product.ProductID + " is being updated");
        }

        private void OnInsertedProducts(Product product)
        {
            WriteLog(DateTime.Now.ToString() + product.ProductID + " has been inserted");
        }

        private void WriteLog(string text)
        {
            // Fake writing log method for submit logic demo
        }
    }
}