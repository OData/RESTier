// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Routing;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.WebApi.Routing;
using Xunit;

namespace Microsoft.Restier.WebApi.Test
{
    public class DefaultODataPathRouteConstraintTest
    {
        [Theory]
        [InlineData("Customers", "Customers")]
        [InlineData("NorthwindDomain", "Products")]
        public void Match_UsesODataDefaultRoutingConventions_IfControllerFound(string expectedControllerName,
            string entitySetName)
        {
            // Arrange
            const string routeName = "api";
            var request = new HttpRequestMessage(HttpMethod.Get, "http://any/" + entitySetName);
            var routeCollection = new HttpRouteCollection { { routeName, new HttpRoute() } };
            var config = new HttpConfiguration(routeCollection);
            request.SetConfiguration(config);
            var pathHandler = new DefaultODataPathHandler();
            var model = GetEdmModel();
            config.MapHttpAttributeRoutes();
            var conventions = config.CreateODataDomainRoutingConventions<NorthwindDomainController>(model);
            var constraint = new DefaultODataPathRouteConstraint(pathHandler, model, routeName, conventions);
            config.EnsureInitialized();
            var values = new Dictionary<string, object>
            {
                { ODataRouteConstants.ODataPath, entitySetName },
            };

            // Act
            var matched = constraint.Match(request, route: null, parameterName: null, values: values,
                routeDirection: HttpRouteDirection.UriResolution);

            // Assert
            Assert.True(matched);
            Assert.Equal(expectedControllerName, values[ODataRouteConstants.Controller]);
        }

        private static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Customer>("Products");
            var model = builder.GetEdmModel();
            return model;
        }

        public class CustomersController : ODataController
        {
            public IQueryable<Customer> Get()
            {
                return Enumerable.Empty<Customer>().AsQueryable();
            }
        }

        public class NorthwindDomainController : ODataDomainController
        {
            protected override IDomain CreateDomain()
            {
                return default(IDomain);
            }

            public HttpResponseMessage Get()
            {
                return default(HttpResponseMessage);
            }
        }

        public class Customer
        {
            public int Id { get; set; }
        }

        public class Product
        {
            public int Id { get; set; }
        }
    }
}
