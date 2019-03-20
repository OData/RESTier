namespace Microsoft.Restier.Samples.Chinook.Api.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MediaType")]
    public sealed class MediaType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public MediaType()
        {
            Tracks = new HashSet<Track>();
        }

        public int MediaTypeId { get; set; }

        [StringLength(120)]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public ICollection<Track> Tracks { get; set; }
    }
}
