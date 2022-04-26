// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData;
using System.Web.Http;

namespace Microsoft.Restier.Tests.AspNet.FallbackTests
{ 

    public class PeopleController : ODataController
    {

        public IHttpActionResult Get()
        { 
            var people = new[]
            {
                new Person { Id = 999 }
            };

            return Ok(people);
        }

        public IHttpActionResult GetOrders(int key)
        {
            var orders = new[]
            {
                new Order { Id = 123 },
            };

            return Ok(orders);
        }

    }

}