﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Core;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.WebApi.Model;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Submit;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Api
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

        private IQueryable<Person> PeopleWithFriends
        {
            get { return Context.People.Include("Friends"); }
        }

        /// <summary>
        /// Implements an action import.
        /// TODO: This method is only for building the model.
        /// </summary>
        [Operation(Namespace = "Microsoft.Restier.WebApi.Test.Services.Trippin.Models", HasSideEffects = true)]
        public void ResetDataSource()
        {
            TrippinModel.ResetDataSource();
        }

        /// <summary>
        /// Action import - clean up all the expired trips.
        /// </summary>
        [Operation(Namespace = "Microsoft.Restier.WebApi.Test.Services.Trippin.Models", HasSideEffects = true)]
        public void CleanUpExpiredTrips()
        {
            // DO NOT ACTUALLY REMOVE THE TRIPS.
        }

        /// <summary>
        /// Bound action - set the end-up time of a trip.
        /// </summary>
        /// <param name="trip">The trip to update.</param>
        /// <returns>The trip updated.</returns>
        [Operation(Namespace = "Microsoft.Restier.WebApi.Test.Services.Trippin.Models", HasSideEffects = true)]
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
        [Operation(Namespace = "Microsoft.Restier.WebApi.Test.Services.Trippin.Models")]
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
        /// Function import - gets the person with most friends.
        /// </summary>
        /// <returns>The person with most friends.</returns>
        [Operation(Namespace = "Microsoft.Restier.WebApi.Test.Services.Trippin.Models", EntitySet = "People")]
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
        /// Function import - gets people with at least n friends.
        /// </summary>
        /// <param name="n">The minimum number of friends.</param>
        /// <returns>People with at least n friends.</returns>
        [Operation(Namespace = "Microsoft.Restier.WebApi.Test.Services.Trippin.Models", EntitySet = "People")]
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

        protected bool CanDeleteTrips()
        {
            return false;
        }

        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            // Add customized OData valiadtion settings 
            Func<IServiceProvider, ODataValidationSettings> validationSettingFactory = (sp) => new ODataValidationSettings
            {
                MaxAnyAllExpressionDepth =3,
                MaxExpansionDepth = 3
            };

            return base.ConfigureApi(services)
                .AddSingleton<ODataPayloadValueConverter, CustomizedPayloadValueConverter>()
                .AddSingleton<ODataValidationSettings>(validationSettingFactory)
                .AddService<IChangeSetItemProcessor, CustomizedSubmitProcessor>();
        }
    }
}