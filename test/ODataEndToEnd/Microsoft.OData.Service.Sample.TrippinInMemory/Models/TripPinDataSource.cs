// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Spatial;

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Models
{
    public class TripPinDataSource
    {
        public List<Person> People { get; set; }

        public List<Airport> Airports { get; private set; }

        public List<Airline> Airlines { get; private set; }

        public Person Me { get; set; }

        public TripPinDataSource()
        {
            this.Reset();
            this.Initialize();
        }

        private void Reset()
        {
            this.People = new List<Person>();
            this.Airports = new List<Airport>();
            this.Airlines = new List<Airline>();
        }

        private void Initialize()
        {
            #region Airports
            this.Airports.AddRange(new List<Airport>()
            {
                new Airport()
                {
                    Name = "San Francisco International Airport",
                    Location = new AirportLocation()
                    {
                        Address = "South McDonnell Road, San Francisco, CA 94128",
                        City = new City()
                        {
                            Name = "San Francisco",
                            CountryRegion = "United States",
                            Region = "California"
                        },
                        Loc = GeographyPoint.Create(37.6188888888889, -122.374722222222)
                    },
                    IataCode = "SFO",
                    IcaoCode = "KSFO"
                },
                new Airport()
                {
                    Name = "Los Angeles International Airport",
                    Location = new AirportLocation()
                    {
                        Address = "1 World Way, Los Angeles, CA, 90045",
                        City = new City()
                        {
                            Name = "Los Angeles",
                            CountryRegion = "United States",
                            Region = "California"
                        },
                        Loc = GeographyPoint.Create(33.9425, -118.408055555556)
                    },
                    IataCode = "LAX",
                    IcaoCode = "KLAX"
                },
                new Airport()
                {
                    Name = "Shanghai Hongqiao International Airport",
                    Location = new AirportLocation()
                    {
                        Address = "Hongqiao Road 2550, Changning District",
                        City = new City()
                        {
                            Name = "Shanghai",
                            CountryRegion = "China",
                            Region = "Shanghai"
                        },
                        Loc = GeographyPoint.Create(31.1977777777778, 121.336111111111)
                    },
                    IataCode = "SHA",
                    IcaoCode = "ZSSS"
                },
                new Airport()
                {
                    Name = "Beijing Capital International Airport",
                    Location = new AirportLocation()
                    {
                        Address = "Airport Road, Chaoyang District, Beijing, 100621",
                        City = new City()
                        {
                            Name = "Beijing",
                            CountryRegion = "China",
                            Region = "Beijing"
                        },
                        Loc = GeographyPoint.Create(40.08, 116.584444444444)
                    },
                    IataCode = "PEK",
                    IcaoCode = "ZBAA"
                },
                new Airport()
                {
                    Name = "John F. Kennedy International Airport",
                    Location = new AirportLocation()
                    {
                        Address = "Jamaica, New York, NY 11430",
                        City = new City()
                        {
                            Name = "New York City",
                            CountryRegion = "United States",
                            Region = "New York"
                        },
                        Loc = GeographyPoint.Create(40.6397222222222, -73.7788888888889)
                    },
                    IataCode = "JFK",
                    IcaoCode = "KJFK"
                }
            });
            #endregion

            #region Airlines
            this.Airlines.AddRange(new List<Airline>()
            {
                new Airline()
                {
                    Name = "American Airlines",
                    AirlineCode = "AA"
                },

                new Airline()
                {
                    Name = "Shanghai Airline",
                    AirlineCode = "FM"
                },

                new Airline()
                {
                    Name = "China Eastern Airlines",
                    AirlineCode = "MU"
                }
            });
            #endregion

            #region People
            this.People.AddRange(new List<Person>()
            {
                new Person()
                {
                    FirstName = "Russell",
                    LastName = "Whyte",
                    UserName = "russellwhyte",
                    Gender = PersonGender.Male,
                    Emails = new List<string> { "Russell@example.com", "Russell@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "187 Suffolk Ln.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Boise",
                              Region = "ID"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 0,
                            ShareId = new Guid("9d9b2fa0-efbf-490e-a5e3-bac8f7d47354"),
                            Name = "Trip in US",
                            Budget = 3000.0f,
                            Description = "Trip from San Francisco to New York City",
                            Tags = new List<string>
                            {
                                "business",
                                "New York meeting"
                            },
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 4)),
                            PlanItems = new List<PlanItem>
                            {
                                new Flight()
                                {
                                    PlanItemId = 11,
                                    ConfirmationCode = "JH58493",
                                    FlightNumber = "VA1930",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1, 8, 0, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 1, 1, 9, 20, 0)),
                                    Airline = Airlines[0],
                                    From = Airports[0],
                                    To = Airports[4]
                                },
                                new Event()
                                {
                                    PlanItemId = 12,
                                    Description = "Client Meeting",
                                    ConfirmationCode = "4372899DD",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 1, 2, 13, 0, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 1, 6, 13, 0, 0)),
                                    Duration = new TimeSpan(3, 0, 0),
                                    OccursAt = new EventLocation()
                                    {
                                        BuildingInfo = "Regus Business Center",
                                        City = new City()
                                        {
                                            Name = "New York City",
                                            CountryRegion = "United States",
                                            Region = "New York"
                                        },
                                        Address = "100 Church Street, 8th Floor, Manhattan, 10007"
                                    }
                                },
                                new Flight()
                                {
                                    PlanItemId = 13,
                                    ConfirmationCode = "JH58493",
                                    FlightNumber = "VA1930",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 1, 4, 13, 0, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 1, 4, 14, 20, 0)),
                                    Airline = Airlines[0],
                                    From = Airports[4],
                                    To = Airports[0]
                                },
                            }
                        },
                        new Trip()
                        {
                            TripId = 1,
                            Name = "Trip in Beijing",
                            Budget = 2000.0f,
                            ShareId = new Guid("f94e9116-8bdd-4dac-ab61-08438d0d9a71"),
                            Description = "Trip from Shanghai to Beijing",
                            Tags = new List<string>{"Travel", "Beijing"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 2, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 2, 4)),
                            PlanItems = new List<PlanItem>
                            {
                                new Flight()
                                {
                                    PlanItemId = 14,
                                    ConfirmationCode = "JH58494",
                                    FlightNumber = "FM1930",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 2, 1, 8, 0, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 2, 1, 9, 20, 0)),
                                    Airline = Airlines[1],
                                    SeatNumber = "B11",
                                    From = Airports[2],
                                    To = Airports[3]
                                },
                                new Flight()
                                {
                                    PlanItemId = 15,
                                    ConfirmationCode = "JH58495",
                                    FlightNumber = "MU1930",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 15, 30, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 16, 30, 0)),
                                    Airline = Airlines[2],
                                    SeatNumber = "A32",
                                    From = Airports[3],
                                    To = Airports[2]
                                },
                                new Event()
                                {
                                    PlanItemId = 16,
                                    Description = "Dinner",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 2, 2, 18, 0, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 2, 2, 21, 0, 0)),
                                    Duration = new TimeSpan(3, 0, 0),
                                    OccursAt = new EventLocation()
                                    {
                                        BuildingInfo = "Beijing Restaurant",
                                        City = new City()
                                        {
                                            Name = "Beijing",
                                            CountryRegion = "China",
                                            Region = "Beijing"
                                        },
                                        Address = "10 Beijing Street, 100000"
                                    }
                                }
                            }
                        },
                        new Trip()
                        {
                            TripId = 2,
                            ShareId = new Guid("9ce142c3-5fd6-4a71-848e-5220ebf1e9f3"),
                            Name = "Honeymoon",
                            Budget = 2650.0f,
                            Description = "Happy honeymoon trip",
                            Tags = new List<string>{"Travel", "honeymoon"},
                            StartsAt = new DateTime(2014, 2, 1),
                            EndsAt = new DateTime(2014, 2, 4)
                        }
                    },
                    Features = new List<Feature>
                    {
                        Feature.Feature1,
                        Feature.Feature2
                    },
                    FavoriteFeature = Feature.Feature1
                },
                new Person()
                {
                    FirstName = "Scott",
                    LastName = "Ketchum",
                    UserName = "scottketchum",
                    Gender = PersonGender.Male,
                    Emails = new List<string> { "Scott@example.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "2817 Milton Dr.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Albuquerque",
                              Region = "NM"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 3,
                            ShareId = new Guid("9d9b2fa0-efbf-490e-a5e3-bac8f7d47354"),
                            Name = "Trip in US",
                            Budget = 5000.0f,
                            Description = "Trip from San Francisco to New York City",
                            Tags = new List<string>{"business","New York meeting"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 4)),
                            PlanItems = new List<PlanItem>
                            {
                                new Flight()
                                {
                                    PlanItemId = 17,
                                    ConfirmationCode = "JH58493",
                                    FlightNumber = "VA1930",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1, 8, 0, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 1, 1, 9, 20, 0)),
                                    Airline = Airlines[0],
                                    SeatNumber = "A12",
                                    From = Airports[0],
                                    To = Airports[4]
                                },
                                new Event()
                                {
                                    PlanItemId = 18,
                                    Description = "Client Meeting",
                                    ConfirmationCode = "4372899DD",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 1, 2, 13, 0, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 1, 2, 16, 0, 0)),
                                    Duration = new TimeSpan(3, 0, 0),
                                    OccursAt = new EventLocation()
                                    {
                                        BuildingInfo = "Regus Business Center",
                                        City = new City()
                                        {
                                            Name = "New York City",
                                            CountryRegion = "United States",
                                            Region = "New York"
                                        },
                                        Address = "100 Church Street, 8th Floor, Manhattan, 10007"
                                    }
                                },
                                new Flight()
                                {
                                    PlanItemId = 19,
                                    ConfirmationCode = "JH58493",
                                    FlightNumber = "VA1930",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 1, 4, 13, 0, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 1, 4, 14, 20, 0)),
                                    Airline = Airlines[0],
                                    From = Airports[4],
                                    To = Airports[0]
                                }
                            }
                        },
                        new Trip()
                        {
                            TripId = 4,
                            ShareId = new Guid("f94e9116-8bdd-4dac-ab61-08438d0d9a71"),
                            Name = "Trip in Beijing",
                            Budget = 11000.0f,
                            Description = "Trip from Shanghai to Beijing",
                            Tags = new List<string>{"Travel", "Beijing"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 2, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 2, 4)),
                            PlanItems = new List<PlanItem>
                            {
                                new Flight()
                                {
                                    PlanItemId = 20,
                                    ConfirmationCode = "JH58494",
                                    FlightNumber = "FM1930",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 2, 1, 8, 0, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 2, 1, 9, 20, 0)),
                                    Airline = Airlines[1],
                                    SeatNumber = "B12",
                                    From = Airports[2],
                                    To = Airports[3]
                                },
                                new Flight()
                                {
                                    PlanItemId = 21,
                                    ConfirmationCode = "JH58495",
                                    FlightNumber = "MU1930",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 16, 30, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 16, 30, 0)),
                                    Airline = Airlines[2],
                                    SeatNumber = "A33",
                                    From = Airports[3],
                                    To = Airports[2]
                                },
                                new Event()
                                {
                                    PlanItemId = 22,
                                    Description = "Dinner",
                                    StartsAt = new DateTimeOffset(new DateTime(2014, 2, 2, 18, 0, 0)),
                                    EndsAt = new DateTimeOffset(new DateTime(2014, 2, 2, 21, 0, 0)),
                                    Duration = new TimeSpan(3, 0, 0),
                                    OccursAt = new EventLocation()
                                    {
                                        BuildingInfo = "Beijing Restaurant",
                                        City = new City()
                                        {
                                            Name = "Beijing",
                                            CountryRegion = "China",
                                            Region = "Beijing"
                                        },
                                        Address = "10 Beijing Street, 100000"
                                    }
                                }
                            }
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Ronald",
                    LastName = "Mundy",
                    UserName = "ronaldmundy",
                    Gender = PersonGender.Male,
                    Emails = new List<string> { "Ronald@example.com", "Ronald@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "187 Suffolk Ln.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Boise",
                              Region = "ID"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 5,
                            ShareId = new Guid("dd6a09c0-e59b-4745-8612-f4499b676c47"),
                            Name = "Gradutaion trip",
                            Budget = 6000.0f,
                            Description = "Gradution trip with friends",
                            Tags = new List<string>{"Travel"},
                            StartsAt = new DateTimeOffset(new DateTime(2013, 5, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2013, 5, 8))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Javier",
                    LastName = "Alfred",
                    UserName = "javieralfred",
                    Gender = PersonGender.Male,
                    Emails = new List<string> { "Javier@example.com", "Javier@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "89 Jefferson Way Suite 2",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Portland",
                              Region = "WA"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 6,
                            ShareId = new Guid("f94e9116-8bdd-4dac-ab61-08438d0d9a71"),
                            Name = "Trip in Beijing",
                            Budget = 800.0f,
                            Description = "Trip from Shanghai to Beijing",
                            Tags = new List<string>{"Travel", "Beijing"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 2, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 2, 4))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Willie",
                    LastName = "Ashmore",
                    UserName = "willieashmore",
                    Gender = PersonGender.Male,
                    Emails = new List<string>(),
                    AddressInfo = new List<Location>(),
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 7,
                            ShareId = new Guid("5ae142c3-5ad6-4a71-768e-5220ebf1e9f3"),
                            Name = "Business Trip",
                            Budget = 3800.5f,
                            Description = "This is my first business trip",
                            Tags = new List<string>{"business", "first"},
                            StartsAt = new DateTime(2014, 2, 1),
                            EndsAt = new DateTime(2014, 2, 4)
                        },
                        new Trip()
                        {
                            TripId = 8,
                            ShareId = new Guid("9ce32ac3-5fd6-4a72-848e-2250ebf1e9f3"),
                            Name = "Trip in Europe",
                            Budget = 2000.0f,
                            Description = "The trip is currently in plan.",
                            Tags = new List<string>{"Travel", "plan"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 2, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 2, 4))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Vincent",
                    LastName = "Calabrese",
                    UserName = "vincentcalabrese",
                    Gender = PersonGender.Male,
                    Emails = new List<string> { "Vincent@example.com", "Vincent@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "55 Grizzly Peak Rd.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Butte",
                              Region = "MT"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 9,
                            ShareId = new Guid("dd6a09c0-e59b-4745-8612-f4499b676c47"),
                            Name = "Gradutaion trip",
                            Budget = 1000.0f,
                            Description = "Gradution trip with friends",
                            Tags = new List<string>{"Travel"},
                            StartsAt = new DateTimeOffset(new DateTime(2013, 5, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2013, 5, 8))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Clyde",
                    LastName = "Guess",
                    UserName = "clydeguess",
                    Gender = PersonGender.Male,
                    HomeAddress = new Location(),
                    Trips = new List<Trip>()
                },
                new Person()
                {
                    FirstName = "Keith",
                    LastName = "Pinckney",
                    UserName = "keithpinckney",
                    Gender = PersonGender.Male,
                    Emails = new List<string> { "Keith@example.com", "Keith@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "55 Grizzly Peak Rd.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Butte",
                              Region = "MT"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 11,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 1550.3f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Marshall",
                    LastName = "Garay",
                    UserName = "marshallgaray",
                    Gender = PersonGender.Male,
                    Emails = new List<string> { "Marshall@example.com", "Marshall@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "55 Grizzly Peak Rd.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Butte",
                              Region = "MT"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 12,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 1550.3f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Ryan",
                    LastName = "Theriault",
                    UserName = "ryantheriault",
                    Gender = PersonGender.Male,
                    Emails = new List<string> { "Ryan@example.com", "Ryan@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "55 Grizzly Peak Rd.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Butte",
                              Region = "MT"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 13,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 1550.3f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Elaine",
                    LastName = "Stewart",
                    UserName = "elainestewart",
                    Gender = PersonGender.Female,
                    Emails = new List<string> { "Elaine@example.com", "Elaine@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "55 Grizzly Peak Rd.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Butte",
                              Region = "MT"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 14,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 1550.3f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Sallie",
                    LastName = "Sampson",
                    UserName = "salliesampson",
                    Gender = PersonGender.Female,
                    Emails = new List<string> { "Sallie@example.com", "Sallie@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "87 Polk St. Suite 5",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "San Francisco",
                              Region = "CA"
                          }
                      },
                      new Location()
                      {
                          Address = "89 Chiaroscuro Rd.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Portland",
                              Region = "OR"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 15,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 600.0f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Joni",
                    LastName = "Rosales",
                    UserName = "jonirosales",
                    Gender = PersonGender.Female,
                    Emails = new List<string> { "Joni@example.com", "Joni@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "55 Grizzly Peak Rd.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Butte",
                              Region = "MT"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 16,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 2000.0f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Georgina",
                    LastName = "Barlow",
                    UserName = "georginabarlow",
                    Gender = PersonGender.Female,
                    Emails = new List<string> { "Georgina@example.com", "Georgina@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "55 Grizzly Peak Rd.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Butte",
                              Region = "MT"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 17,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 1550.3f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Angel",
                    LastName = "Huffman",
                    UserName = "angelhuffman", Gender = PersonGender.Female,
                    Emails = new List<string> { "Angel@example.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "55 Grizzly Peak Rd.",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "Butte",
                              Region = "MT"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 18,
                            ShareId = new Guid("cb0b8acb-79cb-4127-8316-772bc4302824"),
                            Name = "DIY Trip",
                            Budget = 1500.3f,
                            Description = "This is a DIY trip",
                            Tags = new List<string>{"Travel", "DIY"},
                            StartsAt = new DateTimeOffset(new DateTime(2011, 2, 11)),
                            EndsAt = new DateTimeOffset(new DateTime(2011, 2, 14))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Laurel",
                    LastName = "Osborn",
                    UserName = "laurelosborn",
                    Gender = PersonGender.Female,
                    Emails = new List<string> { "Laurel@example.com", "Laurel@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "87 Polk St. Suite 5",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "San Francisco",
                              Region = "CA"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 19,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 1550.3f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Sandy",
                    LastName = "Osborn",
                    UserName = "sandyosborn",
                    Gender = PersonGender.Female,
                    Emails = new List<string> { "Sandy@example.com", "Sandy@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "87 Polk St. Suite 5",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "San Francisco",
                              Region = "CA"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 20,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 1550.3f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Person()
                {
                    FirstName = "Ursula",
                    LastName = "Bright",
                    UserName = "ursulabright",
                    Gender = PersonGender.Female,
                    Emails = new List<string> { "Ursula@example.com", "Ursula@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "87 Polk St. Suite 5",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "San Francisco",
                              Region = "CA"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 21,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 1550.3f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Manager()
                {
                    FirstName = "Genevieve",
                    LastName = "Reeves",
                    UserName = "genevievereeves",
                    Gender = PersonGender.Female,
                    Emails = new List<string> { "Genevieve@example.com", "Genevieve@contoso.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "87 Polk St. Suite 5",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "San Francisco",
                              Region = "CA"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 22,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 1550.3f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                },
                new Employee()
                {
                    FirstName = "Krista",
                    LastName = "Kemp",
                    UserName = "kristakemp",
                    Gender = PersonGender.Female,
                    Emails = new List<string> { "Krista@example.com" },
                    AddressInfo = new List<Location>
                    {
                      new Location()
                      {
                          Address = "87 Polk St. Suite 5",
                          City = new City()
                          {
                              CountryRegion = "United States",
                              Name = "San Francisco",
                              Region = "CA"
                          }
                      }
                    },
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            TripId = 234,
                            ShareId = new Guid("a88f675d-9199-4392-9656-b08e3b46df8a"),
                            Name = "Study trip",
                            Budget = 1550.3f,
                            Description = "This is a 2 weeks study trip",
                            Tags = new List<string>{"study"},
                            StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                            EndsAt = new DateTimeOffset(new DateTime(2014, 1, 14))
                        }
                    }
                }
            });

            People.Single(p => p.UserName == "russellwhyte").Friends = new Collection<Person>()
                {
                    People.Single(p => p.UserName == "scottketchum"),
                    People.Single(p => p.UserName == "ronaldmundy"),
                    People.Single(p => p.UserName == "javieralfred")
                };

            People.Single(p => p.UserName == "russellwhyte").BestFriend=
                People.Single(p => p.UserName == "scottketchum");

            People.Single(p => p.UserName == "scottketchum").Friends = new Collection<Person>()
                {
                    People.Single(p => p.UserName == "russellwhyte"),
                    People.Single(p => p.UserName == "ronaldmundy")
                };
            People.Single(p => p.UserName == "ronaldmundy").Friends = new Collection<Person>()
                {
                    People.Single(p => p.UserName == "russellwhyte"),
                    People.Single(p => p.UserName == "scottketchum")
                };
            People.Single(p => p.UserName == "javieralfred").Friends = new Collection<Person>()
                {
                    People.Single(p => p.UserName == "willieashmore"),
                    People.Single(p => p.UserName == "vincentcalabrese")
                };
            People.Single(p => p.UserName == "willieashmore").Friends = new Collection<Person>()
                {
                    People.Single(p => p.UserName == "javieralfred"),
                    People.Single(p => p.UserName == "vincentcalabrese")
                };
            People.Single(p => p.UserName == "vincentcalabrese").Friends = new Collection<Person>()
                {
                    People.Single(p => p.UserName == "javieralfred"),
                    People.Single(p => p.UserName == "willieashmore")
                };
            People.Single(p => p.UserName == "clydeguess").Friends = new Collection<Person>()
                {
                    People.Single(p => p.UserName == "keithpinckney")
                };
            People.Single(p => p.UserName == "keithpinckney").Friends = new Collection<Person>()
                {
                    People.Single(p => p.UserName == "clydeguess"),
                    People.Single(p => p.UserName == "marshallgaray")
                };
            People.Single(p => p.UserName == "marshallgaray").Friends = new Collection<Person>()
                {
                    People.Single(p => p.UserName == "keithpinckney")
                };
            People.Single(p => p.UserName == "ryantheriault").Friends = new Collection<Person>()
            {
                People.Single(p=>p.UserName == "elainestewart")
            };
            People.Single(p => p.UserName == "elainestewart").Friends = new Collection<Person>()
            {
                People.Single(p => p.UserName == "ryantheriault")
            };
            People.Single(p => p.UserName == "salliesampson").Friends = new Collection<Person>()
            {
                People.Single(p => p.UserName == "jonirosales")
            };
            People.Single(p => p.UserName == "jonirosales").Friends = new Collection<Person>()
            {
                People.Single(p => p.UserName == "salliesampson")
            };
            People.Single(p => p.UserName == "georginabarlow").Friends = new Collection<Person>()
            {
                People.Single(p => p.UserName == "angelhuffman")
            };
            People.Single(p => p.UserName == "angelhuffman").Friends = new Collection<Person>()
            {
                People.Single(p => p.UserName == "georginabarlow")
            };
            People.Single(p => p.UserName == "laurelosborn").Friends = new Collection<Person>()
            {
                People.Single(p => p.UserName == "sandyosborn")
            };
            People.Single(p => p.UserName == "sandyosborn").Friends = new Collection<Person>()
            {
                People.Single(p => p.UserName == "laurelosborn")
            };
            People.Single(p => p.UserName == "ursulabright").Friends = new Collection<Person>()
            {
                People.Single(p => p.UserName == "genevievereeves"),
                People.Single(p => p.UserName == "kristakemp")
            };
            People.Single(p => p.UserName == "genevievereeves").Friends = new Collection<Person>()
            {
                People.Single(p => p.UserName == "ursulabright")
            };
            People.Single(p => p.UserName == "kristakemp").Friends = new Collection<Person>()
            {
                People.Single(p => p.UserName == "ursulabright")
            };
            #endregion

            #region Me
            this.Me = new Person()
            {
                FirstName = "April",
                LastName = "Cline",
                UserName = "aprilcline",
                Gender = PersonGender.Female,
                Emails = new List<string> { "April@example.com", "April@contoso.com" },
                AddressInfo = new List<Location>
                {
                    new Location()
                    {
                        Address = "P.O. Box 555",
                        City = new City()
                        {
                            CountryRegion = "United States",
                            Name = "Lander",
                            Region = "WY"
                        }
                    }
                },
                Trips = new List<Trip>
                {
                    new Trip()
                    {
                        TripId = 101,
                        ShareId = new Guid("9d9b2fa0-efbf-490e-a5e3-bac8f7d47354"),
                        Name = "Trip in US",
                        Budget = 1000.0f,
                        Description = "Trip in US",
                        Tags = new List<string>
                        {
                            "business",
                            "US"
                        },
                        StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1)),
                        EndsAt = new DateTimeOffset(new DateTime(2014, 1, 4)),
                        PlanItems = new List<PlanItem>
                        {
                            new Flight()
                            {
                                PlanItemId = 11,
                                ConfirmationCode = "JH58493",
                                FlightNumber = "VA1930",
                                StartsAt = new DateTimeOffset(new DateTime(2014, 1, 1, 8, 0, 0)),
                                EndsAt = new DateTimeOffset(new DateTime(2014, 1, 1, 9, 20, 0)),
                                Airline = Airlines[0],
                                From = Airports[0],
                                To = Airports[1]
                            },
                            new Event()
                            {
                                PlanItemId = 12,
                                Description = "Client Meeting",
                                ConfirmationCode = "4372899DD",
                                StartsAt = new DateTimeOffset(new DateTime(2014, 1, 2, 13, 0, 0)),
                                EndsAt = new DateTimeOffset(new DateTime(2014, 1, 2, 16, 0, 0)),
                                Duration = new TimeSpan(3, 0, 0),
                                OccursAt = new EventLocation()
                                {
                                    Address = "100 Church Street, 8th Floor, Manhattan, 10007",
                                    BuildingInfo = "Regus Business Center",
                                    City = new City()
                                    {
                                        Name = "New York City",
                                        CountryRegion = "United States",
                                        Region = "New York"
                                    }
                                }
                            }
                        }
                    },
                    new Trip()
                    {
                        TripId = 102,
                        Name = "Trip in Beijing",
                        Budget = 3000.0f,
                        ShareId = new Guid("f94e9116-8bdd-4dac-ab61-08438d0d9a71"),
                        Description = "Trip from Shanghai to Beijing",
                        Tags = new List<string>{"Travel", "Beijing"},
                        StartsAt = new DateTimeOffset(new DateTime(2014, 2, 1)),
                        EndsAt = new DateTimeOffset(new DateTime(2014, 2, 4)),
                        PlanItems = new List<PlanItem>
                        {
                            new Flight()
                            {
                                PlanItemId = 21,
                                ConfirmationCode = "JH58494",
                                FlightNumber = "FM1930",
                                StartsAt = new DateTimeOffset(new DateTime(2014, 2, 1, 8, 0, 0)),
                                EndsAt = new DateTimeOffset(new DateTime(2014, 2, 1, 9, 20, 0)),
                                Airline = Airlines[1],
                                SeatNumber = "B11",
                                From = Airports[2],
                                To = Airports[3]
                            },
                            new Flight()
                            {
                                PlanItemId = 32,
                                ConfirmationCode = "JH58495",
                                FlightNumber = "MU1930",
                                StartsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 15, 00, 0)),
                                EndsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 16, 30, 0)),
                                Airline = Airlines[2],
                                SeatNumber = "A32",
                                From = Airports[3],
                                To = Airports[2]
                            },
                            new Event()
                            {
                                PlanItemId = 5,
                                Description = "Dinner",
                                StartsAt = new DateTimeOffset(new DateTime(2014, 2, 2, 18, 0, 0)),
                                EndsAt = new DateTimeOffset(new DateTime(2014, 2, 2, 21, 0, 0)),
                                Duration = new TimeSpan(3, 0, 0),
                                OccursAt = new EventLocation()
                                {
                                    Address = "10 Beijing Street, 100000",
                                    City = new City(){
                                        Name = "Beijing",
                                        CountryRegion = "China",
                                        Region = "Beijing"
                                    },
                                    BuildingInfo = "Beijing Restaurant"
                                }
                            }
                        }
                    },
                    new Trip()
                    {
                        TripId = 103,
                        ShareId = new Guid("9ce142c3-5fd6-4a71-848e-5220ebf1e9f3"),
                        Name = "Honeymoon",
                        Budget = 800.0f,
                        Description = "Happy honeymoon trip",
                        Tags = new List<string>{"Travel", "honeymoon"},
                        StartsAt = new DateTime(2014, 2, 1),
                        EndsAt = new DateTime(2014, 2, 4)
                    },
                    new Trip()
                    {
                        TripId = 104,
                        ShareId = new Guid("4CCFB043-C79C-44EF-8CFE-CD493CED6654"),
                        Name = "Business trip to OData",
                        Budget = 324.6f,
                        Description = "Business trip to OData",
                        Tags = new List<string>{"business", "odata"},
                        StartsAt = new DateTime(2013, 1, 1),
                        EndsAt = new DateTime(2013, 1, 4)
                    },
                    new Trip()
                    {
                        TripId = 105,
                        ShareId = new Guid("4546F419-0070-45F7-BA2C-19E4BC3647E1"),
                        Name = "Travel trip in US",
                        Budget = 1250.0f,
                        Description = "Travel trip in US",
                        Tags = new List<string>{"travel", "overseas"},
                        StartsAt = new DateTime(2013, 1, 19),
                        EndsAt = new DateTime(2013, 1, 28)
                    },
                    new Trip()
                    {
                        TripId = 106,
                        ShareId = new Guid("26F0E8F6-657A-4561-BF3B-719366EF04FA"),
                        Name = "Study music in Europe",
                        Budget = 3200.0f,
                        Description = "Study music in Europe",
                        Tags = new List<string>{"study", "overseas"},
                        StartsAt = new DateTime(2013, 3, 1),
                        EndsAt = new DateTime(2013, 5, 4)
                    },
                    new Trip()
                    {
                        TripId = 107,
                        ShareId = new Guid("2E77BF06-A354-454B-8BCA-5F004C1AFB59"),
                        Name = "Conference talk about OData",
                        Budget = 2120.55f,
                        Description = "Conference talk about ODatan",
                        Tags = new List<string>{"odata", "overseas"},
                        StartsAt = new DateTime(2013, 7, 2),
                        EndsAt = new DateTime(2013, 7, 5)
                    },
                    new Trip()
                    {
                        TripId = 108,
                        ShareId = new Guid("E6E23FB2-C428-439E-BDAB-9283482F49F0"),
                        Name = "Vocation at hometown",
                        Budget = 1500.0f,
                        Description = "Vocation at hometown",
                        Tags = new List<string>{"voaction"},
                        StartsAt = new DateTime(2013, 10, 1),
                        EndsAt = new DateTime(2013, 10, 5)
                    },
                    new Trip()
                    {
                        TripId = 109,
                        ShareId = new Guid("FAE31279-35CE-4119-9BDC-53F6E19DD1C5"),
                        Name = "Business trip for tech training",
                        Budget = 100.0f,
                        Description = "Business trip for tech training",
                        Tags = new List<string>{"business"},
                        StartsAt = new DateTime(2013, 9, 1),
                        EndsAt = new DateTime(2013, 9, 4)
                    }
                }
            };

            Me.Friends = People;
            #endregion Me
        }
    }
}