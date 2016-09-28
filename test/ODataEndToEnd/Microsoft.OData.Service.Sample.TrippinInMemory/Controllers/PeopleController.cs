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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Service.Sample.TrippinInMemory.Api;
using Microsoft.OData.Service.Sample.TrippinInMemory.Models;
using Microsoft.Restier.Core;

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Controllers
{
    public class PeopleController : ODataController
    {
        private TrippinApi _api;
        private TrippinApi Api
        {
            get
            {
                if (_api == null)
                {
                    _api = (TrippinApi)this.Request.GetRequestContainer().GetService<ApiBase>();
                }

                return _api;
            }
        }

        /// <summary>
        /// Restier only supports put and post entity set.
        /// Use property name to simulate the bound action.
        /// </summary>
        /// <param name="key">Key of people entity set, parsed from uri.</param>
        /// <param name="name">The value of last name to be updated.</param>
        /// <returns><see cref="IHttpActionResult"></returns>
        [HttpPut]
        [ODataRoute("People({key})/LastName")]
        public IHttpActionResult UpdatePersonLastName([FromODataUri]string key, [FromBody] string name)
        {
            var person = Api.People.Single(p => p.UserName == key);
            if (Api.UpdatePersonLastName(person, name))
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        [ODataRoute("People({key})/Friends/$ref")]
        public IHttpActionResult GetRefToFriendsFromPeople([FromODataUri]string key)
        {
            var entity = Api.People.FirstOrDefault(p => p.UserName == key);
            if (entity == null)
            {
                return NotFound();
            }

            var friends = entity.Friends;
            if (friends == null)
            {
                return NotFound();
            }

            var serviceRootUri = Helpers.GetServiceRootUri(Request);
            IList<Uri> uris = new List<Uri>();
            foreach (var friend in friends)
            {
                uris.Add(new Uri(string.Format("{0}/People('{1}')", serviceRootUri, friend.UserName)));
            }

            return Ok(uris);
        }

        [HttpGet]
        [ODataRoute("People({key})/Friends({key2})/$ref")]
        public IHttpActionResult GetRefToOneFriendFromPeople([FromODataUri]string key, [FromODataUri]string key2)
        {
            var entity = Api.People.FirstOrDefault(p => p.UserName == key);
            if (entity == null)
            {
                return NotFound();
            }

            var friends = entity.Friends;
            if (friends == null)
            {
                return NotFound();
            }

            var serviceRootUri = Helpers.GetServiceRootUri(Request);
            if (friends.All(t => t.UserName != key2))
            {
                return NotFound();
            }

            return Ok(new Uri(string.Format("{0}/People('{1}')", serviceRootUri, key2)));
        }

        [HttpPost]
        [ODataRoute("People({key})/Friends/$ref")]
        public IHttpActionResult CreateRefForFriendsToPeople([FromODataUri]string key, [FromBody] Uri link)
        {
            var entity = Api.People.FirstOrDefault(p => p.UserName == key);
            if (entity == null)
            {
                return NotFound();
            }

            var relatedKey = Helpers.GetKeyFromUri<string>(Request, link);
            var friend = Api.People.SingleOrDefault(t => t.UserName == relatedKey);
            if (friend == null)
            {
                return NotFound();
            }

            entity.Friends.Add(friend);
            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete]
        [ODataRoute("People({key})/Friends({relatedKey})/$ref")]
        public IHttpActionResult DeleteRefToFriendsFromPeople([FromODataUri]string key, [FromODataUri]string relatedKey)
        {
            var entity = Api.People.FirstOrDefault(p => p.UserName == key);
            if (entity == null)
            {
                return NotFound();
            }

            var friend = entity.Friends.SingleOrDefault(t => t.UserName == relatedKey);
            if (friend == null)
            {
                return NotFound();
            }

            entity.Friends.Remove(friend);
            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpGet]
        [ODataRoute("People({key})/BestFriend/$ref")]
        public IHttpActionResult GetRefToBestFriendFromPeople([FromODataUri]string key)
        {
            var entity = Api.People.FirstOrDefault(p => p.UserName == key);
            if (entity == null)
            {
                return NotFound();
            }

            var friend = entity.BestFriend;
            if (friend == null)
            {
                return NotFound();
            }

            var serviceRootUri = Helpers.GetServiceRootUri(Request);
            var uri = new Uri(string.Format("{0}/People('{1}')", serviceRootUri, friend.UserName));
            return Ok(uri);
        }

        [HttpPut]
        [ODataRoute("People({key})/BestFriend/$ref")]
        public IHttpActionResult CreateRefForBestFriendToPeople([FromODataUri]string key, [FromBody] Uri link)
        {
            var entity = Api.People.FirstOrDefault(p => p.UserName == key);
            if (entity == null)
            {
                return NotFound();
            }

            var relatedKey = Helpers.GetKeyFromUri<string>(Request, link);
            var friend = Api.People.SingleOrDefault(t => t.UserName == relatedKey);
            if (friend == null)
            {
                return NotFound();
            }

            entity.BestFriend = friend;
            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete]
        [ODataRoute("People({key})/BestFriend/$ref")]
        public IHttpActionResult DeleteRefToBestFriendFromPeople([FromODataUri]string key)
        {
            var entity = Api.People.FirstOrDefault(p => p.UserName == key);
            if (entity == null)
            {
                return NotFound();
            }

            entity.BestFriend = null;
            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}