// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Web.Http;
using System.Web.OData;
using Microsoft.Data.Domain.Samples.Northwind.Models;

namespace Microsoft.Data.Domain.Samples.Northwind.Controllers
{
    public class RegionsController : ODataController
    {
        private readonly NorthwindContext _context = new NorthwindContext();

        [EnableQuery]
        public IQueryable<Region> Get()
        {
            return _context.Regions;
        }

        [EnableQuery]
        public SingleResult<Region> Get(int key)
        {
            return new SingleResult<Region>(_context.Regions.Where(r => r.RegionID == key));
        }
    }
}