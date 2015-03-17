// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Restier.Conventions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Samples.Northwind.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Samples.Northwind.Tests
{
    [TestClass]
    public class SaveTests
    {
        [EnableConventions]
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
        [TestMethod]
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
            Assert.AreEqual("New CustOnInserting", newCustomer.CompanyName);
            
            ctx.Customers.Remove(newCustomer);
            await ctx.SaveChangesAsync();
        }
    }
}
