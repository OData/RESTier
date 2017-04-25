// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Providers.EntityFramework;
using Microsoft.Restier.Publishers.OData.Model;

namespace Microsoft.OData.Service.Sample.Northwind.Models
{
    /// <summary>
    /// This class is only used for unqualified operation call test
    /// </summary>
    public class NorthwindApi2 : EntityFrameworkApi<NorthwindContext>
    {
        public NorthwindContext ModelContext { get { return DbContext; } }

        // Imperative views. Currently CUD operations not supported
        [Resource]
        public IQueryable<Product> ExpensiveProducts
        {
            get
            {
                return this.GetQueryableSource<Product>("Products")
                    .Where(c => c.UnitPrice > 50);
            }
        }

        [Resource]
        public IQueryable<Order> CurrentOrders
        {
            get
            {
                return this.GetQueryableSource<Order>("Orders")
                    .Where(o => o.ShippedDate == null);
            }
        }

        [Operation(IsBound = true, HasSideEffects = true)]
        public void IncreasePrice(Product bindingParameter, int diff)
        {
        }

        [Operation(IsBound = true, HasSideEffects = true)]
        public Task IncreasePriceAsync(Product bindingParameter, int diff)
        {
            return Task.FromResult(0);
        }

        [Operation(HasSideEffects = true)]
        public void ResetDataSource()
        {
        }

        [Operation(IsBound = true)]
        public double MostExpensive(IEnumerable<Product> bindingParameter)
        {
            return 0.0;
        }

        [Operation(IsBound = true)]
        public Task<double> MostExpensiveAsync(IEnumerable<Product> bindingParameter)
        {
            return Task.FromResult(0.0);
        }

        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            return EntityFrameworkApi<NorthwindContext>.ConfigureApi(apiType, services)
                .AddService<IModelBuilder, NorthwindModelExtender>()
                .AddSingleton<ODataUriResolver>(new UnqualifiedODataUriResolver());
        }

        // Entity set filter
        protected IQueryable<Customer> OnFilterCustomer(IQueryable<Customer> customers)
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

        private class NorthwindModelExtender : IModelBuilder
        {
            public IModelBuilder InnerHandler { get; set; }

            public async Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var model = await InnerHandler.GetModelAsync(context, cancellationToken);

                // enable auto-expand through model annotation.
                var orderType = (EdmEntityType)model.SchemaElements.Single(e => e.Name == "Order");
                var orderDetailsProperty = (EdmNavigationProperty)orderType.DeclaredProperties
                    .Single(prop => prop.Name == "Order_Details");
                model.SetAnnotationValue(orderDetailsProperty,
                    new QueryableRestrictionsAnnotation(new QueryableRestrictions { AutoExpand = true }));

                return model;
            }
        }

        public NorthwindApi2(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}