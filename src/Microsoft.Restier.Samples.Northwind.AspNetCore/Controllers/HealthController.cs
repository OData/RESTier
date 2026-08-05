// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Restier.Samples.Northwind.AspNetCore.Controllers
{

    /// <summary>
    /// Plain ASP.NET Core controller used to demonstrate combining Restier with regular MVC
    /// endpoints in the same OpenAPI surface. This controller appears in the "controllers"
    /// OpenAPI document, separate from the Restier-derived Northwind document.
    /// </summary>
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {

        [HttpGet("live")]
        public IActionResult Live() => Ok(new { status = "ok" });

        [HttpGet("version")]
        public IActionResult Version() => Ok(new { version = typeof(HealthController).Assembly.GetName().Version?.ToString() });

    }

}
