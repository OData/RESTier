﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Security;

namespace Microsoft.Restier.Samples.Northwind.Models
{
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
    [Grant(DomainPermissionType.All, On = "ResetDataSource")]
    public class NorthwindDomain : DbDomain<NorthwindContext>
    {
        public NorthwindContext Context { get { return DbContext; } }

        protected EdmModel OnModelExtending(EdmModel model)
        {
            var ns = model.DeclaredNamespaces.First();
            var product = (IEdmEntityType)model.FindDeclaredType(ns + "." + "Product");
            var products = EdmCoreModel.GetCollection(new EdmEntityTypeReference(product, false));
            var mostExpensive = new EdmFunction(ns, "MostExpensive",
                EdmCoreModel.Instance.GetPrimitive(EdmPrimitiveTypeKind.Double, isNullable: false), isBound: true,
                entitySetPathExpression: null, isComposable: false);
            mostExpensive.AddParameter("bindingParameter", products);
            model.AddElement(mostExpensive);

            var increasePrice = new EdmAction(ns, "IncreasePrice", null, true, null);
            increasePrice.AddParameter("bindingParameter", new EdmEntityTypeReference(product as IEdmEntityType, false));
            increasePrice.AddParameter("diff", EdmCoreModel.Instance.GetInt32(false));
            model.AddElement(increasePrice);

            var resetDataSource = new EdmAction(ns, "ResetDataSource", null, false, null);
            model.AddElement(resetDataSource);
            var entityContainer = (EdmEntityContainer)model.EntityContainer;
            entityContainer.AddActionImport("ResetDataSource", resetDataSource);
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