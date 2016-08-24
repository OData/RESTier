// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Service.Sample.TrippinInMemory.Api;
using Microsoft.Restier.Core;

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Controllers
{
    public class TrippinController : ODataController
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
    }
}