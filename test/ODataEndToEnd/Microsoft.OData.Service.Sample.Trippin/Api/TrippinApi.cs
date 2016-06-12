// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Query;
using System.Web.OData.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Library.Annotations;
using Microsoft.OData.Edm.Library.Values;
using Microsoft.OData.Service.Sample.Trippin.Extension;
using Microsoft.OData.Service.Sample.Trippin.Models;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Providers.EntityFramework;
using Microsoft.Restier.Publishers.OData.Model;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {
        public new TrippinModel Context { get { return DbContext; } }

        public Person Me
        {
            get
            {
                return DbContext.People
                    .Include("Friends")
                    .Include("Trips")
                    .Single(p => p.PersonId == 1);
            }
        }

        protected IQueryable<Person> OnFilterPeople(IQueryable<Person> entitySet)
        {
            return entitySet.Where(s => s.PersonId % 2 == 0).AsQueryable();
        }

        protected IQueryable<Person> OnFilterPerson(IQueryable<Person> entitySet)
        {
            return entitySet.Where(s => s.PersonId % 2 == 0).AsQueryable();
        }

        protected IQueryable<Trip> OnFilterTrips(IQueryable<Trip> entitySet)
        {
            return entitySet.Where(s => s.TripId % 2 == 0).AsQueryable();
        }

        protected IQueryable<Trip> OnFilterTrip(IQueryable<Trip> entitySet)
        {
            return entitySet.Where(s => s.TripId % 2 == 0).AsQueryable();
        }

        private IQueryable<Person> PeopleWithFriends
        {
            get { return Context.People.Include("Friends"); }
        }

        /// <summary>
        /// Implements an action import.
        /// TODO: This method is only for building the model.
        /// </summary>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public void ResetDataSource()
        {
            TrippinModel.ResetDataSource();
        }

        /// <summary>
        /// Action import - clean up all the expired trips.
        /// </summary>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public void CleanUpExpiredTrips()
        {
            // DO NOT ACTUALLY REMOVE THE TRIPS.
        }

        /// <summary>
        /// Bound action - set the end-up time of a trip.
        /// </summary>
        /// <param name="trip">The trip to update.</param>
        /// <returns>The trip updated.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public Trip EndTrip(Trip trip)
        {
            // DO NOT ACTUALLY UPDATE THE TRIP.
            return trip;
        }

        /// <summary>
        /// Bound function - gets the number of friends of a person.
        /// </summary>
        /// <param name="person">The key of the binding person.</param>
        /// <returns>The number of friends of the person.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
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
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public int GetBoundEntitySetIEnumerable(IEnumerable<Person> people, int n)
        {
            return n*10;
        }

        /// <summary>
        /// Bound function - bound to entity set with two parameters.
        /// </summary>
        /// <param name="people">The binding entity set.</param>
        /// <param name="n">Test parameter.</param>
        /// <param name="m">Test parameter.</param>
        /// <returns>Single value.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
        public int GetBoundEntitySetICollection(ICollection<Person> people, int n, int m)
        {
            return n*m;
        }

        /// <summary>
        /// Bound function - bound to entity set with two parameters.
        /// </summary>
        /// <param name="people">The binding entity set.</param>
        /// <param name="n">Test parameter.</param>
        /// <param name="m">Test parameter.</param>
        /// <returns>Single value.</returns>
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models")]
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
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", EntitySet = "People")]
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
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", EntitySet = "People")]
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

        protected bool CanDeleteTrips()
        {
            return false;
        }

        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            // Add customized OData validation settings 
            Func<IServiceProvider, ODataValidationSettings> validationSettingFactory = sp => new ODataValidationSettings
            {
                MaxAnyAllExpressionDepth =3,
                MaxExpansionDepth = 3
            };

            return base.ConfigureApi(services)
                .AddSingleton<ODataPayloadValueConverter, CustomizedPayloadValueConverter>()
                .AddSingleton<ODataValidationSettings>(validationSettingFactory)
                .AddSingleton<IODataPathHandler, PathAndSlashEscapeODataPathHandler>()
                .AddService<IChangeSetItemProcessor, CustomizedSubmitProcessor>()
                .AddService<IModelBuilder, TrippinModelExtender>();
        }


        private class TrippinModelExtender : IModelBuilder
        {
            public IModelBuilder InnerHandler { get; set; }

            public async Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var model = await InnerHandler.GetModelAsync(context, cancellationToken);

                // Set computed annotation
                var tripType = (EdmEntityType)model.SchemaElements.Single(e => e.Name == "Trip");
                var trackGuidProperty = tripType.DeclaredProperties.Single(prop => prop.Name == "TrackGuid");
                var timeStampValueProp= model.EntityContainer.FindEntitySet("Airlines").EntityType().FindProperty("TimeStampValue");
                var term = new EdmTerm("Org.OData.Core.V1", "Computed", EdmPrimitiveTypeKind.Boolean);
                var anno1 = new EdmAnnotation(trackGuidProperty, term, new EdmBooleanConstant(true));
                var anno2 = new EdmAnnotation(timeStampValueProp, term, new EdmBooleanConstant(true));
                ((EdmModel)model).SetVocabularyAnnotation(anno1);
                ((EdmModel)model).SetVocabularyAnnotation(anno2);

                return model;
            }
        }
    }
}