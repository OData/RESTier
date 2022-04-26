// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Restier.Tests.AspNetCore.FallbackTests
{

    public class PeopleController : ODataController
    {

        [EnableQuery]
        public IActionResult Get()
        {
            var people = new[]
            {
                new Person { Id = 999 }
            };

            return Ok(people);
        }

        [EnableQuery]
        public IActionResult GetOrders(int key)
        {
            var orders = new[]
            {
                new Order { Id = 123 },
            };

            return Ok(orders);
        }

    }

}