// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Service.Sample.Trippin.Api;
using Microsoft.OData.Service.Sample.Trippin.Models;

namespace Microsoft.OData.Service.Sample.Trippin.Controllers
{
    public class TrippinController : ODataController
    {
        private TrippinApi api;

        private TrippinApi Api
        {
            get
            {
                if (api == null)
                {
                    api = new TrippinApi();
                }

                return api;
            }
        }

        private TrippinModel DbContext
        {
            get
            {
                return Api.Context;
            }
        }

        /// <summary>
        /// TODO: This method is for actual executing.
        /// </summary>
        [ODataRoute("ResetDataSource")]
        public void ResetDataSource()
        {
            TrippinModel.ResetDataSource();
        }

        [ODataRoute("CleanUpExpiredTrips")]
        public void CleanUpExpiredTrips()
        {
            Api.CleanUpExpiredTrips();
        }

        [ODataRoute("Trips({key})/Microsoft.OData.Service.Sample.Trippin.Models.EndTrip")]
        public IHttpActionResult EndTrip(int key)
        {
            var trip = DbContext.Trips.SingleOrDefault(t => t.TripId == key);
            return Ok(Api.EndTrip(trip));
        }

        private bool PeopleExists(int key)
        {
            return DbContext.People.Any(p => p.PersonId == key);
        }

        private string GetServiceRootUri()
        {
            var routeName = Request.ODataProperties().RouteName;
            ODataRoute odataRoute = Configuration.Routes[routeName] as ODataRoute;
            var prefixName = odataRoute.RoutePrefix;
            var requestUri = Request.RequestUri.ToString();
            var serviceRootUri = requestUri.Substring(0, requestUri.IndexOf(prefixName, StringComparison.InvariantCultureIgnoreCase) + prefixName.Length);
            return serviceRootUri;
        }

        [ODataRoute("People({key})/Microsoft.OData.Service.Sample.Trippin.Models.GetNumberOfFriends")]
        public IHttpActionResult GetNumberOfFriends([FromODataUri]int key)
        {
            var person = DbContext.People.SingleOrDefault(p => p.PersonId == key);
            return Ok(Api.GetNumberOfFriends(person));
        }

        [ODataRoute("GetPersonWithMostFriends")]
        public IHttpActionResult GetPersonWithMostFriends()
        {
            return Ok(Api.GetPersonWithMostFriends());
        }

        [EnableQuery]
        [ODataRoute("GetPeopleWithFriendsAtLeast(n={n})")]
        public IHttpActionResult GetPeopleWithFriendsAtLeast(int n)
        {
            return Ok(Api.GetPeopleWithFriendsAtLeast(n));
        }

        [HttpPut]
        [ODataRoute("People({key})/LastName")]
        public IHttpActionResult UpdatePersonLastName([FromODataUri]int key, [FromBody]string name)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            entity.LastName = name;

            try
            {
                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                if (!PeopleExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw e;
                }
            }
            return Ok(name);
        }

        [HttpPut]
        [ODataRoute("People({key})/BirthDate")]
        public IHttpActionResult UpdatePersonBirthDate([FromODataUri]int key, [FromBody]string birthDate)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            entity.BirthDate = Date.Parse(birthDate);

            try
            {
                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                if (!PeopleExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw e;
                }
            }
            return Ok(birthDate);
        }

        [HttpPut]
        [ODataRoute("People({key})/BirthDate2")]
        public IHttpActionResult UpdatePersonBirthDate2([FromODataUri]int key, [FromBody]string birthDate)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            entity.BirthDate2 = Date.Parse(birthDate);

            try
            {
                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                if (!PeopleExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw e;
                }
            }
            return Ok(birthDate);
        }

        [HttpPut]
        [ODataRoute("People({key})/BirthTime")]
        public IHttpActionResult UpdatePersonBirthTime([FromODataUri]int key, [FromBody]string birthTime)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            entity.BirthTime = TimeOfDay.Parse(birthTime);

            try
            {
                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                if (!PeopleExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw e;
                }
            }
            return Ok(birthTime);
        }

        [HttpPut]
        [ODataRoute("People({key})/BirthTime2")]
        public IHttpActionResult UpdatePersonBirthTime2([FromODataUri]int key, [FromBody]string birthTime)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            entity.BirthTime2 = TimeOfDay.Parse(birthTime);

            try
            {
                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                if (!PeopleExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw e;
                }
            }
            return Ok(birthTime);
        }

        [HttpPut]
        [ODataRoute("People({key})/BirthDateTime")]
        public IHttpActionResult UpdatePersonBirthDateTime([FromODataUri]int key, [FromBody]string birthDateTime)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            entity.BirthDateTime = DateTimeOffset.Parse(birthDateTime).DateTime;

            try
            {
                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                if (!PeopleExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw e;
                }
            }
            return Ok(birthDateTime);
        }

        [HttpPut]
        [ODataRoute("People({key})/BirthDateTime2")]
        public IHttpActionResult UpdatePersonBirthDateTime2([FromODataUri]int key, [FromBody]string birthDateTime)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            entity.BirthDateTime2 = DateTimeOffset.Parse(birthDateTime).DateTime;

            try
            {
                DbContext.SaveChanges();
            }
            catch (Exception e)
            {
                if (!PeopleExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw e;
                }
            }
            return Ok(birthDateTime);
        }

        [HttpGet]
        [ODataRoute("People({key})/Trips/$ref")]
        public IHttpActionResult GetRefToTripsFromPeople([FromODataUri]int key)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }
            var trips = DbContext.Trips.Where(t => t.PersonId == key);
            var serviceRootUri = GetServiceRootUri();
            IList<Uri> uris = new List<Uri>();
            foreach (var trip in trips)
            {
                uris.Add(new Uri(string.Format("{0}/Trips({1})", serviceRootUri, trip.TripId)));
            }
            return Ok(uris);
        }

        [HttpGet]
        [ODataRoute("People({key})/Trips({key2})/$ref")]
        public IHttpActionResult GetRefToTripsFromPeople([FromODataUri]int key, [FromODataUri]int key2)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }
            var trips = DbContext.Trips.Where(t => t.PersonId == key);
            var serviceRootUri = GetServiceRootUri();

            if (trips.All(t => t.TripId != key2))
            {
                return NotFound();
            }

            return Ok(new Uri(string.Format("{0}/Trips({1})", serviceRootUri, key2)));
        }

        [HttpPost]
        [ODataRoute("People({key})/Trips")]
        public IHttpActionResult PostToTripsFromPeople([FromODataUri]int key, Trip trip)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }
            if (entity.PersonId != key)
            {
                return BadRequest();
            }
            DbContext.Trips.Add(trip);
            DbContext.SaveChanges();
            return Created(trip);
        }

        [HttpPost]
        [ODataRoute("People({key})/Trips/$ref")]
        public IHttpActionResult CreateRefForTripsToPeople([FromODataUri]int key, [FromBody] Uri link)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            var relatedKey = Helpers.GetKeyFromUri<int>(Request, link);
            var trip = DbContext.Trips.SingleOrDefault(t => t.TripId == relatedKey);
            if (trip == null)
            {
                return NotFound();
            }

            trip.PersonId = key;
            DbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete]
        [ODataRoute("People({key})/Trips({relatedKey})/$ref")]
        public IHttpActionResult DeleteRefToTripsFromPeople([FromODataUri]int key, [FromODataUri]int relatedKey)
        {
            var entity = DbContext.People.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            var trip = DbContext.Trips.SingleOrDefault(t => t.TripId == relatedKey && t.PersonId == key);
            if (trip == null)
            {
                return NotFound();
            }

            trip.PersonId = null;
            DbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpGet]
        [ODataRoute("Flights({key})/Airline/$ref")]
        public IHttpActionResult GetRefToAirlineFromFlight([FromODataUri]int key)
        {
            var entity = DbContext.Flights.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            if (entity.AirlineId == null)
            {
                return NotFound();
            }

            var serviceRootUri = GetServiceRootUri();
            var uri = new Uri(string.Format("{0}/Airlines('{1}')", serviceRootUri, entity.AirlineId));
            return Ok(uri);
        }

        [HttpPut]
        [ODataRoute("Flights({key})/Airline/$ref")]
        public IHttpActionResult UpdateRefToAirLineFromFlight([FromODataUri] int key, [FromBody] Uri link)
        {
            var entity = DbContext.Flights.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            var relatedKey = Helpers.GetKeyFromUri<string>(Request, link);
            var aireLine = DbContext.Airlines
                .SingleOrDefault(t => t.AirlineCode.Equals(relatedKey, StringComparison.OrdinalIgnoreCase));
            if (aireLine == null)
            {
                return NotFound();
            }

            entity.Airline = aireLine;
            DbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete]
        [ODataRoute("Flights({key})/Airline/$ref")]
        public IHttpActionResult DeleteRefToAirLineFromFlight([FromODataUri] int key)
        {
            var entity = DbContext.Flights.Find(key);
            if (entity == null)
            {
                return NotFound();
            }

            entity.AirlineId = null;
            DbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Disposes the API and the controller.
        /// </summary>
        /// <param name="disposing">Indicates whether disposing is happening.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.api != null)
                {
                    this.api.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}