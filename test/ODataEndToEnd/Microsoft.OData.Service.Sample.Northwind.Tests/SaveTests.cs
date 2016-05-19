// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
#if EF7
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.OData.Service.Sample.Northwind.Models;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Xunit;

namespace Microsoft.OData.Service.Sample.Northwind.Tests
{
    public class SaveTests : TestBase
    {
        private class TestEntityFilterReturnsTaskApi : NorthwindApi
        {
            protected async Task OnInsertingCustomers(Customer customer)
            {
                await Task.Delay(10);
                customer.CompanyName += "OnInserting";
            }
        }

        /// <summary>
        /// Tests an Entity Inserting Filter that returns a Task is awaited successfully.
        /// </summary>
        [Fact]
        public async Task TestEntityFilterReturnsTask()
        {
            TestEntityFilterReturnsTaskApi api = new TestEntityFilterReturnsTaskApi();
            DataModificationItem<Customer> createCustomer = new DataModificationItem<Customer>(
                "Customers",
                "Customer",
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
