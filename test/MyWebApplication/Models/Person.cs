namespace MyWebApplication.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Person
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Person()
        {
            People1 = new HashSet<Person>();
            People11 = new HashSet<Person>();
            People12 = new HashSet<Person>();
            TripsTables = new HashSet<TripsTable>();
            People13 = new HashSet<Person>();
            People = new HashSet<Person>();
        }

        public long PersonId { get; set; }

        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        [StringLength(26)]
        public string LastName { get; set; }

        public long? Age { get; set; }

        public long Concurrency { get; set; }

        [Column(TypeName = "date")]
        public DateTime BirthDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? BirthDate2 { get; set; }

        public TimeSpan BirthTime { get; set; }

        public TimeSpan? BirthTime2 { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime BirthDateTime { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? BirthDateTime2 { get; set; }

        public int FavoriteFeature { get; set; }

        public int? FavoriteFeature2 { get; set; }

        public long? Cost { get; set; }

        public long? Budget { get; set; }

        public string BossOffice_Address { get; set; }

        [Required]
        [StringLength(128)]
        public string Discriminator { get; set; }

        public long? BestFriend_PersonId { get; set; }

        public long? Employee_PersonId { get; set; }

        public long? Manager_PersonId { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Person> People1 { get; set; }

        public virtual Person Person1 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Person> People11 { get; set; }

        public virtual Person Person2 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Person> People12 { get; set; }

        public virtual Person Person3 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TripsTable> TripsTables { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Person> People13 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Person> People { get; set; }
    }
}
