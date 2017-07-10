using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Providers.EntityFramework;

namespace Microsoft.Restier.Providers.EntityFramework7.Tests.Models.Primitives
{
    class PrimitivesApi : EntityFrameworkApi<PrimitivesContext>
    {
        public PrimitivesApi(IServiceProvider sp) : base(sp)
        {
        }

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
