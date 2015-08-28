// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
#if EF7
using Microsoft.Data.Entity;
#endif
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Samples.Northwind.Models;
using Xunit;

namespace Microsoft.Restier.Samples.Northwind.Tests
{
    public class SaveTests : TestBase
    {
        private class TestEntityFilterReturnsTaskDomain : NorthwindDomain
        {
            private async Task OnInsertingCustomers(Customer customer)
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
            TestEntityFilterReturnsTaskDomain domain = new TestEntityFilterReturnsTaskDomain();
            DataModificationEntry<Customer> createCustomer = new DataModificationEntry<Customer>(
                "Customers",
                "Customer",
                null,
                null,
                new Dictionary<string, object>()
                {
                    {"CustomerID", "NEW01"},
                    {"CompanyName", "New Cust"},
                });

            await domain.SubmitAsync(new ChangeSet(new ChangeSetEntry[] { createCustomer }));

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
