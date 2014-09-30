// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Domain;
using Microsoft.Data.Domain.Samples.Northwind.Models;
using Microsoft.Data.Domain.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NorthwindService.Tests
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
            
            Customer newCustomer = await ctx.Customers.FindAsync("NEW01");
            // The "OnInserting" should have been appended by the OnInsertingCustomers filter
            Assert.AreEqual("New CustOnInserting", newCustomer.CompanyName);
            
            ctx.Customers.Remove(newCustomer);
            await ctx.SaveChangesAsync();
        }
    }
}
