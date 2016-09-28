// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Service.Sample.TrippinInMemory.Models;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Providers.InMemory.DataStoreManager;
using Microsoft.Restier.Providers.InMemory.Submit;
using Microsoft.Restier.Providers.InMemory.Utils;
using Microsoft.Restier.Publishers.OData.Model;
using Microsoft.Spatial;

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Api
{
    public class TrippinApi : ApiBase
    {
        private IDataStoreManager<string, TripPinDataSource> DataStoreManager
        {
            get { return this.GetApiService<IDataStoreManager<string, TripPinDataSource>>(); }
        }

        private string Key
        {
            get { return InMemoryProviderUtils.GetSessionId(); }
        }

        public TrippinApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        #region Entity Set

        [Resource]
        public IQueryable<Person> People
        {
            get
            {
                var datasource = DataStoreManager.GetDataStoreInstance(Key);
                if (datasource != null)
                {
                    return datasource.People.AsQueryable();
                }

                return null;
            }
        }

        [Resource]
        public IQueryable<Person> NewComePeople
        {
            get
            {
                var datasource = DataStoreManager.GetDataStoreInstance(Key);
                if (datasource != null)
                {
                    return datasource.People.AsQueryable();
                }

                return null;
            }
        }

        [Resource]
        public Person Me
        {
            get
            {
                var datasource = DataStoreManager.GetDataStoreInstance(Key);
                if (datasource != null)
                {
                    return datasource.Me;
                }

                return null;
            }
        }

        [Resource]
        public IQueryable<Airline> Airlines
        {
            get
            {
                var datasource = DataStoreManager.GetDataStoreInstance(Key);
                if (datasource != null)
                {
                    return datasource.Airlines.AsQueryable();
                }

                return null;
            }
        }

        [Resource]
        public IQueryable<Airport> Airports
        {
            get
            {
                var datasource = DataStoreManager.GetDataStoreInstance(Key);
                if (datasource != null)
                {
                    return datasource.Airports.AsQueryable();
                }

                return null;
            }
        }

        #endregion

        #region function/action

        /// <summary>
        ///     Unbound function, Get Person with most friends.
        /// </summary>
        /// <returns>
        ///     <see cref="Person">
        /// </returns>
        [Operation(EntitySet = "People")]
        public Person GetPersonWithMostFriends()
        {
            Person result = null;
            foreach (var person in People)
            {
                if (person.Friends == null)
                {
                    continue;
                }

                if (result == null)
                {
                    result = person;
                }

                if (person.Friends.Count > result.Friends.Count)
                {
                    result = person;
                }
            }

            return result;
        }

        /// <summary>
        ///     Unbound function, get nearest aireport to GeographyPoint(lat, lon).
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <returns>
        ///     <see cref="Airport">
        /// </returns>
        [Operation(EntitySet = "Airports")]
        public Airport GetNearestAirport(double lat, double lon)
        {
            var startPoint = GeographyPoint.Create(lat, lon);
            double minDistance = 2;
            Airport nearestAirport = null;

            foreach (var airport in Airports)
            {
                var distance = CalculateDistance(startPoint, airport.Location.Loc);
                if (distance < minDistance)
                {
                    nearestAirport = airport;
                    minDistance = distance;
                }
            }

            return nearestAirport;
        }

        [Operation(IsBound = true)]
        public Airline GetFavoriteAirline(Person person)
        {
            var countDict = new Dictionary<string, int>();
            foreach (var a in Airlines)
            {
                countDict.Add(a.AirlineCode, 0);
            }

            foreach (var t in person.Trips)
            {
                foreach (var p in t.PlanItems)
                {
                    var f = p as Flight;
                    if (f != null)
                    {
                        countDict[f.Airline.AirlineCode]++;
                    }
                }
            }

            var max = -1;
            string favoriteAirlineCode = null;
            foreach (var record in countDict)
            {
                if (max < record.Value)
                {
                    favoriteAirlineCode = record.Key;
                    max = record.Value;
                }
            }

            return Airlines.Single(a => a.AirlineCode.Equals(favoriteAirlineCode));
        }

        /// <summary>
        ///     Bound Function, get the trips of one friend with userName
        /// </summary>
        [Operation(IsBound = true)]
        public ICollection<Trip> GetFriendsTrips(Person person, string userName)
        {
            var friends = person.Friends.Where(p => p.UserName.Equals(userName)).ToArray();
            if (friends.Count() == 0)
            {
                //todo: in this case it should throw a 404 not found error.
                return new Collection<Trip>();
            }
            else
            {
                return friends[0].Trips;
            }
        }

        [Operation(IsBound = true)]
        public ICollection<Person> GetInvolvedPeople(Trip trip)
        {
            var shareID = trip.ShareId;
            ICollection<Person> sharingPersons = new Collection<Person>();

            foreach (var person in People)
            {
                if (person.Trips != null)
                {
                    foreach (var t in person.Trips)
                    {
                        if (shareID.Equals(t.ShareId))
                        {
                            sharingPersons.Add(person);
                            break;
                        }
                    }
                }
            }

            return sharingPersons;
        }

        /// <summary>
        ///     Unbound action, reset datasource.
        /// </summary>
        [Operation(HasSideEffects = true)]
        public void ResetDataSource()
        {
            DataStoreManager.ResetDataStoreInstance(Key);
        }

        /// <summary>
        ///     Bound Action, update the last name of one person.
        /// </summary>
        /// <param name="person">The person to be updated.</param>
        /// <param name="lastName">The value of last name to be updated.</param>
        /// <returns>True if update successfully.</returns>
        [Operation(IsBound = true)]
        public bool UpdatePersonLastName(Person person, string lastName)
        {
            if (person != null)
            {
                person.LastName = lastName;
                return true;
            }
            else
            {
                return false;
            }
        }

        [Operation(IsBound = true, HasSideEffects = true)]
        public void ShareTrip(Person personInstance, string userName, int tripId)
        {
            if (personInstance == null)
            {
                throw new ArgumentNullException("personInstance");
            }
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException("userName");
            }
            if (tripId < 0)
            {
                throw new ArgumentNullException("tripId");
            }

            var tripInstance = personInstance.Trips.FirstOrDefault(item => item.TripId == tripId);

            if (tripInstance == null)
            {
                throw new Exception(string.Format("Can't get trip with ID '{0}' in person '{1}'", tripId,
                    personInstance.UserName));
            }

            var friendInstance = personInstance.Friends.FirstOrDefault(item => item.UserName == userName);

            if (friendInstance == null)
            {
                throw new Exception(string.Format("Can't get friend with userName '{0}' in person '{1}'", userName,
                    personInstance.UserName));
            }

            if (friendInstance.Trips != null && friendInstance.Trips.All(item => item.TripId != tripId))
            {
                //TODO, should return 201 if we add new entity, those behavior should be update in handler.
                var newTrip = tripInstance.Clone() as Trip;
                var maxTripId = friendInstance.Trips.Select(item => item.TripId).Max();
                newTrip.TripId = maxTripId + 1;
                friendInstance.Trips.Add(newTrip);
            }
        }

        private static double CalculateDistance(GeographyPoint p1, GeographyPoint p2)
        {
            // using Haversine formula
            // refer to http://en.wikipedia.org/wiki/Haversine_formula.
            var lat1 = Math.PI*p1.Latitude/180;
            var lat2 = Math.PI*p2.Latitude/180;
            var lon1 = Math.PI*p1.Longitude/180;
            var lon2 = Math.PI*p2.Longitude/180;
            var item1 = Math.Sin((lat1 - lat2)/2)*Math.Sin((lat1 - lat2)/2);
            var item2 = Math.Cos(lat1)*Math.Cos(lat2)*Math.Sin((lon1 - lon2)/2)*Math.Sin((lon1 - lon2)/2);
            return Math.Asin(Math.Sqrt(item1 + item2));
        }

        #endregion

        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            Func<IServiceProvider, IDataStoreManager<string, TripPinDataSource>> defaultDataStoreManager =
                sp => new DefaultDataStoreManager<string, TripPinDataSource>()
                {
                    MaxDataStoreInstanceCapacity = 1000,
                    MaxDataStoreInstanceLifeTime = new TimeSpan(0, 30, 0)
                };

            services.AddService<IModelBuilder>((sp, next) => new ModelBuilder());
            services.AddService<IChangeSetInitializer>((sp, next) => new ChangeSetInitializer<TripPinDataSource>());
            services.AddService<ISubmitExecutor>((sp, next) => new SubmitExecutor());
            services.AddSingleton(defaultDataStoreManager);
            return ApiBase.ConfigureApi(apiType, services);
        }

        private class ModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var modelBuilder = new ODataConventionModelBuilder();
                modelBuilder.Namespace = "Microsoft.OData.Service.Sample.TrippinInMemory.Models";
                modelBuilder.EntitySet<Person>("People");
                modelBuilder.EntitySet<Airline>("Airlines");
                modelBuilder.EntitySet<Airport>("Airports");
                return Task.FromResult(modelBuilder.GetEdmModel());
            }
        }
    }
}