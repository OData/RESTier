namespace Microsoft.Restier.Samples.Chinook.Api.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("InvoiceLine")]
    public class InvoiceLine
    {
        public int InvoiceLineId { get; set; }

        public int InvoiceId { get; set; }

        public int TrackId { get; set; }

        [Column(TypeName = "numeric")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public virtual Invoice Invoice { get; set; }

        public virtual Track Track { get; set; }
    }
}
