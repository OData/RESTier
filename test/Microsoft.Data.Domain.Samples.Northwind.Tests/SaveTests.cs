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
