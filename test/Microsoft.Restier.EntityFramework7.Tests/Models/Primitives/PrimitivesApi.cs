using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Restier.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Restier.EntityFramework.Tests.Models.Primitives
{
    class PrimitivesApi : EntityFrameworkApi<PrimitivesContext>
    {
        internal PrimitivesContext DataContext
        {
            get { return (PrimitivesContext)this.DbContext; }
        }
    }

    class PrimitivesContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase();

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DateItem>().HasKey(e => e.RowId);
            modelBuilder.Entity<DateItem>().Property(e => e.DateProperty).HasColumnType("date");
            modelBuilder.Entity<DateItem>().Property(e => e.TODProperty).HasColumnType("time");
        }

        public DbSet<DateItem> Dates { get; set; }
    }

    class DateItem
    {
        [Key]
        public int RowId { get; set; }

        public DateTime? DTProperty { get; set; }

        public DateTimeOffset DTOProperty { get; set; }

        public TimeSpan TSProperty { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateProperty { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan? TODProperty { get; set; }
    }
}
