// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EF7
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
#else
using System.Data.Entity;

#endif

namespace Microsoft.OData.Service.Sample.Northwind.Models
{
    public class NorthwindContext : DbContext
    {
        static NorthwindContext()
        {
            LoadDataSource();
        }

#if EF7
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
            // Seems for now EF7 can't support named connection string like "name=NorthwindConnection",
            // find an equivalent approach when it's ready.
            optionsBuilder.UseSqlServer(@"data source=(localdb)\MSSQLLocalDB;attachdbfilename=|DataDirectory|\Northwind.mdf;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework");

            base.OnConfiguring(optionsBuilder);
        }
#else
        public NorthwindContext()
            : base("name=NorthwindConnection")
        {
        }
#endif

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
        public virtual DbSet<Territory> Territories { get; set; }

        public void ResetDataSource()
        {
#if EF7
            Database.EnsureDeleted();
#else
            if (Database.Exists())
            {
                Database.Delete();
            }
#endif
            LoadDataSource();
        }

        private static void LoadDataSource()
        {
            var dbPath = SqlLoader.GetDatabaseDirectory(null);
            var loader = new SqlLoader();
            loader.SetDatabaseEngine("(localdb)\\MSSQLLocalDB");
            loader.AddScript("instnwdb.sql");
            loader.AddScriptArgument("SqlSamplesDatabasePath", dbPath);

            // Length of database name in SQLServer cannot exceed 128.
            var dbNamePrefix = dbPath;
            if (dbNamePrefix.Length > 100)
            {
                dbNamePrefix = dbNamePrefix.Substring(dbNamePrefix.Length - 100);
                dbNamePrefix = dbNamePrefix.Substring(dbNamePrefix.IndexOf('\\') + 1);
            }

            loader.AddScriptArgument("SqlDBNamePrefix", dbNamePrefix);
            loader.Execute(dbPath);
        }

#if EF7
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ForSqlServerUseIdentityColumns();

            modelBuilder.Entity<Order_Detail>(entityBuilder =>
            {
                entityBuilder.Property(e => e.UnitPrice).HasColumnType("money");
                entityBuilder.HasKey(e => new
                {
                    K1 = e.OrderID,
                    K2 = e.ProductID,
                });
            });

            modelBuilder.Entity<Shipper>(entityBuilder =>
            {
                entityBuilder.HasMany(e => e.Orders)
                    .WithOne(e => e.Shipper)
                    .HasForeignKey(e => e.ShipVia)
                    .IsRequired(false);
            });

            modelBuilder.Entity<Order>(entityBuilder =>
            {
                entityBuilder.HasMany(e => e.Order_Details).WithOne(e => e.Order)
                    .IsRequired()
                    .HasForeignKey(e => e.OrderID).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Product>(entityBuilder =>
            {
                entityBuilder.HasMany(e => e.Order_Details)
                    .WithOne(e => e.Product)
                    .IsRequired()
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
            // ToTable() for navigation configuration is not yet supported in EF7, remove following ignores after it's ready.
            modelBuilder.Entity<Customer>().Ignore(e => e.CustomerDemographics);

            modelBuilder.Entity<CustomerDemographic>().Ignore(e => e.Customers);

            modelBuilder.Entity<Employee>().Ignore(e => e.Territories);

            modelBuilder.Entity<Territory>().Ignore(e => e.Employees);

        }
#else
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
        }
#endif
    }
}
