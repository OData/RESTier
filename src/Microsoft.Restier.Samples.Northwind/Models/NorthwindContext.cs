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
    public partial class NorthwindContext : DbContext
    {
#if EF7
        public NorthwindContext()
        {
            try
            {
                if (!Database.AsRelational().Exists())
                {
                    LoadDataSource();
                }
            }
            catch
            {
                ResetDataSource();
            }
        }

        protected override void OnConfiguring(EntityOptionsBuilder optionsBuilder)
        {
            // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
            // Seems for now EF7 can't support named connection string like "name=NorthwindConnection",
            // find an equivalent approach when it's ready.
            optionsBuilder.UseSqlServer(@"data source=(LocalDB)\v11.0;attachdbfilename=|DataDirectory|\Northwind.mdf;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework");

            base.OnConfiguring(optionsBuilder);
        }
#else
        public NorthwindContext()
            : base("name=NorthwindConnection")
        {
            if (!Database.Exists())
            {
                LoadDataSource();
            }
        }
#endif

        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Contact> Contacts { get; set; }
#if !EF7
        // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
        // Restore this property after ToTable() is supported by EF7.
        public virtual DbSet<CustomerDemographic> CustomerDemographics { get; set; }
#endif
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

            modelBuilder.ForSqlServer().UseIdentity();

            // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
            // After EF7 adds support for DataAnnotation, some of following configuration could be deprecated.
            modelBuilder.Entity<Alphabetical_list_of_product>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Alphabetical list of products");
                entityBuilder.Property(e => e.UnitPrice).ForSqlServer().ColumnType("money");
                entityBuilder.Property(e => e.ProductName).MaxLength(40);
                entityBuilder.Property(e => e.QuantityPerUnit).MaxLength(40);
                entityBuilder.Property(e => e.CategoryName).MaxLength(15);
                entityBuilder.Key(e => new
                {
                    K1 = e.ProductID,
                    K2 = e.ProductName,
                    K3 = e.Discontinued,
                    K4 = e.CategoryName,
                });
            });

            modelBuilder.Entity<Category>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Categories");
                entityBuilder.Property(e => e.CategoryName).Required().MaxLength(15);
                entityBuilder.Property(e => e.Description).ForSqlServer().ColumnType("ntext");
                entityBuilder.Property(e => e.Picture).ForSqlServer().ColumnType("image");
                entityBuilder.Property(e => e.CategoryName).MaxLength(15);
                entityBuilder.Key(e => e.CategoryID);
            });

            modelBuilder.Entity<Category_Sales_for_1997>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Category Sales for 1997");
                entityBuilder.Property(e => e.CategoryName).MaxLength(15);
                entityBuilder.Property(e => e.CategorySales).ForSqlServer().ColumnType("money");
                entityBuilder.Key(e => e.CategoryName);
            });

            modelBuilder.Entity<Contact>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Contacts");
                entityBuilder.Property(e => e.HomePage).ForSqlServer().ColumnType("ntext");
                entityBuilder.Property(e => e.Photo).ForSqlServer().ColumnType("image");
                //entityBuilder.Key(e => e.ContactID);
            });

            modelBuilder.Entity<Current_Product_List>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Current Product List");
                entityBuilder.Key(e => new
                {
                    K1 = e.ProductID,
                    K2 = e.ProductName,
                });
            });

            modelBuilder.Entity<Customer>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Customers");
                entityBuilder.Property(e => e.CompanyName).Required().MaxLength(40);
                entityBuilder.Key(e => e.CustomerID);
            });

            modelBuilder.Entity<Customer_and_Suppliers_by_City>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Customer and Suppliers by City");
                //entityBuilder.Property(e => e.CompanyName).Required().MaxLength(40);
                entityBuilder.Key(e => new
                {
                    K1 = e.CompanyName,
                    K2 = e.Relationship,
                });
            });

            modelBuilder.Entity<Employee>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Employees");
                entityBuilder.Property(e => e.LastName).Required().MaxLength(20);
                entityBuilder.Property(e => e.FirstName).Required().MaxLength(10);
                entityBuilder.Collection(e => e.Employees1).InverseReference(e => e.Employee1).Required(false).ForeignKey(e => e.ReportsTo);
            });

            modelBuilder.Entity<Invoice>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Invoices");
                entityBuilder.Property(e => e.UnitPrice).ForSqlServer().ColumnType("money");
                entityBuilder.Property(e => e.ExtendedPrice).ForSqlServer().ColumnType("money");
                entityBuilder.Property(e => e.Freight).ForSqlServer().ColumnType("money");
                entityBuilder.Key(e => new
                {
                    K1 = e.CustomerName,
                    K2 = e.Salesperson,
                    K3 = e.OrderID,
                    K4 = e.ShipperName,
                    K5 = e.ProductID,
                    K6 = e.ProductName,
                    K7 = e.UnitPrice,
                    K8 = e.Quantity,
                    K9 = e.Discount,
                });
            });

            modelBuilder.Entity<Order>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Orders");
                entityBuilder.Collection(e => e.Order_Details).InverseReference(e => e.Order).Required().ForeignKey(e => e.OrderID);
            });

            modelBuilder.Entity<Order_Detail>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Order Details");
                entityBuilder.Property(e => e.UnitPrice).ForSqlServer().ColumnType("money");
                entityBuilder.Key(e => new
                {
                    K1 = e.OrderID,
                    K2 = e.ProductID,
                });
            });

            modelBuilder.Entity<Order_Details_Extended>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Order Details Extended");
                entityBuilder.Property(e => e.UnitPrice).ForSqlServer().ColumnType("money");
                entityBuilder.Property(e => e.ExtendedPrice).ForSqlServer().ColumnType("money");
                entityBuilder.Key(e => new
                {
                    K0 = e.OrderID,
                    K1 = e.ProductID,
                    K2 = e.ProductName,
                    K3 = e.UnitPrice,
                    K4 = e.Quantity,
                    K5 = e.Discount,
                });
            });

            modelBuilder.Entity<Order_Subtotal>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Order Subtotals");
                entityBuilder.Property(e => e.Subtotal).ForSqlServer().ColumnType("money");
                entityBuilder.Key(e => e.OrderID);
            });

            modelBuilder.Entity<Orders_Qry>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Order Qry");
                entityBuilder.Property(e => e.Freight).ForSqlServer().ColumnType("money");
                entityBuilder.Key(e => new
                {
                    K0 = e.OrderID,
                    K1 = e.CompanyName,
                });
            });

            modelBuilder.Entity<Product>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Products");
                entityBuilder.Property(e => e.UnitPrice).ForSqlServer().ColumnType("money");
                entityBuilder.Collection(e => e.Order_Details).InverseReference(e => e.Product).Required();
            });

            modelBuilder.Entity<Product_Sales_for_1997>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Product Sales for 1997");
                entityBuilder.Property(e => e.ProductSales).ForSqlServer().ColumnType("money");
                entityBuilder.Key(e => new
                {
                    K0 = e.CategoryName,
                    K1 = e.ProductName,
                });
            });

            modelBuilder.Entity<Products_Above_Average_Price>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Products Above Average Price");
                entityBuilder.Property(e => e.UnitPrice).ForSqlServer().ColumnType("money");
                entityBuilder.Key(e => e.ProductName);
            });

            modelBuilder.Entity<Products_by_Category>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Products by Category");
                entityBuilder.Key(e => new
                {
                    K0 = e.CategoryName,
                    K1 = e.ProductName,
                    K2 = e.Discontinued,
                });
            });

            modelBuilder.Entity<Region>(entityBuilder =>
            {
                entityBuilder.Property(e => e.RegionDescription).Required();
                entityBuilder.Key(e => e.RegionID);
            });

            modelBuilder.Entity<Sales_by_Category>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Sales by Category");
                entityBuilder.Property(e => e.ProductSales).ForSqlServer().ColumnType("money");
                entityBuilder.Key(e => new
                {
                    K0 = e.CategoryID,
                    K1 = e.CategoryName,
                    K2 = e.ProductName,
                });
            });

            modelBuilder.Entity<Sales_Totals_by_Amount>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Sales Totals by Amount");
                entityBuilder.Property(e => e.SaleAmount).ForSqlServer().ColumnType("money");
                entityBuilder.Key(e => new
                {
                    K0 = e.OrderID,
                    K1 = e.CompanyName,
                });
            });

            modelBuilder.Entity<Shipper>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Shippers");
                entityBuilder.Collection(e => e.Orders).InverseReference(e => e.Shipper).ForeignKey(e => e.ShipVia).Required(false);
            });

            modelBuilder.Entity<Summary_of_Sales_by_Quarter>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Summary of Sales by Quarter");
                entityBuilder.Property(e => e.Subtotal).ForSqlServer().ColumnType("money");
                entityBuilder.Property(e => e.OrderID).StoreGeneratedPattern(StoreGeneratedPattern.None);
                entityBuilder.Key(e => e.OrderID);
            });

            modelBuilder.Entity<Summary_of_Sales_by_Year>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Summary of Sales by Year");
                entityBuilder.Property(e => e.Subtotal).ForSqlServer().ColumnType("money");
                entityBuilder.Property(e => e.OrderID).StoreGeneratedPattern(StoreGeneratedPattern.None);
                entityBuilder.Key(e => e.OrderID);
            });

            modelBuilder.Entity<Supplier>(entityBuilder =>
            {
            });

            modelBuilder.Entity<sysdiagram>(entityBuilder =>
            {
                entityBuilder.Property(e => e.name).Required();
                entityBuilder.Key(e => e.diagram_id);
            });

            modelBuilder.Entity<Territory>(entityBuilder =>
            {
                entityBuilder.ForSqlServer().Table("Territories");
            });

            // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
            // ToTable() is not yet supported in EF7, remove following ignores after it's ready.
            modelBuilder.Ignore<CustomerDemographic>();

            modelBuilder.Entity<Customer>().Ignore(e => e.CustomerDemographics);

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
#endif
    }
}
