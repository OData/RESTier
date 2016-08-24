// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
#if EF7
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.OData.Service.Sample.Northwind.Models;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Providers.EntityFramework;
using Microsoft.Restier.Publishers.OData;
using Xunit;

namespace Microsoft.OData.Service.Sample.Northwind.Tests
{
    public class SaveTests : TestBase
    {
        private class TestEntityFilterReturnsTaskApi : NorthwindApi
        {
            /// <summary>
            /// Need to override the method here as the calling here does not call map restier route and does not get right services added.
            /// </summary>
            /// <param name="services"></param>
            /// <returns></returns>
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                // Add core and convention's services
                services = services.AddCoreServices(apiType)
                    .AddConventionBasedServices(apiType);
                // Add EF related services
                services.AddEfProviderServices<NorthwindContext>();

                // This is used to add the publisher's services
                services.AddODataServices<NorthwindApi>();
                return services;
            }

            protected async Task OnInsertingCustomers(Customer customer)
            {
                await Task.Delay(10);
                customer.CompanyName += "OnInserting";
            }

            public TestEntityFilterReturnsTaskApi(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        /// <summary>
        /// Tests an Entity Inserting Filter that returns a Task is awaited successfully.
        /// </summary>
        [Fact]
        public async Task TestEntityFilterReturnsTask()
        {
            var container = new RestierContainerBuilder(typeof(TestEntityFilterReturnsTaskApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            DataModificationItem<Customer> createCustomer = new DataModificationItem<Customer>(
                "Customers",
                typeof(Customer),
                null,
                DataModificationItemAction.Insert,
                null,
                null,
                new Dictionary<string, object>()
                {
                    {"CustomerID", "NEW01"},
                    {"CompanyName", "New Cust"},
                });

            await api.SubmitAsync(new ChangeSet(new ChangeSetItem[] { createCustomer }));

            NorthwindContext ctx = new NorthwindContext();

#if EF7
            Customer newCustomer = await ctx.Customers.FirstOrDefaultAsync(e => e.CustomerID == "NEW01");
#else
            Customer newCustomer = await ctx.Customers.FindAsync("NEW01");
#endif
            // The "OnInserting" should have been appended by the OnInsertingCustomers filter
            Assert.Equal("New CustOnInserting", newCustomer.CompanyName);

            ctx.Customers.Remove(newCustomer);
            await ctx.SaveChangesAsync();
        }
    }
}
