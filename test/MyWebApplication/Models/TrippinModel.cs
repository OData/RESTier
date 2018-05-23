namespace MyWebApplication.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class TrippinModel : DbContext
    {
        public TrippinModel()
            : base("name=TrippinModel")
        {
        }

        public virtual DbSet<C__MigrationHistory> C__MigrationHistory { get; set; }
        public virtual DbSet<Airline> Airlines { get; set; }
        public virtual DbSet<Airport> Airports { get; set; }
        public virtual DbSet<Conference> Conferences { get; set; }
        public virtual DbSet<Event> Events { get; set; }
        public virtual DbSet<Flight> Flights { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Person> People { get; set; }
        public virtual DbSet<Sponsor> Sponsors { get; set; }
        public virtual DbSet<Staff> Staffs { get; set; }
        public virtual DbSet<TripsTable> TripsTables { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Airline>()
                .Property(e => e.TimeStampValue)
                .IsFixedLength();

            modelBuilder.Entity<Airline>()
                .HasMany(e => e.Flights)
                .WithOptional(e => e.Airline)
                .HasForeignKey(e => e.AirlineId);

            modelBuilder.Entity<Airport>()
                .HasMany(e => e.Flights)
                .WithOptional(e => e.Airport)
                .HasForeignKey(e => e.FromId);

            modelBuilder.Entity<Airport>()
                .HasMany(e => e.Flights1)
                .WithOptional(e => e.Airport1)
                .HasForeignKey(e => e.ToId);

            modelBuilder.Entity<Conference>()
                .HasMany(e => e.Sponsors)
                .WithMany(e => e.Conferences)
                .Map(m => m.ToTable("ConferenceSponsors").MapLeftKey("ConferenceId").MapRightKey("SponsorId"));

            modelBuilder.Entity<Conference>()
                .HasMany(e => e.Sponsors1)
                .WithMany(e => e.Conferences1)
                .Map(m => m.ToTable("HighEndConferenceGlodSponsors").MapLeftKey("ConferenceId").MapRightKey("SponsorId"));

            modelBuilder.Entity<Conference>()
                .HasMany(e => e.Staffs)
                .WithMany(e => e.Conferences)
                .Map(m => m.ToTable("SeniorStaffHighEndConferences").MapLeftKey("ConferenceId").MapRightKey("StaffId"));

            modelBuilder.Entity<Conference>()
                .HasMany(e => e.Staffs1)
                .WithMany(e => e.Conferences1)
                .Map(m => m.ToTable("StaffConferences").MapLeftKey("ConferenceId").MapRightKey("StaffId"));

            modelBuilder.Entity<Flight>()
                .HasMany(e => e.TripsTables)
                .WithMany(e => e.Flights)
                .Map(m => m.ToTable("TripAndFlights").MapLeftKey("FlightId").MapRightKey("TripId"));

            modelBuilder.Entity<Person>()
                .HasMany(e => e.People1)
                .WithOptional(e => e.Person1)
                .HasForeignKey(e => e.BestFriend_PersonId);

            modelBuilder.Entity<Person>()
                .HasMany(e => e.People11)
                .WithOptional(e => e.Person2)
                .HasForeignKey(e => e.Employee_PersonId);

            modelBuilder.Entity<Person>()
                .HasMany(e => e.People12)
                .WithOptional(e => e.Person3)
                .HasForeignKey(e => e.Manager_PersonId);

            modelBuilder.Entity<Person>()
                .HasMany(e => e.People13)
                .WithMany(e => e.People)
                .Map(m => m.ToTable("PersonFriends").MapLeftKey("Friend1Id").MapRightKey("Friend2Id"));

            modelBuilder.Entity<Staff>()
                .HasMany(e => e.Staffs1)
                .WithMany(e => e.Staffs)
                .Map(m => m.ToTable("SeniorStaffPeers").MapLeftKey("StaffId1").MapRightKey("StaffId2"));

            modelBuilder.Entity<Staff>()
                .HasMany(e => e.Staffs11)
                .WithMany(e => e.Staffs2)
                .Map(m => m.ToTable("StaffPeers").MapLeftKey("StaffId1").MapRightKey("StaffId2"));

            modelBuilder.Entity<TripsTable>()
                .HasMany(e => e.Events)
                .WithOptional(e => e.TripsTable)
                .HasForeignKey(e => e.Trip_TripId);
        }
    }
}
