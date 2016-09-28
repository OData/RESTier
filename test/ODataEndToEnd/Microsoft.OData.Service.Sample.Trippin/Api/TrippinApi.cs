// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Query;
using System.Web.OData.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Service.Sample.Trippin.Extension;
using Microsoft.OData.Service.Sample.Trippin.Models;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Providers.EntityFramework;
using Microsoft.Restier.Publishers.OData.Model;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web.OData.Builder;
using Microsoft.OData.Edm.Vocabularies;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {
        [NotMapped]
        [IgnoreDataMember]
        public TrippinModel ModelContext
        {
            get { return DbContext; }
        }

        [Resource]
        public Person Me
        {
            get
            {
                // Cannot use this.GetQueryableSource<Person>("People") as this should be return a single value
                return DbContext.People
                    .Include("Friends")
                    .Include("Trips")
                    .Single(p => p.PersonId == 1);
            }
        }

        [Resource]
        public IQueryable<Flight> Flights1
        {
            get { return DbContext.Flights; }
        }

        [Resource]
        public IQueryable<Flight> Flights2
        {
            get { return this.GetQueryableSource<Flight>("Flights"); }
        }

        [Resource]
        public IQueryable<PersonWithAge> PeopleWithAge
        {
            get
            {
                return DbContext.People.Select(p => new PersonWithAge
                {
                    Id = p.PersonId,
                    UserName = p.UserName,
                    FirstName = p.FirstName,
                    LastName = p.LastName
                });
            }
        }

        [Resource]
        public IQueryable<PersonWithAge> PeopleWithAge1
        {
            get
            {
                return this.GetQueryableSource<Person>("People").Select(p => new PersonWithAge
                {
                    Id = p.PersonId,
                    UserName = p.UserName,
                    FirstName = p.FirstName,
                    LastName = p.LastName
                });
            }
        }

        [Resource]
        public PersonWithAge PeopleWithAgeMe
        {
            get
            {
                return ModelContext.People.Select(p => new PersonWithAge
                {
                    Id = p.PersonId,
                    UserName = p.UserName,
                    FirstName = p.FirstName,
                    LastName = p.LastName
                }).Single(p => p.Id == 1);
            }
        }

        protected IQueryable<Staff> OnFilterStaff(IQueryable<Staff> entitySet)
        {
            return entitySet.Where(s => s.StaffId % 2 == 0).AsQueryable();
        }

        protected IQueryable<Conference> OnFilterConference(IQueryable<Conference> entitySet)
        {
            return entitySet.Where(c => c.ConferenceId % 2 == 0).AsQueryable();
        }

        protected IQueryable<Sponsor> OnFilterSponsor(IQueryable<Sponsor> entitySet)
        {
            return entitySet.Where(s => s.SponsorId % 2 == 0).AsQueryable();
        }

        private IQueryable<Person> PeopleWithFriends
        {
            get { return ModelContext.People.Include("Friends"); }
        }

        /// <summary>
        /// Implements an action import.
        /// Default namespace will be same as the namespace of entity set and entity type
        /// </summary>
        [Operation(HasSideEffects = true)]
        public void ResetDataSource()
        {
            TrippinModel.ResetDataSource();
        }

        /// <summary>
        /// Action import - clean up all the expired trips.
        /// Specified namespace will be used.
        /// </summary>
        [Operation(Namespace = "Different.Namespace", HasSideEffects = true)]
        public void CleanUpExpiredTrips()
        {
            // DO NOT ACTUALLY REMOVE THE TRIPS.
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public void CleanUpTrip(int id, Location location, Feature feature)
        {
            // Only for testing passed in parameters
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public void CleanUpTrips(IEnumerable<int> ids, IEnumerable<Location> locations, IEnumerable<Feature> features)
        {
            // Only for testing passed in parameters
        }

        /// <summary>
        /// Bound action - set the end-up time of a trip.
        /// </summary>
        /// <param name="passedInTrip">The trip to update.</param>
        /// <returns>The trip updated.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", IsBound = true, HasSideEffects = true)]
        public Trip EndTrip(Trip passedInTrip)
        {
            // DO NOT ACTUALLY UPDATE THE TRIP.
            return passedInTrip;
        }

        [Operation(IsBound = true)]
        public ICollection<Person> GetPersonFriends(Person person)
        {
            if (person == null)
            {
                return null;
            }

            var personWithFriends = PeopleWithFriends.Single(p => p.PersonId == person.PersonId);
            return personWithFriends.Friends;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", IsBound = true, HasSideEffects = true)]
        public Location EndTripWithPara(Trip trip, int id, Location location, Feature feature)
        {
            // Test kinds of different passed in parameters
            return location;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", IsBound = true, HasSideEffects = true)]
        public IEnumerable<Trip> EndTripsIEnumerable(IEnumerable<Trip> trips, IEnumerable<int> ids, IEnumerable<Location> locations, IEnumerable<Feature> features)
        {
            // Test kinds of different passed in parameters
            return trips;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", IsBound = true, HasSideEffects = true)]
        public ICollection<int> EndTripsICollection(ICollection<Trip> trips, ICollection<int> ids, ICollection<Location> locations, ICollection<Feature> features)
        {
            // Test kinds of different passed in parameters
            return ids;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", IsBound = true, HasSideEffects = true)]
        public Location[] EndTripsArray(Trip[] trips, int[] ids, Location[] locations, Feature[] features)
        {
            // Test kinds of different passed in parameters
            return locations;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public int ActionPrimitive()
        {
            return 100;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public int? ActionNullPrimitive()
        {
            return null;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public Feature ActionEnum(Feature f)
        {
            return f;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public Feature? ActionNullEnum()
        {
            return null;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public Location ActionComplex(Location l)
        {
            return l;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public int[] ActionPrimitiveArray(int[] intArray)
        {
            return intArray;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public ICollection<Feature> ActionEnumCollection(ICollection<Feature> coll)
        {
            return coll;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public ICollection<Location> ActionComplexCollection(ICollection<Location> coll)
        {
            return coll;
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public IEnumerable<Person> ActionWithException()
        {
            throw new ArgumentException("Test get function throw exception");
        }

        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public void ActionForAuthorization()
        {
        }

        protected bool CanExecuteActionForAuthorization()
        {
            // Can be checked based on current login user
            return false;
        }

        /// <summary>
        /// Bound function - gets the number of friends of a person.
        /// </summary>
        /// <param name="person">The key of the binding person.</param>
        /// <returns>The number of friends of the person.</returns>
        [Operation(IsBound = true)]
        public int GetNumberOfFriends(Person person)
        {
            if (person == null)
            {
                return 0;
            }

            var personWithFriends = PeopleWithFriends.Single(p => p.PersonId == person.PersonId);
            return personWithFriends.Friends == null ? 0 : personWithFriends.Friends.Count;
        }

        /// <summary>
        /// Bound function - bound to entity set with one parameter.
        /// </summary>
        /// <param name="people">The binding entity set.</param>
        /// <param name="n">Test parameter.</param>
        /// <returns>Single value.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", IsBound = true)]
        public int GetBoundEntitySetIEnumerable(IEnumerable<Person> people, int n)
        {
            return n * 10;
        }

        /// <summary>
        /// Bound function - bound to entity set with two parameters.
        /// </summary>
        /// <param name="people">The binding entity set.</param>
        /// <param name="n">Test parameter.</param>
        /// <param name="m">Test parameter.</param>
        /// <returns>Single value.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", IsBound = true)]
        public int GetBoundEntitySetICollection(ICollection<Person> people, int n, int m)
        {
            return n * m;
        }

        /// <summary>
        /// Bound function - bound to entity set with two parameters.
        /// </summary>
        /// <param name="people">The binding entity set.</param>
        /// <param name="n">Test parameter.</param>
        /// <param name="m">Test parameter.</param>
        /// <returns>Single value.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", IsBound = true)]
        public int GetBoundEntitySetArray(Person[] people, int n, int m)
        {
            return n * m;
        }

        /// <summary>
        /// Function import - For non-null primitive test cases
        /// </summary>
        /// <returns>value</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public int GetPrimitive()
        {
            return 100;
        }

        /// <summary>
        /// Function import - Return null for primitive type null test cases
        /// </summary>
        /// <returns>The value.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public int? GetNullPrimitive()
        {
            return null;
        }

        /// <summary>
        /// Function import - Test parameter is enum and return type is enum.
        /// </summary>
        /// <param name="f">Feature.</param>
        /// <returns>An enum.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public Feature GetEnum(Feature f)
        {
            return f;
        }

        /// <summary>
        /// Function import - Test return null enum.
        /// </summary>
        /// <returns>null.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public Feature? GetNullEnum()
        {
            return null;
        }

        /// <summary>
        /// Function import - Test parameter is complex and return type is complex.
        /// </summary>
        /// <param name="l">The complex type.</param>
        /// <returns>A complex.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public Location GetComplex(Location l)
        {
            return l;
        }

        /// <summary>
        /// Function import - gets the person with most friends.
        /// </summary>
        /// <returns>The person with most friends.</returns>
        [Operation(EntitySet = "People")]
        public Person GetPersonWithMostFriends()
        {
            Person result = null;

            foreach (var person in PeopleWithFriends)
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
        /// Function import -  Return null for entity null case testing
        /// </summary>
        /// <returns>null.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", EntitySet = "People")]
        public Person GetNullEntity()
        {
            return null;
        }

        /// <summary>
        /// Function import - gets people with at least n friends.
        /// </summary>
        /// <param name="n">The minimum number of friends.</param>
        /// <returns>People with at least n friends.</returns>
        [Operation]
        public IEnumerable<Person> GetPeopleWithFriendsAtLeast(int n)
        {
            foreach (var person in PeopleWithFriends)
            {
                if (person.Friends == null)
                {
                    continue;
                }

                if (person.Friends.Count >= n)
                {
                    yield return person;
                }
            }
        }

        /// <summary>
        /// Function import - gets people with at least n friends and most of m friends.
        /// </summary>
        /// <param name="n">The minimum number of friends.</param>
        /// <param name="m">The maximum number of friends.</param>
        /// <returns>People with at least n friends.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", EntitySet = "People")]
        public IEnumerable<Person> GetPeopleWithFriendsAtLeastMost(int n, int m)
        {
            if (n > m)
            {
                yield return null;
            }

            foreach (var person in PeopleWithFriends)
            {
                if (person.Friends == null)
                {
                    continue;
                }

                if (person.Friends.Count >= n && person.Friends.Count <= m)
                {
                    yield return person;
                }
            }
        }

        /// <summary>
        /// Test null collection case
        /// </summary>
        /// <returns>null for test only.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", EntitySet = "People")]
        public IEnumerable<Person> GetNullEntityCollection()
        {
            return null;
        }

        /// <summary>
        /// Function import - Test parameter is IEnumerable of int and return type is IEnumerable of int.
        /// </summary>
        /// <param name="intEnumerable">The IEnumerable of int.</param>
        /// <returns>A IEnumerable of int.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public IEnumerable<int> GetIEnumerable(IEnumerable<int> intEnumerable)
        {
            return intEnumerable;
        }

        /// <summary>
        /// Function import - Test parameter is ICollection of int and return type is ICollection of int.
        /// </summary>
        /// <param name="intColl">The ICollection of int.</param>
        /// <returns>A ICollection of int.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public ICollection<int> GetICollection(ICollection<int> intColl)
        {
            return intColl;
        }

        /// <summary>
        /// Function import - Test parameter is Array of int and return type is Array of int.
        /// </summary>
        /// <param name="intArray">The Array of int.</param>
        /// <returns>A Array of int.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public int[] GetArray(int[] intArray)
        {
            return intArray;
        }

        /// <summary>
        /// Function import - Test parameter is enum collection and return type is enum collection.
        /// </summary>
        /// <param name="coll">Feature collection.</param>
        /// <returns>An enum collection.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public ICollection<Feature> GetEnumCollection(ICollection<Feature> coll)
        {
            return coll;
        }

        /// <summary>
        /// Function import - Test parameter is complex collection and return type is complex collection.
        /// </summary>
        /// <param name="coll">The complex type collection.</param>
        /// <returns>A complex collection.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public ICollection<Location> GetComplexCollection(ICollection<Location> coll)
        {
            return coll;
        }

        /// <summary>
        /// Test function exception case
        /// </summary>
        /// <returns>null for test only.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public IEnumerable<Person> GetWithException()
        {
            throw new ArgumentException("Test get function throw exception");
        }

        /// <summary>
        /// Bound Function - For bound flag testing
        /// </summary>
        /// <returns>value</returns>
        [Operation(IsBound = true)]
        public int GetBoundPrimitive(int i)
        {
            return i * 100;
        }

        /// <summary>
        /// Bound Function - For bound flag testing
        /// </summary>
        /// <param name="l">The complex type.</param>
        /// <returns>A complex.</returns>
        [Operation(IsBound = true)]
        public Location GetBoundComplex(Location l)
        {
            return l;
        }

        protected bool CanDeleteTrips()
        {
            return false;
        }

        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            // Add customized OData validation settings 
            Func<IServiceProvider, ODataValidationSettings> validationSettingFactory = sp => new ODataValidationSettings
            {
                MaxAnyAllExpressionDepth =3,
                MaxExpansionDepth = 3
            };

            services.AddService<IModelBuilder, TrippinModelExtender>();

            return EntityFrameworkApi<TrippinModel>.ConfigureApi(apiType, services)
                .AddSingleton<ODataPayloadValueConverter, CustomizedPayloadValueConverter>()
                .AddSingleton<ODataValidationSettings>(validationSettingFactory)
                .AddSingleton<IODataPathHandler, PathAndSlashEscapeODataPathHandler>()
                .AddService<IChangeSetItemFilter, CustomizedSubmitFilter>()
                .AddService<IModelBuilder, TrippinModelCustomizer>();
        }


        private class TrippinModelExtender : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var builder = new ODataConventionModelBuilder();
                builder.EntityType<PersonWithAge>();
                return Task.FromResult(builder.GetEdmModel());
            }
        }

        private class TrippinModelCustomizer : IModelBuilder
        {
            public IModelBuilder InnerHandler { get; set; }

            public async Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var model = await InnerHandler.GetModelAsync(context, cancellationToken);

                var trueConstant = new EdmBooleanConstant(true);

                // Set computed annotation
                var tripType = (EdmEntityType)model.SchemaElements.Single(e => e.Name == "Trip");
                var trackGuidProperty = tripType.DeclaredProperties.Single(prop => prop.Name == "TrackGuid");
                var timeStampValueProp = model.EntityContainer.FindEntitySet("Airlines").EntityType().FindProperty("TimeStampValue");
                var computedTerm = new EdmTerm("Org.OData.Core.V1", "Computed", EdmPrimitiveTypeKind.Boolean);
                var anno1 = new EdmVocabularyAnnotation(trackGuidProperty, computedTerm, trueConstant);
                var anno2 = new EdmVocabularyAnnotation(timeStampValueProp, computedTerm, trueConstant);
                ((EdmModel)model).SetVocabularyAnnotation(anno1);
                ((EdmModel)model).SetVocabularyAnnotation(anno2);

                var immutableTerm = new EdmTerm("Org.OData.Core.V1", "Immutable", EdmPrimitiveTypeKind.Boolean);

                var orderType = (EdmEntityType)model.SchemaElements.Single(e => e.Name == "Order");
                var orderProp1 = orderType.DeclaredProperties.Single(prop => prop.Name == "ComputedProperty");
                var orderProp2 = orderType.DeclaredProperties.Single(prop => prop.Name == "ImmutableProperty");
                var orderProp3 = orderType.DeclaredProperties.Single(prop => prop.Name == "ComputedOrderDetail");
                var orderProp4 = orderType.DeclaredProperties.Single(prop => prop.Name == "ImmutableOrderDetail");

                ((EdmModel)model).SetVocabularyAnnotation(new EdmVocabularyAnnotation(orderProp1, computedTerm, trueConstant));
                ((EdmModel)model).SetVocabularyAnnotation(new EdmVocabularyAnnotation(orderProp2, immutableTerm, trueConstant));
                ((EdmModel)model).SetVocabularyAnnotation(new EdmVocabularyAnnotation(orderProp3, computedTerm, trueConstant));
                ((EdmModel)model).SetVocabularyAnnotation(new EdmVocabularyAnnotation(orderProp4, immutableTerm, trueConstant));

                var orderDetailType = (EdmComplexType)model.SchemaElements.Single(e => e.Name == "OrderDetail");
                var detailProp1 = orderDetailType.DeclaredProperties.Single(prop => prop.Name == "ComputedProperty");
                var detailProp2 = orderDetailType.DeclaredProperties.Single(prop => prop.Name == "ImmutableProperty");
                ((EdmModel)model).SetVocabularyAnnotation(new EdmVocabularyAnnotation(detailProp1, computedTerm, trueConstant));
                ((EdmModel)model).SetVocabularyAnnotation(new EdmVocabularyAnnotation(detailProp2, immutableTerm, trueConstant));

                var personType = (EdmEntityType)model.SchemaElements.Single(e => e.Name == "Person");
                var type = personType.FindProperty("PersonId").Type;

                var isNullableField = typeof(EdmTypeReference).GetField("isNullable", BindingFlags.Instance | BindingFlags.NonPublic);
                if (isNullableField != null)
                {
                    isNullableField.SetValue(type, false);
                }

                return model;
            }
        }

        public TrippinApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}