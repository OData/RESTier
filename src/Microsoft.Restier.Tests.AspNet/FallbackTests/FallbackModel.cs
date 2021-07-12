// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Builder;
using Microsoft.OData.Edm;
using System.Collections.Generic;

#if NETCORE3 || NETCOREAPP3_1_OR_GREATER

namespace Microsoft.Restier.Tests.AspNetCore.FallbackTests

#else
using CloudNimble.Breakdance.WebApi;
using Microsoft.Restier.AspNet.Model;
using System.Web.Http;

namespace Microsoft.Restier.Tests.AspNet.FallbackTests
#endif

{

    public static class FallbackModel
    {
        public static EdmModel Model { get; private set; }

        static FallbackModel()
        {
            var builder = new ODataConventionModelBuilder
            {
                Namespace = "Microsoft.Restier.Tests.AspNet"
            };
            builder.EntitySet<Order>("Orders");
            builder.EntitySet<Person>("People");
            Model = (EdmModel)builder.GetEdmModel();
        }
    }

    internal class Person
    {
        public int Id { get; set; }

        public IEnumerable<Order> Orders { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
    }

}