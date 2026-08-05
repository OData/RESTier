// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Microsoft.Restier.Tests.AspNetCore.FallbackTests;

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
