// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Models
{
    public partial class TrippinModel : DbContext
    {
        public TrippinModel()
            : base("name=TrippinModel")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Trip>().HasMany<Flight>(s => s.Flights).WithMany().Map(c =>
            {
                c.MapLeftKey("TripId");
                c.MapRightKey("FlightId");
                c.ToTable("TripAndFlights");
            });

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Person> People { get; set; }
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Airline> Airlines { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Event> Events { get; set; }

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
            instance.People.RemoveRange(instance.People);
            instance.Flights.RemoveRange(instance.Flights);
            instance.Airlines.RemoveRange(instance.Airlines);
            instance.Airports.RemoveRange(instance.Airports);
            instance.Trips.RemoveRange(instance.Trips);
            // This is to set the People Id from 0
            instance.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('People', RESEED, 0)");
            instance.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('Flights', RESEED, 0)");
            instance.Database.ExecuteSqlCommand("DBCC CHECKIDENT ('TripsTable', RESEED, 0)");
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
                }
            };
            instance.Airlines.AddRange(airlines);

            #endregion

            #region People

            instance.People.AddRange(new List<Person>
            {
                #region russellwhyte
                new Person
                {
                    PersonId = 1,
                    FirstName = "Russell",
                    LastName = "Whyte",
                    UserName = "russellwhyte",
                },

                #endregion

                #region scottketchum
                new Person
                {
                    PersonId = 2,
                    FirstName = "Scott",
                    LastName = "Ketchum",
                    UserName = "scottketchum",
                },

                #endregion

                #region ronaldmundy
                new Person
                {
                    PersonId = 3,
                    FirstName = "Ronald",
                    LastName = "Mundy",
                    UserName = "ronaldmundy",
                },
                #endregion

                new Person
                {
                    PersonId = 4,
                    FirstName = "Javier",
                    LastName = "Alfred",
                    UserName = "javieralfred",
                },
                new Person
                {
                    PersonId = 5,
                    FirstName = "Willie",
                    LastName = "Ashmore",
                    UserName = "willieashmore",
                },
                new Person
                {
                    PersonId = 6,
                    FirstName = "Vincent",
                    LastName = "Calabrese",
                    UserName = "vincentcalabrese",
                },
                new Person
                {
                    PersonId = 7,
                    FirstName = "Clyde",
                    LastName = "Guess",
                    UserName = "clydeguess",
                },
                new Person
                {
                    PersonId = 8,
                    FirstName = "Keith",
                    LastName = "Pinckney",
                    UserName = "keithpinckney",
                },
                new Person
                {
                    PersonId = 9,
                    FirstName = "Marshall",
                    LastName = "Garay",
                    UserName = "marshallgaray",
                },
                new Person
                {
                    PersonId = 10,
                    FirstName = "Ryan",
                    LastName = "Theriault",
                    UserName = "ryantheriault",
                },
                new Person
                {
                    PersonId = 11,
                    FirstName = "Elaine",
                    LastName = "Stewart",
                    UserName = "elainestewart",
                },
                new Person
                {
                    PersonId = 12,
                    FirstName = "Sallie",
                    LastName = "Sampson",
                    UserName = "salliesampson",
                },
                new Person
                {
                    PersonId = 13,
                    FirstName = "Joni",
                    LastName = "Rosales",
                    UserName = "jonirosales",
                }
            });

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

            instance.SaveChanges();
        }
    }
}
