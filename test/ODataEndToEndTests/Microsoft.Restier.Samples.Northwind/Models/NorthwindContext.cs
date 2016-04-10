// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EF7
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
#else
using System.Data.Entity;
#endif

namespace Microsoft.Restier.Samples.Northwind.Models
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
            loader.Execute(dbPath);
        }

#if EF7
        protected override void OnModelCreating(Data.Entity.ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ForSqlServerUseIdentityColumns();

            // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
            // After EF7 adds support for DataAnnotation, some of following configuration could be deprecated.

            modelBuilder.Entity<Category>(entityBuilder =>
            {
                entityBuilder.ToTable("Categories");
                entityBuilder.Property(e => e.CategoryName).IsRequired().HasMaxLength(15);
                entityBuilder.Property(e => e.Description).HasColumnType("ntext");
                entityBuilder.Property(e => e.Picture).HasColumnType("image");
                entityBuilder.Property(e => e.CategoryName).HasMaxLength(15);
                entityBuilder.HasKey(e => e.CategoryID);
            });


            modelBuilder.Entity<Contact>(entityBuilder =>
            {
                entityBuilder.ToTable("Contacts");
                entityBuilder.Property(e => e.HomePage).HasColumnType("ntext");
                entityBuilder.Property(e => e.Photo).HasColumnType("image");
                //entityBuilder.Key(e => e.ContactID);
            });

            modelBuilder.Entity<Customer>(entityBuilder =>
            {
                entityBuilder.ToTable("Customers");
                entityBuilder.Property(e => e.CompanyName).IsRequired().HasMaxLength(40);
                entityBuilder.HasKey(e => e.CustomerID);
            });

            modelBuilder.Entity<CustomerDemographic>(entityBuilder =>
            {
                entityBuilder.ToTable("CustomerDemographics");
            });

            modelBuilder.Entity<Employee>(entityBuilder =>
            {
                entityBuilder.ToTable("Employees");
                entityBuilder.Property(e => e.LastName).IsRequired().HasMaxLength(20);
                entityBuilder.Property(e => e.FirstName).IsRequired().HasMaxLength(10);
				entityBuilder.Property(e => e.BirthDate).HasColumnType("date");
				entityBuilder.Property(e => e.HireDate).HasColumnType("date");
				entityBuilder.HasMany(e => e.Employees1)
                    .WithOne(e => e.Employee1)
                    .IsRequired(false)
                    .HasForeignKey(e => e.ReportsTo);
            });

            modelBuilder.Entity<Order>(entityBuilder =>
            {
                entityBuilder.ToTable("Orders");
				entityBuilder.Property(e => e.OrderDate).HasColumnType("date");
				entityBuilder.Property(e => e.RequiredDate).HasColumnType("date");
				entityBuilder.Property(e => e.ShippedDate).HasColumnType("date");
				entityBuilder.HasMany(e => e.Order_Details).WithOne(e => e.Order)
                    .IsRequired()
                    .HasForeignKey(e => e.OrderID).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Order_Detail>(entityBuilder =>
            {
                entityBuilder.ToTable("Order Details");
                entityBuilder.Property(e => e.UnitPrice).HasColumnType("money");
                entityBuilder.HasKey(e => new
                {
                    K1 = e.OrderID,
                    K2 = e.ProductID,
                });
            });

            modelBuilder.Entity<Product>(entityBuilder =>
            {
                entityBuilder.ToTable("Products");
                entityBuilder.Property(e => e.UnitPrice).HasColumnType("money");
                entityBuilder.HasMany(e => e.Order_Details)
                    .WithOne(e => e.Product)
                    .IsRequired()
                    .HasForeignKey(e => e.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Region>(entityBuilder =>
            {
                entityBuilder.Property(e => e.RegionDescription).IsRequired();
                entityBuilder.HasKey(e => e.RegionID);
            });

            modelBuilder.Entity<Shipper>(entityBuilder =>
            {
                entityBuilder.ToTable("Shippers");
                entityBuilder.HasMany(e => e.Orders)
                    .WithOne(e => e.Shipper)
                    .HasForeignKey(e => e.ShipVia)
                    .IsRequired(false);
            });

            modelBuilder.Entity<Supplier>(entityBuilder =>
            {
            });

            modelBuilder.Entity<Territory>(entityBuilder =>
            {
                entityBuilder.ToTable("Territories");
                entityBuilder.HasOne(e => e.Region)
                    .WithMany(e => e.Territories)
                    .HasForeignKey(e => e.RegionID)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
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
