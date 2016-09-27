﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
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

        private string GetServiceRootUri()
        {
            var routeName = Request.ODataProperties().RouteName;
            ODataRoute odataRoute = Configuration.Routes[routeName] as ODataRoute;
            var prefixName = odataRoute.RoutePrefix;
            var requestUri = Request.RequestUri.ToString();
            var serviceRootUri = requestUri.Substring(0, requestUri.IndexOf(prefixName, StringComparison.InvariantCultureIgnoreCase) + prefixName.Length);
            return serviceRootUri;
        }
    }
}