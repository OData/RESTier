// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    public class TrippinModel : DbContext
    {
        static TrippinModel()
        {
            Database.SetInitializer(new TrippinDatabaseInitializer());
        }

        public TrippinModel()
            : base("name=TrippinModel")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Airline>().HasKey(a =>a.AirlineCode);

            modelBuilder.Entity<Trip>().HasMany<Flight>(s => s.Flights).WithMany().Map(c =>
            {
                c.MapLeftKey("TripId");
                c.MapRightKey("FlightId");
                c.ToTable("TripAndFlights");
            });

            modelBuilder.Entity<Person>().HasMany<Person>(p => p.Friends).WithMany().Map(
                c =>
                {
                    c.MapLeftKey("Friend1Id");
                    c.MapRightKey("Friend2Id");
                    c.ToTable("PersonFriends");
                }
            );

            modelBuilder.Entity<Staff>().HasMany<Staff>(s => s.PeerStaffs).WithMany().Map(c =>
            {
                c.MapLeftKey("StaffId1");
                c.MapRightKey("StaffId2");
                c.ToTable("StaffPeers");
            });

            modelBuilder.Entity<Staff>().HasMany<Conference>(s => s.Conferences).WithMany().Map(c =>
            {
                c.MapLeftKey("StaffId");
                c.MapRightKey("ConferenceId");
                c.ToTable("StaffConferences");
            });

            modelBuilder.Entity<SeniorStaff>().HasMany<SeniorStaff>(s => s.PeerSeniorStaffs).WithMany().Map(c =>
            {
                c.MapLeftKey("StaffId1");
                c.MapRightKey("StaffId2");
                c.ToTable("SeniorStaffPeers");
            });

            modelBuilder.Entity<SeniorStaff>().HasMany<HighEndConference>(s => s.HighEndConferences).WithMany().Map(c =>
            {
                c.MapLeftKey("StaffId");
                c.MapRightKey("ConferenceId");
                c.ToTable("SeniorStaffHighEndConferences");
            });

            modelBuilder.Entity<Conference>().HasMany<Sponsor>(s => s.Sponsors).WithMany().Map(c =>
            {
                c.MapLeftKey("ConferenceId");
                c.MapRightKey("SponsorId");
                c.ToTable("ConferenceSponsors");
            });

            modelBuilder.Entity<HighEndConference>().HasMany<GlodSponsor>(s => s.GlodSponsors).WithMany().Map(c =>
            {
                c.MapLeftKey("ConferenceId");
                c.MapRightKey("SponsorId");
                c.ToTable("HighEndConferenceGlodSponsors");
            });

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Person> People { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Airline> Airlines { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Conference> Conferences { get; set; }
        public DbSet<Sponsor> Sponsors { get; set; }

        private static TrippinModel instance;
        public static TrippinModel Instance
        {
            get
            {
                if (instance == null)
                {
                    ResetDataSource();
                }
                return instance;
            }
        }

        public static void ResetDataSource()
        {
            instance = new TrippinModel();

            // As per kb321843, manually handle dropping Friends constraint before cleaning up People table.
            instance.Database.ExecuteSqlCommand("DELETE FROM PersonFriends");

            // Discard all local changes, and reload data from DB, them remove all
            foreach (var x in instance.People)
            {
                // Discard local changes for the person..
                instance.Entry(x).State = EntityState.Detached;
            }

            instance.Database.ExecuteSqlCommand("DELETE FROM StaffPeers");
            instance.Database.ExecuteSqlCommand("DELETE FROM SeniorStaffPeers");

            foreach (var x in instance.Staffs)
            {
                // Discard local changes for the person..
                instance.Entry(x).State = EntityState.Detached;
            }

            instance.People.RemoveRange(instance.People);
            instance.Orders.RemoveRange(instance.Orders);
            instance.Flights.RemoveRange(instance.Flights);
            instance.Airlines.RemoveRange(instance.Airlines);
            instance.Airports.RemoveRange(instance.Airports);
            instance.Trips.RemoveRange(instance.Trips);
            instance.Events.RemoveRange(instance.Events);
            instance.Staffs.RemoveRange(instance.Staffs);
            instance.Conferences.RemoveRange(instance.Conferences);
            instance.Sponsors.RemoveRange(instance.Sponsors);

            // This is to set the People Id from 0
            instance.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('People', RESEED, 0)");
            instance.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Flights', RESEED, 0)");
            instance.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('TripsTable', RESEED, 0)");
            instance.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Staffs', RESEED, 0)");
            instance.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Conferences', RESEED, 0)");
            instance.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Sponsors', RESEED, 0)");

            instance.SaveChanges();

            #region Airports

            var airports = new List<Airport>
            {
                new Airport
                {
                    Name = "San Francisco International Airport",
                    IataCode = "SFO",
                    IcaoCode = "KSFO"
                },
                new Airport
                {
                    Name = "Los Angeles International Airport",
                    IataCode = "LAX",
                    IcaoCode = "KLAX"
                },
                new Airport
                {
                    Name = "Shanghai Hongqiao International Airport",
                    IataCode = "SHA",
                    IcaoCode = "ZSSS"
                },
                new Airport
                {
                    Name = "Beijing Capital International Airport",
                    IataCode = "PEK",
                    IcaoCode = "ZBAA"
                },
                new Airport
                {
                    Name = "John F. Kennedy International Airport",
                    IataCode = "JFK",
                    IcaoCode = "KJFK"
                },
                new Airport
                {
                    Name = "Rome Ciampino Airport",
                    IataCode = "CIA",
                    IcaoCode = "LIRA"
                },
                new Airport
                {
                    Name = "Toronto Pearson International Airport",
                    IataCode = "YYZ",
                    IcaoCode = "CYYZ"
                },
                new Airport
                {
                    Name = "Sydney Airport",
                    IataCode = "SYD",
                    IcaoCode = "YSSY"
                },
                new Airport
                {
                    Name = "Istanbul Ataturk Airport",
                    IataCode = "IST",
                    IcaoCode = "LTBA"
                },
                new Airport
                {
                    Name = "Singapore Changi Airport",
                    IataCode = "SIN",
                    IcaoCode = "WSSS"
                },
                new Airport
                {
                    Name = "Abu Dhabi International Airport",
                    IataCode = "AUH",
                    IcaoCode = "OMAA"
                },
                new Airport
                {
                    Name = "Guangzhou Baiyun International Airport",
                    IataCode = "CAN",
                    IcaoCode = "ZGGG"
                },
                new Airport
                {
                    Name = "O'Hare International Airport",
                    IataCode = "ORD",
                    IcaoCode = "KORD"
                },
                new Airport
                {
                   Name = "Hartsfield-Jackson Atlanta International Airport",
                   IataCode = "ATL",
                   IcaoCode = "KATL"
                },
                new Airport
                {
                    Name = "Seattle-Tacoma International Airport",
                    IataCode = "SEA",
                    IcaoCode = "KSEA"
                }
            };
            instance.Airports.AddRange(airports);

            #endregion

            #region Airlines

            var airlines = new List<Airline>
            {
                new Airline
                {
                    Name = "American Airlines",
                    AirlineCode = "AA" 
                },

                new Airline
                {
                    Name = "Shanghai Airline",
                    AirlineCode = "FM"
                },

                new Airline
                {
                    Name = "China Eastern Airlines",
                    AirlineCode = "MU"
                },

                new Airline
                {
                    Name = "Air France",
                    AirlineCode = "AF"
                },

                new Airline
                {
                    Name = "Alitalia",
                    AirlineCode = "AZ"
                },

                new Airline
                {
                    Name = "Air Canada",
                    AirlineCode = "AC"
                },

                new Airline
                {
                    Name = "Austrian Airlines",
                    AirlineCode = "OS"
                },

                new Airline
                {
                    Name = "Turkish Airlines",
                    AirlineCode = "TK"
                },

                new Airline
                {
                    Name = "Japan Airlines",
                    AirlineCode = "JL"
                },

                new Airline
                {
                    Name = "Singapore Airlines",
                    AirlineCode = "SQ"
                },

                new Airline
                {
                    Name = "Korean Air",
                    AirlineCode = "KE"
                },

                new Airline
                {
                    Name = "China Southern",
                    AirlineCode = "CZ"
                },

                new Airline
                {
                    Name = "AirAsia",
                    AirlineCode = "AK"
                },

                new Airline
                {
                    Name = "Hong Kong Airlines",
                    AirlineCode = "HX"
                },

                new Airline
                {
                    Name = "Emirates",
                    AirlineCode = "EK"
                },

                new Airline
                {
                    Name = "Slash%2F",
                    AirlineCode = "S/"
                },

                new Airline
                {
                    Name = "BackSlash%5C",
                    AirlineCode = "BS\\"
                }
            };
            instance.Airlines.AddRange(airlines);

            #endregion

            #region People

            #region Friends russellwhyte & scottketchum & ronaldmundy

            var person0 = new Person
            {
                PersonId = 0,
                FirstName = "Russell",
                LastName = "Whyte",
                UserName = "russellwhyte",
                BirthDate = new DateTime(1980, 10, 15),
                BirthTime = new TimeSpan(2, 3, 4),
                BirthDateTime = new DateTime(1980, 10, 15, 2, 3, 4),
                FavoriteFeature = Feature.Feature1,
            };

            var person1 = new Person
            {
                PersonId = 1,
                FirstName = "Scott",
                LastName = "Ketchum",
                UserName = "scottketchum",
                BirthDate = new DateTime(1983, 11, 12),
                BirthTime = new TimeSpan(1, 2, 3),
                BirthDateTime = new DateTime(1983, 11, 12, 1, 2, 3),
                FavoriteFeature = Feature.Feature2,
                Friends = new Collection<Person> { person0 }
            };

            var person2 = new Person
            {
                PersonId = 2,
                FirstName = "Ronald",
                LastName = "Mundy",
                UserName = "ronaldmundy",
                BirthDate = new DateTime(1984, 12, 11),
                BirthTime = new TimeSpan(0, 1, 2),
                BirthDateTime = new DateTime(1984, 12, 11, 0, 1, 2),
                FavoriteFeature = Feature.Feature3,
                Friends = new Collection<Person> { person0, person1 }
            };

            var person3 = new Person
            {
                PersonId = 3,
                FirstName = "Javier",
                UserName = "javieralfred",
                BirthDate = new DateTime(1985, 1, 10),
                BirthTime = new TimeSpan(23, 59, 1),
                BirthDateTime = new DateTime(1985, 1, 10, 23, 59, 1),
                FavoriteFeature = Feature.Feature4,
                BirthDate2 = new DateTime(1985, 1, 10),
                BirthTime2 = new TimeSpan(23, 59, 1),
                BirthDateTime2 = new DateTime(1985, 1, 10, 23, 59, 1),
                FavoriteFeature2 = Feature.Feature4,
            };


            var person4 = new Person
            {
                PersonId = 4,
                FirstName = "Willie",
                LastName = "Ashmore",
                UserName = "willieashmore",
                BirthDate = new DateTime(1986, 2, 9),
                BirthTime = new TimeSpan(22, 58, 2),
                BirthDateTime = new DateTime(1986, 2, 9, 22, 58, 2),
                FavoriteFeature = Feature.Feature1,
                BestFriend = person3,
                Friends = new Collection<Person>()
            };
            var person5 = new Person
            {
                PersonId = 5,
                FirstName = "Vincent",
                LastName = "Calabrese",
                UserName = "vincentcalabrese",
                BirthDate = new DateTime(1987, 3, 8),
                BirthTime = new TimeSpan(21, 57, 3),
                BirthDateTime = new DateTime(1987, 3, 8, 21, 57, 3),
                FavoriteFeature = Feature.Feature2,
                BestFriend = person4,
                Friends = new Collection<Person>()
            };
            var person6 = new Person
            {
                PersonId = 6,
                FirstName = "Clyde",
                LastName = "Guess",
                UserName = "clydeguess",
                BirthDate = new DateTime(1988, 4, 7),
                BirthTime = new TimeSpan(20, 56, 4),
                BirthDateTime = new DateTime(1988, 4, 7, 20, 56, 4),
                FavoriteFeature = Feature.Feature3,
            };
            var person7 = new Person
            {
                PersonId = 7,
                FirstName = "Keith",
                LastName = "Pinckney",
                UserName = "keithpinckney",
                BirthDate = new DateTime(1989, 5, 6),
                BirthTime = new TimeSpan(19, 55, 5),
                BirthDateTime = new DateTime(1989, 5, 6, 19, 55, 5),
                FavoriteFeature = Feature.Feature4,
            };
            var person8 = new Person
            {
                PersonId = 8,
                FirstName = "Marshall",
                LastName = "Garay",
                UserName = "marshallgaray",
                BirthDate = new DateTime(1990, 6, 5),
                BirthTime = new TimeSpan(18, 54, 6),
                BirthDateTime = new DateTime(1990, 6, 5, 18, 54, 6),
                FavoriteFeature = Feature.Feature1,
            };
            var person9 = new Person
            {
                PersonId = 9,
                FirstName = "Ryan",
                LastName = "Theriault",
                UserName = "ryantheriault",
                BirthDate = new DateTime(1991, 7, 4),
                BirthTime = new TimeSpan(17, 53, 7),
                BirthDateTime = new DateTime(1991, 7, 4, 17, 53, 7),
                FavoriteFeature = Feature.Feature2,
            };
            var person10 = new Person
            {
                PersonId = 10,
                FirstName = "Elaine",
                LastName = "Stewart",
                UserName = "elainestewart",
                BirthDate = new DateTime(1992, 8, 3),
                BirthTime = new TimeSpan(16, 52, 8),
                BirthDateTime = new DateTime(1992, 8, 3, 16, 52, 8),
                FavoriteFeature = Feature.Feature3,
            };
            var person11 = new Employee
            {
                PersonId = 11,
                FirstName = "Sallie",
                LastName = "Sampson",
                UserName = "salliesampson",
                Cost = 1000000,
                BirthDate = new DateTime(1993, 9, 2),
                BirthTime = new TimeSpan(15, 51, 9),
                BirthDateTime = new DateTime(1993, 9, 2, 15, 51, 9),
                FavoriteFeature = Feature.Feature4,
            };
            var person12 = new Manager
            {
                PersonId = 12,
                FirstName = "Joni",
                LastName = "Rosales",
                UserName = "jonirosales",
                Budget = 60000000,
                BirthDate = new DateTime(1994, 10, 1),
                BirthTime = new TimeSpan(14, 50, 10),
                BirthDateTime = new DateTime(1994, 10, 1, 14, 50, 10),
                FavoriteFeature = Feature.Feature1,
                BossOffice = new Location()
                {
                    Address = "ROOM 1001"
                }
            };

            person0.Friends = new Collection<Person> { person1 };
            person10.Friends = new Collection<Person> { person11 };
            #endregion

            instance.People.AddRange(new List<Person>
            {
                person0,
                person1,
                person2,
                person3,
                person4,
                person5,
                person6,
                person7,
                person8,
                person9,
                person10,
                person11,
                person12,
            });

            #endregion

            #region Orders

            var orders = new List<Order>
            {
                new Order
                {
                    PersonId = 1,
                    OrderId = 1,
                    Description = "Person 1 Order 1",
                    Price = 200,
                    NormalOrderDetail = new OrderDetail()
                    {
                        NormalProperty = "NormalProperty",
                        AnotherNormalProperty = "AnotherNormalProperty"
                    },
                    ComputedOrderDetail = new OrderDetail(),
                    ImmutableOrderDetail = new OrderDetail()
                },
                new Order
                {
                    PersonId = 1,
                    OrderId = 2,
                    Description = "Person 1 Order 2",
                    Price = 400,
                    NormalOrderDetail = new OrderDetail(),
                    ComputedOrderDetail = new OrderDetail(),
                    ImmutableOrderDetail = new OrderDetail()
                },
                new Order
                {
                    PersonId = 2,
                    OrderId = 1,
                    Description = "Person 2 Order 1",
                    Price = 600,
                    NormalOrderDetail = new OrderDetail(),
                    ComputedOrderDetail = new OrderDetail(),
                    ImmutableOrderDetail = new OrderDetail()
                },
                new Order
                {
                    PersonId = 2,
                    OrderId = 2,
                    Description = "Person 2 Order 2",
                    Price = 800,
                    NormalOrderDetail = new OrderDetail(),
                    ComputedOrderDetail = new OrderDetail(),
                    ImmutableOrderDetail = new OrderDetail()
                },
            };
            instance.Orders.AddRange(orders);

            #endregion

            #region Flights

            var flights = new List<Flight>
            {
                new Flight
                {
                    ConfirmationCode = "JH58493",
                    FlightNumber = "AA26",
                    StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1, 6, 15, 0)),
                    EndsAt = new DateTimeOffset(new DateTime(2014, 1, 1, 11, 35, 0)),
                    AirlineId = airlines[0].AirlineCode,
                    FromId = airports[12].IcaoCode,
                    ToId = airports[4].IcaoCode
                },
                new Flight
                {
                    ConfirmationCode = "JH38143",
                    FlightNumber = "AA4035",
                    StartsAt = new DateTimeOffset(new DateTime(2014, 1, 4, 17, 55, 0)),
                    EndsAt = new DateTimeOffset(new DateTime(2014, 1, 4, 20, 45, 0)),
                    AirlineId = airlines[0].AirlineCode,
                    FromId = airports[4].IcaoCode,
                    ToId = airports[12].IcaoCode
                },
                new Flight
                {
                    ConfirmationCode = "JH58494",
                    FlightNumber = "FM1930",
                    StartsAt = new DateTimeOffset(new DateTime(2014, 2, 1, 8, 0, 0)),
                    EndsAt = new DateTimeOffset(new DateTime(2014, 2, 1, 9, 20, 0)),
                    AirlineId = airlines[1].AirlineCode,
                    SeatNumber = "B11",
                    FromId = airports[2].IcaoCode,
                    ToId = airports[3].IcaoCode
                },
                new Flight
                {
                    ConfirmationCode = "JH58495",
                    FlightNumber = "MU1930",
                    StartsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 15, 00, 0)),
                    EndsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 16, 30, 0)),
                    AirlineId = airlines[2].AirlineCode,
                    SeatNumber = "A32",
                    FromId = airports[3].IcaoCode,
                    ToId = airports[2].IcaoCode
                },
            };
            flights.ForEach(f => f.Duration = f.EndsAt - f.StartsAt);
            instance.Flights.AddRange(flights);

            #endregion

            #region Trips

            var trips = new List<Trip>
            {
                new Trip
                {
                    PersonId = 1,
                    Name = "Trip in Beijing",
                    Budget = 2000.0f,
                    ShareId = new Guid("f94e9116-8bdd-4dac-ab61-08438d0d9a71"),
                    Description = "Trip from Shanghai to Beijing",
                    StartsAt = new DateTimeOffset(new DateTime(2014, 2, 1)),
                    EndsAt = new DateTimeOffset(new DateTime(2014, 2, 4)),
                    Flights = new List<Flight>(){flights[0], flights[1]},
                    LastUpdated = DateTime.UtcNow,
                },
                new Trip
                {
                    PersonId = 1,
                    ShareId = new Guid("9d9b2fa0-efbf-490e-a5e3-bac8f7d47354"),
                    Name = "Trip in US",
                    Budget = 3000.0f,
                    Description = "Trip from San Francisco to New York City. It is a 4 days' trip.",
                    StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                    EndsAt = new DateTimeOffset(new DateTime(2014, 1, 4)),
                    LastUpdated = DateTime.UtcNow,
                },
                new Trip
                {
                    PersonId = 2,
                    ShareId = new Guid("ccbda221-8a58-4ab8-b232-e47e3391bfc2"),
                    Name = "Honeymoon",
                    Budget = 2650.0f,
                    Description = "Happy honeymoon trip",
                    StartsAt = new DateTime(2014, 2, 1),
                    EndsAt = new DateTime(2014, 2, 4),
                    LastUpdated = DateTime.UtcNow,
                }
            };
            instance.Trips.AddRange(trips);

            #endregion

            #region Events

            instance.Events.AddRange(new[]
            {
                new Event
                {
                    OccursAt = new Location
                    {
                        Address = "Address1"
                    }
                },
            });
            #endregion

            #region Sponsors
            var sponsor0 = new Sponsor
            {
                SponsorId = 0,
                Name = "sponsor0"
            };
            var sponsor1 = new Sponsor
            {
                SponsorId = 1,
                Name = "sponsor1"
            };
            var sponsor2 = new Sponsor
            {
                SponsorId = 2,
                Name = "sponsor2"
            };
            var sponsor3 = new Sponsor
            {
                SponsorId = 3,
                Name = "sponsor3"
            };
            var sponsor4 = new GlodSponsor
            {
                SponsorId = 4,
                Name = "sponsor4",
                Funding = 10
            };
            var sponsor5 = new GlodSponsor
            {
                SponsorId = 5,
                Name = "sponsor5",
                Funding = 100
            };
            var sponsor6 = new GlodSponsor
            {
                SponsorId = 6,
                Name = "sponsor6",
                Funding = 1000
            };
            var sponsor7 = new GlodSponsor
            {
                SponsorId = 7,
                Name = "sponsor7",
                Funding = 10000
            };
            instance.Sponsors.AddRange(new List<Sponsor>
            {
                sponsor0,
                sponsor1,
                sponsor2,
                sponsor3,
                sponsor4,
                sponsor5,
                sponsor6,
                sponsor7,
            });
            #endregion

            #region Conferences
            var conference0 = new Conference
            {
                ConferenceId = 0,
                Name = "conference0",
                NumberOfAttendees = 1000
            };
            var conference1 = new Conference
            {
                ConferenceId = 1,
                Name = "conference1",
                NumberOfAttendees = 2000
            };
            var conference2 = new Conference
            {
                ConferenceId = 2,
                Name = "conference2",
                NumberOfAttendees = 1000
            };
            var conference3 = new Conference
            {
                ConferenceId = 3,
                Name = "conference3",
                NumberOfAttendees = 1000
            };
            var conference4 = new HighEndConference
            {
                ConferenceId = 4,
                Name = "conference4",
                NumberOfAttendees = 1000,
                NumberofVips = 1000
            };
            var conference5 = new HighEndConference
            {
                ConferenceId = 5,
                Name = "conference5",
                NumberOfAttendees = 1000,
                NumberofVips = 1000
            };
            var conference6 = new HighEndConference
            {
                ConferenceId = 6,
                Name = "conference6",
                NumberOfAttendees = 1000,
                NumberofVips = 1000
            };
            var conference7 = new HighEndConference
            {
                ConferenceId = 7,
                Name = "conference7",
                NumberOfAttendees = 1000,
                NumberofVips = 1000
            };

            conference0.Sponsors = new Collection<Sponsor> { sponsor0, sponsor1, sponsor2, sponsor3, sponsor4, sponsor5, sponsor6, sponsor7 };
            conference1.Sponsors = new Collection<Sponsor> { sponsor0, sponsor1, sponsor2, sponsor3, sponsor4, sponsor5, sponsor6, sponsor7 };
            conference2.Sponsors = new Collection<Sponsor> { sponsor0, sponsor1, sponsor2, sponsor3, sponsor4, sponsor5, sponsor6, sponsor7 };
            conference3.Sponsors = new Collection<Sponsor> { sponsor0, sponsor1, sponsor2, sponsor3, sponsor4, sponsor5, sponsor6, sponsor7 };
            conference4.Sponsors = new Collection<Sponsor> { sponsor0, sponsor1, sponsor2, sponsor3, sponsor4, sponsor5, sponsor6, sponsor7 };
            conference5.Sponsors = new Collection<Sponsor> { sponsor0, sponsor1, sponsor2, sponsor3, sponsor4, sponsor5, sponsor6, sponsor7 };
            conference6.Sponsors = new Collection<Sponsor> { sponsor0, sponsor1, sponsor2, sponsor3, sponsor4, sponsor5, sponsor6, sponsor7 };
            conference7.Sponsors = new Collection<Sponsor> { sponsor0, sponsor1, sponsor2, sponsor3, sponsor4, sponsor5, sponsor6, sponsor7 };
            conference4.GlodSponsors = new Collection<GlodSponsor> { sponsor4, sponsor5, sponsor6, sponsor7 };
            conference5.GlodSponsors = new Collection<GlodSponsor> { sponsor4, sponsor5, sponsor6, sponsor7 };
            conference6.GlodSponsors = new Collection<GlodSponsor> { sponsor4, sponsor5, sponsor6, sponsor7 };
            conference7.GlodSponsors = new Collection<GlodSponsor> { sponsor4, sponsor5, sponsor6, sponsor7 };

            instance.Conferences.AddRange(new List<Conference>
            {
                conference0,
                conference1,
                conference2,
                conference3,
                conference4,
                conference5,
                conference6,
                conference7,
            });
            #endregion

            #region Staff

            var staff0 = new Staff
            {
                StaffId = 0,
                FirstName = "Russell",
                UserName = "russellwhyte",
            };
            var staff1 = new Staff
            {
                StaffId = 1,
                FirstName = "Scott",
                UserName = "scottketchum",
            };
            var staff2 = new Staff
            {
                StaffId = 2,
                FirstName = "Ronald",
                UserName = "ronaldmundy",
            };
            var staff3 = new Staff
            {
                StaffId = 3,
                FirstName = "Javier",
                UserName = "javieralfred",
            };

            var staff4 = new Staff
            {
                StaffId = 4,
                FirstName = "Willie",
                UserName = "willieashmore",
            };
            var staff5 = new SeniorStaff
            {
                StaffId = 5,
                FirstName = "Vincent",
                UserName = "vincentcalabrese",
            };
            var staff6 = new SeniorStaff
            {
                StaffId = 6,
                FirstName = "Clyde",
                UserName = "clydeguess",
            };
            var staff7 = new SeniorStaff
            {
                StaffId = 7,
                FirstName = "Keith",
                UserName = "keithpinckney",
            };
            var staff8 = new SeniorStaff
            {
                StaffId = 7,
                FirstName = "Rains",
                UserName = "Lewis",
            };
            var staff9= new SeniorStaff
            {
                StaffId = 7,
                FirstName = "Layla",
                UserName = "Rains",
            };


            instance.Staffs.AddRange(new List<Staff>
            {
                staff0,
                staff1,
                staff2,
                staff3,
                staff4,
                staff5,
                staff6,
                staff7,
                staff8,
                staff9,
            });

            staff0.PeerStaffs = new Collection<Staff> { staff1, staff2, staff3, staff4, staff5, staff6, staff7, staff8, staff9 };
            staff1.PeerStaffs = new Collection<Staff> { staff0, staff2, staff3, staff4, staff5, staff6, staff7, staff8, staff9 };
            staff2.PeerStaffs = new Collection<Staff> { staff0, staff1, staff3, staff4, staff5, staff6, staff7, staff8, staff9 };
            staff3.PeerStaffs = new Collection<Staff> { staff0, staff1, staff2, staff4, staff5, staff6, staff7, staff8, staff9 };
            staff5.PeerStaffs = new Collection<Staff> { staff0, staff1, staff2, staff3, staff4, staff6, staff7, staff8, staff9 };
            staff6.PeerStaffs = new Collection<Staff> { staff0, staff1, staff2, staff3, staff4, staff5, staff7, staff8, staff9 };
            staff7.PeerStaffs = new Collection<Staff> { staff0, staff1, staff2, staff3, staff4, staff5, staff6, staff8, staff9 };

            staff0.Conferences = new Collection<Conference> { conference0, conference1, conference2, conference3, conference4, conference5, conference6, conference7};
            staff1.Conferences = new Collection<Conference> { conference0, conference1, conference2, conference3, conference4, conference5, conference6, conference7 };
            staff2.Conferences = new Collection<Conference> { conference0, conference1, conference2, conference3, conference4, conference5, conference6, conference7 };
            staff3.Conferences = new Collection<Conference> { conference0, conference1, conference2, conference3, conference4, conference5, conference6, conference7 };
            staff5.Conferences = new Collection<Conference> { conference0, conference1, conference2, conference3, conference4, conference5, conference6, conference7 };
            staff6.Conferences = new Collection<Conference> { conference0, conference1, conference2, conference3, conference4, conference5, conference6, conference7 };
            staff7.Conferences = new Collection<Conference> { conference0, conference1, conference2, conference3, conference4, conference5, conference6, conference7 };

            staff5.PeerSeniorStaffs = new Collection<SeniorStaff> { staff6, staff7, staff8, staff9 };
            staff6.PeerSeniorStaffs = new Collection<SeniorStaff> { staff5, staff7, staff8, staff9 };
            staff7.PeerSeniorStaffs = new Collection<SeniorStaff> { staff5, staff6, staff8, staff9 };

            staff5.HighEndConferences = new Collection<HighEndConference> { conference4, conference5, conference6, conference7 };
            staff6.HighEndConferences = new Collection<HighEndConference> { conference4, conference5, conference6, conference7 };
            staff7.HighEndConferences = new Collection<HighEndConference> { conference4, conference5, conference6, conference7 };

            #endregion

            instance.SaveChanges();
        }
    }

    class TrippinDatabaseInitializer : DropCreateDatabaseAlways<TrippinModel>
    {
        protected override void Seed(TrippinModel context)
        {
            TrippinModel.ResetDataSource();
        }
    }
}
