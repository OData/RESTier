// // Copyright (c) Microsoft Corporation.  All rights reserved.
// // Licensed under the MIT License.  See License.txt in the project root for license information.

#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Service.Library.DataStoreManager;
using Microsoft.OData.Service.Library.Utils;
using Microsoft.OData.Service.Sample.TrippinInMemory.Models;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Publishers.OData.Model;
using Microsoft.Spatial;

#endregion

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Api
{
    public class TrippinApi : ApiBase
    {
        private static IDataStoreManager<string, TripPinDataSource> _dataStoreManager
            = new DefaultDataStoreManager<string, TripPinDataSource>()
            {
                MaxDataStoreInstanceCapacity = 1000,
                MaxDataStoreInstanceLifeTime = new TimeSpan(0, 30, 0)
            };

        private string Key
        {
            get { return LibraryUtils.GetSessionId(); }
        }

        #region Entity Set

        public IQueryable<Person> People
        {
            get
            {
                var datasource = _dataStoreManager.GetDataStoreInstance(Key);
                if (datasource != null)
                {
                    return datasource.People.AsQueryable();
                }

                return null;
            }
        }

        public IQueryable<Person> NewComePeople
        {
            get
            {
                var datasource = _dataStoreManager.GetDataStoreInstance(Key);
                if (datasource != null)
                {
                    return datasource.People.AsQueryable();
                }

                return null;
            }
        }

        public Person Me
        {
            get
            {
                var datasource = _dataStoreManager.GetDataStoreInstance(Key);
                if (datasource != null)
                {
                    return datasource.Me;
                }

                return null;
            }
        }

        public IQueryable<Airline> Airlines
        {
            get
            {
                var datasource = _dataStoreManager.GetDataStoreInstance(Key);
                if (datasource != null)
                {
                    return datasource.Airlines.AsQueryable();
                }

                return null;
            }
        }

        public IQueryable<Airport> Airports
        {
            get
            {
                var datasource = _dataStoreManager.GetDataStoreInstance(Key);
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
            _dataStoreManager.ResetDataStoreInstance(Key);
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

        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            services.AddService<IModelBuilder>((sp, next) => new ModelBuilder());
            services.AddService<IChangeSetInitializer>((sp, next) => new CustomerizedChangeSetInitializer());
            services.AddService<ISubmitExecutor>((sp, next) => new CustomerizedSubmitExecutor());
            return base.ConfigureApi(services);
        }

        private class ModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var modelBuilder = new ODataConventionModelBuilder();
                modelBuilder.EntityType<Person>();
                return Task.FromResult(modelBuilder.GetEdmModel());
            }
        }

        #region Services

        private class CustomerizedSubmitExecutor : ISubmitExecutor
        {
            public Task<SubmitResult> ExecuteSubmitAsync(SubmitContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(new SubmitResult(context.ChangeSet));
            }
        }

        /// <summary>
        /// ChangeSetInitializer class.
        /// Since our datasource is in memory,
        /// we just confirm the data change here, not in SubmitExecutor
        /// </summary>
        private class CustomerizedChangeSetInitializer : IChangeSetInitializer
        {
            public Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken)
            {
                var key = LibraryUtils.GetSessionId();
                var dataSource = _dataStoreManager.GetDataStoreInstance(key);
                foreach (var dataModificationItem in context.ChangeSet.Entries.OfType<DataModificationItem>())
                {
                    var expectedEntiType = dataModificationItem.ExpectedResourceType;
                    var operation = dataModificationItem.DataModificationItemAction;
                    object entity;
                    switch (operation)
                    {
                        case DataModificationItemAction.Insert:
                        {
                            // Here we create a instance of entity, parameters are from the request.
                            // Known issues: 1) not support odata.id
                            //               2) not support nested entity.
                            entity = Activator.CreateInstance(expectedEntiType);
                            SetValues(entity, expectedEntiType, dataModificationItem.LocalValues);
                            dataModificationItem.Resource = entity;

                            // insert new entity into entity set
                            var entitySetProp = GetEntitySetPropertyInfoFromDataModificationItem(dataSource,
                                dataModificationItem);

                            if (entitySetProp != null && entitySetProp.CanWrite)
                            {
                                var originSet = entitySetProp.GetValue(dataSource);
                                entitySetProp.PropertyType.GetMethod("Add").Invoke(originSet, new[] {entity});
                            }
                        }
                            break;
                        case DataModificationItemAction.Update:
                        {
                            entity = FindEntity(dataSource, context, dataModificationItem, cancellationToken);
                            dataModificationItem.Resource = entity;

                            // update the entity
                            if (entity != null)
                            {
                                SetValues(entity, expectedEntiType, dataModificationItem.LocalValues);
                            }
                        }
                            break;
                        case DataModificationItemAction.Remove:
                        {
                            entity = FindEntity(dataSource, context, dataModificationItem, cancellationToken);
                            dataModificationItem.Resource = entity;

                            // remove the entity
                            if (entity != null)
                            {
                                var entitySetProp = GetEntitySetPropertyInfoFromDataModificationItem(dataSource,
                                    dataModificationItem);

                                if (entitySetProp != null && entitySetProp.CanWrite)
                                {
                                    var originSet = entitySetProp.GetValue(dataSource);
                                    entitySetProp.PropertyType.GetMethod("Remove").Invoke(originSet, new[] {entity});
                                }
                            }
                        }
                            break;
                        case DataModificationItemAction.Undefined:
                        {
                            throw new NotImplementedException();
                        }
                    }
                }

                return Task.WhenAll();
            }

            private static void SetValues(object instance, Type type, IReadOnlyDictionary<string, object> values)
            {
                foreach (KeyValuePair<string, object> propertyPair in values)
                {
                    object value = propertyPair.Value;
                    PropertyInfo propertyInfo = type.GetProperty(propertyPair.Key);
                    if (value == null)
                    {
                        // If the property value is null, we set null in the object too.
                        propertyInfo.SetValue(instance, null);
                        continue;
                    }

                    if (!propertyInfo.PropertyType.IsInstanceOfType(value))
                    {
                        var dic = value as IReadOnlyDictionary<string, object>;
                        var col = value as System.Web.OData.EdmComplexObjectCollection;

                        if (dic != null)
                        {
                            value = Activator.CreateInstance(propertyInfo.PropertyType);
                            SetValues(value, propertyInfo.PropertyType, dic);
                        }
                        else if (col != null)
                        {
                            var realType = propertyInfo.PropertyType.GenericTypeArguments[0];
                            var valueType = typeof(Collection<>).MakeGenericType(realType);
                            value = Activator.CreateInstance(valueType);
                            foreach (var c in col)
                            {
                                value.GetType().GetMethod("Add").Invoke(value, new[] {c});
                            }
                        }
                        else
                        {
                            throw new NotSupportedException(string.Format(
                                CultureInfo.InvariantCulture,
                                propertyPair.Key));
                        }
                    }

                    propertyInfo.SetValue(instance, value);
                }
            }

            private static object FindEntity(
                object instance,
                SubmitContext context,
                DataModificationItem item,
                CancellationToken cancellationToken)
            {
                var entitySetPropertyInfo = GetEntitySetPropertyInfoFromDataModificationItem(instance, item);
                var originSet = entitySetPropertyInfo.GetValue(instance);

                object entity = null;
                var enumerableSet = originSet as IEnumerable<object>;
                if (enumerableSet != null)
                {
                    foreach (var o in enumerableSet)
                    {
                        var foundFlag = true;
                        foreach (var keyVal in item.ResourceKey)
                        {
                            var entityProp = o.GetType().GetProperty(keyVal.Key);
                            if (entityProp != null)
                            {
                                foundFlag &= entityProp.GetValue(o).Equals(keyVal.Value);
                            }
                            else
                            {
                                foundFlag = false;
                            }

                            if (!foundFlag)
                            {
                                break;
                            }
                        }

                        if (foundFlag)
                        {
                            entity = o;
                            break;
                        }
                    }
                }

                return entity;
            }

            private static PropertyInfo GetEntitySetPropertyInfoFromDataModificationItem(object instance,
                DataModificationItem dataModificationItem)
            {
                var entitySetName = dataModificationItem.ResourceSetName;
                var entitySetProp = instance.GetType()
                    .GetProperty(entitySetName, BindingFlags.Public | BindingFlags.Instance);
                return entitySetProp;
            }
        }

        #endregion
    }
}