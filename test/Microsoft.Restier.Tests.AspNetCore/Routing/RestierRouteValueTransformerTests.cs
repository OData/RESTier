// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Routing;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Routing
{
    /// <summary>
    /// Unit tests for <see cref="RestierRouteValueTransformer"/>.
    /// </summary>
    public class RestierRouteValueTransformerTests
    {
        #region Helper Methods

        /// <summary>
        /// Builds a simple test EDM model with Customers and Orders entity sets,
        /// a bound action on the Orders collection, and a bound function on the Customers collection.
        /// </summary>
        private static IEdmModel BuildTestModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<TestCustomer>("Customers");
            builder.EntitySet<TestOrder>("Orders");

            // Bound action on Orders collection
            builder.EntityType<TestOrder>().Collection.Action("Discontinue");

            // Bound function on Customers collection returning collection
            builder.EntityType<TestCustomer>().Collection
                .Function("TopCustomers")
                .ReturnsCollectionFromEntitySet<TestCustomer>("Customers");

            // Unbound action (action import)
            builder.Action("ResetDatabase");

            return builder.GetEdmModel();
        }

        /// <summary>
        /// Creates a transformer with the test model registered under the given prefix,
        /// with <see cref="RestierRouteMarker"/> registered in per-route services.
        /// Sets <c>transformer.State = routePrefix</c> to simulate what MapRestier does.
        /// </summary>
        private static (RestierRouteValueTransformer transformer, ODataOptions options) CreateTransformer(
            string routePrefix = "")
        {
            var model = BuildTestModel();

            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton<IOptions<ODataOptions>>(sp =>
            {
                var odataOptions = new ODataOptions();
                odataOptions.AddRouteComponents(routePrefix, model, routeServices =>
                {
                    routeServices.AddSingleton(new RestierRouteMarker(typeof(object)));
                });
                return Options.Create(odataOptions);
            });

            var serviceProvider = services.BuildServiceProvider();
            var odataOptionsInstance = serviceProvider.GetRequiredService<IOptions<ODataOptions>>();

            var transformer = new RestierRouteValueTransformer(odataOptionsInstance)
            {
                State = routePrefix
            };

            return (transformer, odataOptionsInstance.Value);
        }

        /// <summary>
        /// Creates a <see cref="DefaultHttpContext"/> with the specified HTTP method and path.
        /// </summary>
        private static HttpContext CreateHttpContext(string method, string path)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("localhost");
            context.Request.Path = new PathString("/" + path.TrimStart('/'));
            return context;
        }

        #endregion

        #region Test Cases

        [Fact]
        public async Task Get_EntitySet_ReturnsGetActionWithEntitySetSegment()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers" };
            var httpContext = CreateHttpContext("GET", "/Customers");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("Get");

            var feature = httpContext.ODataFeature();
            feature.Path.Should().NotBeNull();
            feature.Path.Should().ContainItemsAssignableTo<EntitySetSegment>();
        }

        [Fact]
        public async Task Get_EntityWithKey_ReturnsGetActionWithTwoSegments()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers(1)" };
            var httpContext = CreateHttpContext("GET", "/Customers(1)");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("Get");

            var feature = httpContext.ODataFeature();
            feature.Path.Should().NotBeNull();
            feature.Path.Should().HaveCount(2);
        }

        [Fact]
        public async Task Post_EntitySet_ReturnsPostAction()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers" };
            var httpContext = CreateHttpContext("POST", "/Customers");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("Post");
        }

        [Fact]
        public async Task Post_BoundAction_ReturnsPostActionAction()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Orders/Discontinue" };
            var httpContext = CreateHttpContext("POST", "/Orders/Discontinue");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("PostAction");
        }

        [Fact]
        public async Task Put_EntityWithKey_ReturnsPutAction()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers(1)" };
            var httpContext = CreateHttpContext("PUT", "/Customers(1)");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("Put");
        }

        [Fact]
        public async Task Patch_EntityWithKey_ReturnsPatchAction()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers(1)" };
            var httpContext = CreateHttpContext("PATCH", "/Customers(1)");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("Patch");
        }

        [Fact]
        public async Task Delete_EntityWithKey_ReturnsDeleteAction()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers(1)" };
            var httpContext = CreateHttpContext("DELETE", "/Customers(1)");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("Delete");
        }

        [Fact]
        public async Task Get_InvalidPath_ReturnsNull()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "NonExistentEntitySet" };
            var httpContext = CreateHttpContext("GET", "/NonExistentEntitySet");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Get_EmptyPath_ReturnsGetServiceDocumentAction()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "" };
            var httpContext = CreateHttpContext("GET", "/");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("GetServiceDocument");

            var feature = httpContext.ODataFeature();
            feature.Path.Should().NotBeNull();
            feature.Path.Should().HaveCount(0);
        }

        [Fact]
        public async Task Get_MetadataPath_ReturnsGetMetadataAction()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "$metadata" };
            var httpContext = CreateHttpContext("GET", "/$metadata");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("GetMetadata");

            var feature = httpContext.ODataFeature();
            feature.Path.Should().NotBeNull();
            feature.Path.LastOrDefault().Should().BeOfType<MetadataSegment>();
        }

        [Fact]
        public async Task ODataFeature_IsCorrectlyPopulated()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers" };
            var httpContext = CreateHttpContext("GET", "/Customers");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();

            var feature = httpContext.ODataFeature();
            feature.Path.Should().NotBeNull();
            feature.Model.Should().NotBeNull();
            feature.RoutePrefix.Should().Be(string.Empty);
            feature.BaseAddress.Should().NotBeNullOrEmpty();
            feature.BaseAddress.Should().EndWith("/");
        }

        [Fact]
        public async Task RoutePrefix_PopulatesCorrectBaseAddress()
        {
            // Arrange
            var (transformer, _) = CreateTransformer("api/v1");
            var values = new RouteValueDictionary { ["odataPath"] = "Customers" };
            var httpContext = CreateHttpContext("GET", "/api/v1/Customers");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();

            var feature = httpContext.ODataFeature();
            feature.RoutePrefix.Should().Be("api/v1");
            feature.BaseAddress.Should().Contain("api/v1");
            feature.BaseAddress.Should().EndWith("/");
        }

        [Fact]
        public async Task NonRestierRoute_IsIgnored()
        {
            // Arrange
            var model = BuildTestModel();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddSingleton<IOptions<ODataOptions>>(sp =>
            {
                var odataOptions = new ODataOptions();
                // Register without RestierRouteMarker
                odataOptions.AddRouteComponents("other", model);
                return Options.Create(odataOptions);
            });

            var serviceProvider = services.BuildServiceProvider();
            var odataOptionsInstance = serviceProvider.GetRequiredService<IOptions<ODataOptions>>();

            var transformer = new RestierRouteValueTransformer(odataOptionsInstance)
            {
                State = "other"
            };

            var values = new RouteValueDictionary { ["odataPath"] = "Customers" };
            var httpContext = CreateHttpContext("GET", "/other/Customers");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Get_BoundFunction_ReturnsGetAction()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers/TopCustomers()" };
            var httpContext = CreateHttpContext("GET", "/Customers/TopCustomers()");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("Get");
        }

        [Fact]
        public async Task Post_ActionImport_ReturnsPostActionAction()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "ResetDatabase" };
            var httpContext = CreateHttpContext("POST", "/ResetDatabase");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("PostAction");
        }

        [Fact]
        public async Task Options_UnsupportedMethod_ReturnsNull()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers" };
            var httpContext = CreateHttpContext("OPTIONS", "/Customers");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task NullHttpContext_ReturnsNull()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers" };

            // Act
            var result = await transformer.TransformAsync(null, values);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        public async Task NonGet_Metadata_ReturnsNull(string method)
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "$metadata" };
            var httpContext = CreateHttpContext(method, "/$metadata");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        public async Task NonGet_ServiceDocument_ReturnsNull(string method)
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "" };
            var httpContext = CreateHttpContext(method, "/");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task PathBase_IncludedInBaseAddress()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers" };
            var httpContext = CreateHttpContext("GET", "/Customers");
            httpContext.Request.PathBase = new PathString("/myapp");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            var feature = httpContext.ODataFeature();
            feature.BaseAddress.Should().Be("https://localhost/myapp/");
        }

        [Fact]
        public async Task PathBase_WithRoutePrefix_IncludedInBaseAddress()
        {
            // Arrange
            var (transformer, _) = CreateTransformer("api/v1");
            var values = new RouteValueDictionary { ["odataPath"] = "Customers" };
            var httpContext = CreateHttpContext("GET", "/api/v1/Customers");
            httpContext.Request.PathBase = new PathString("/myapp");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            var feature = httpContext.ODataFeature();
            feature.BaseAddress.Should().Be("https://localhost/myapp/api/v1/");
        }

        [Fact]
        public async Task PathBase_RootSlash_DoesNotProduceDoubleSlash()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers" };
            var httpContext = CreateHttpContext("GET", "/Customers");
            httpContext.Request.PathBase = new PathString("/");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            var feature = httpContext.ODataFeature();
            feature.BaseAddress.Should().Be("https://localhost/");
        }

        [Fact]
        public async Task Get_NavigationProperty_ReturnsGetAction()
        {
            // Arrange
            var (transformer, _) = CreateTransformer();
            var values = new RouteValueDictionary { ["odataPath"] = "Customers(1)/Orders" };
            var httpContext = CreateHttpContext("GET", "/Customers(1)/Orders");

            // Act
            var result = await transformer.TransformAsync(httpContext, values);

            // Assert
            result.Should().NotBeNull();
            result["controller"].Should().Be("Restier");
            result["action"].Should().Be("Get");

            var feature = httpContext.ODataFeature();
            feature.Path.Should().HaveCount(3); // EntitySet + Key + NavigationProperty
        }

        #endregion

        #region Entity Classes

        /// <summary>Entity class for use with ODataConventionModelBuilder.</summary>
        public class TestCustomer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public System.Collections.Generic.List<TestOrder> Orders { get; set; }
        }

        /// <summary>Entity class for use with ODataConventionModelBuilder.</summary>
        public class TestOrder
        {
            public int Id { get; set; }
            public string Product { get; set; }
        }

        #endregion
    }
}
