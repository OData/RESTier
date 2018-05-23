namespace MyWebApplication.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Order
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PersonId { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int OrderId { get; set; }

        public double Price { get; set; }

        public string ComputedProperty { get; set; }

        public string ImmutableProperty { get; set; }

        public string NormalOrderDetail_NormalProperty { get; set; }

        public string NormalOrderDetail_AnotherNormalProperty { get; set; }

        public string NormalOrderDetail_ComputedProperty { get; set; }

        public string NormalOrderDetail_ImmutableProperty { get; set; }

        public string ComputedOrderDetail_NormalProperty { get; set; }

        public string ComputedOrderDetail_AnotherNormalProperty { get; set; }

        public string ComputedOrderDetail_ComputedProperty { get; set; }

        public string ComputedOrderDetail_ImmutableProperty { get; set; }

        public string ImmutableOrderDetail_NormalProperty { get; set; }

        public string ImmutableOrderDetail_AnotherNormalProperty { get; set; }

        public string ImmutableOrderDetail_ComputedProperty { get; set; }

        public string ImmutableOrderDetail_ImmutableProperty { get; set; }

        public string Description { get; set; }
    }
}
