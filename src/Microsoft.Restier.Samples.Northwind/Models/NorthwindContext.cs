// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Data.Entity;

namespace Microsoft.Restier.Samples.Northwind.Models
{
    public partial class NorthwindContext : DbContext
    {
        public NorthwindContext()
            : base("name=NorthwindConnection")
        {
        }

        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Contact> Contacts { get; set; }
        public virtual DbSet<CustomerDemographic> CustomerDemographics { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<Employee> Employees { get; set; }
        public virtual DbSet<Order_Detail> Order_Details { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Region> Regions { get; set; }
        public virtual DbSet<Shipper> Shippers { get; set; }
        public virtual DbSet<Supplier> Suppliers { get; set; }
        public virtual DbSet<sysdiagram> sysdiagrams { get; set; }
        public virtual DbSet<Territory> Territories { get; set; }
        public virtual DbSet<Alphabetical_list_of_product> Alphabetical_list_of_products { get; set; }
        public virtual DbSet<Category_Sales_for_1997> Category_Sales_for_1997 { get; set; }
        public virtual DbSet<Current_Product_List> Current_Product_Lists { get; set; }
        public virtual DbSet<Customer_and_Suppliers_by_City> Customer_and_Suppliers_by_Cities { get; set; }
        public virtual DbSet<Invoice> Invoices { get; set; }
        public virtual DbSet<Order_Details_Extended> Order_Details_Extendeds { get; set; }
        public virtual DbSet<Order_Subtotal> Order_Subtotals { get; set; }
        public virtual DbSet<Orders_Qry> Orders_Qries { get; set; }
        public virtual DbSet<Product_Sales_for_1997> Product_Sales_for_1997 { get; set; }
        public virtual DbSet<Products_Above_Average_Price> Products_Above_Average_Prices { get; set; }
        public virtual DbSet<Products_by_Category> Products_by_Categories { get; set; }
        public virtual DbSet<Sales_by_Category> Sales_by_Categories { get; set; }
        public virtual DbSet<Sales_Totals_by_Amount> Sales_Totals_by_Amounts { get; set; }
        public virtual DbSet<Summary_of_Sales_by_Quarter> Summary_of_Sales_by_Quarters { get; set; }
        public virtual DbSet<Summary_of_Sales_by_Year> Summary_of_Sales_by_Years { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerDemographic>()
                .Property(e => e.CustomerTypeID)
                .IsFixedLength();

            modelBuilder.Entity<CustomerDemographic>()
                .HasMany(e => e.Customers)
                .WithMany(e => e.CustomerDemographics)
                .Map(m => m.ToTable("CustomerCustomerDemo").MapLeftKey("CustomerTypeID").MapRightKey("CustomerID"));

            modelBuilder.Entity<Customer>()
                .Property(e => e.CustomerID)
                .IsFixedLength();

            modelBuilder.Entity<Employee>()
                .HasMany(e => e.Employees1)
                .WithOptional(e => e.Employee1)
                .HasForeignKey(e => e.ReportsTo);

            modelBuilder.Entity<Employee>()
                .HasMany(e => e.Territories)
                .WithMany(e => e.Employees)
                .Map(m => m.ToTable("EmployeeTerritories").MapLeftKey("EmployeeID").MapRightKey("TerritoryID"));

            modelBuilder.Entity<Order_Detail>()
                .Property(e => e.UnitPrice)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Order>()
                .Property(e => e.CustomerID)
                .IsFixedLength();

            modelBuilder.Entity<Order>()
                .Property(e => e.Freight)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Order>()
                .HasMany(e => e.Order_Details)
                .WithRequired(e => e.Order)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Product>()
                .Property(e => e.UnitPrice)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Product>()
                .HasMany(e => e.Order_Details)
                .WithRequired(e => e.Product)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Region>()
                .Property(e => e.RegionDescription)
                .IsFixedLength();

            modelBuilder.Entity<Region>()
                .HasMany(e => e.Territories)
                .WithRequired(e => e.Region)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Shipper>()
                .HasMany(e => e.Orders)
                .WithOptional(e => e.Shipper)
                .HasForeignKey(e => e.ShipVia);

            modelBuilder.Entity<Territory>()
                .Property(e => e.TerritoryDescription)
                .IsFixedLength();

            modelBuilder.Entity<Alphabetical_list_of_product>()
                .Property(e => e.UnitPrice)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Category_Sales_for_1997>()
                .Property(e => e.CategorySales)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Customer_and_Suppliers_by_City>()
                .Property(e => e.Relationship)
                .IsUnicode(false);

            modelBuilder.Entity<Invoice>()
                .Property(e => e.CustomerID)
                .IsFixedLength();

            modelBuilder.Entity<Invoice>()
                .Property(e => e.UnitPrice)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Invoice>()
                .Property(e => e.ExtendedPrice)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Invoice>()
                .Property(e => e.Freight)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Order_Details_Extended>()
                .Property(e => e.UnitPrice)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Order_Details_Extended>()
                .Property(e => e.ExtendedPrice)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Order_Subtotal>()
                .Property(e => e.Subtotal)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Orders_Qry>()
                .Property(e => e.CustomerID)
                .IsFixedLength();

            modelBuilder.Entity<Orders_Qry>()
                .Property(e => e.Freight)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Product_Sales_for_1997>()
                .Property(e => e.ProductSales)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Products_Above_Average_Price>()
                .Property(e => e.UnitPrice)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Sales_by_Category>()
                .Property(e => e.ProductSales)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Sales_Totals_by_Amount>()
                .Property(e => e.SaleAmount)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Summary_of_Sales_by_Quarter>()
                .Property(e => e.Subtotal)
                .HasPrecision(19, 4);

            modelBuilder.Entity<Summary_of_Sales_by_Year>()
                .Property(e => e.Subtotal)
                .HasPrecision(19, 4);
        }
    }
}
