// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning
{

    [TestClass]
    public class RestierApiVersionRegistryTests
    {

        [TestMethod]
        public void Add_AppendsDescriptorWithEverySpecifiedField()
        {
            var registry = new RestierApiVersionRegistry();

            var descriptor = registry.Add(
                new ApiVersion(1, 0),
                basePrefix: "api",
                routePrefix: "api/v1",
                apiType: typeof(SampleApi),
                isDeprecated: true,
                groupName: "v1",
                sunsetDate: new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

            descriptor.Version.Should().Be("1.0");
            descriptor.BasePrefix.Should().Be("api");
            descriptor.RoutePrefix.Should().Be("api/v1");
            descriptor.ApiType.Should().Be(typeof(SampleApi));
            descriptor.IsDeprecated.Should().BeTrue();
            descriptor.GroupName.Should().Be("v1");
            descriptor.SunsetDate.Should().Be(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

            registry.Descriptors.Should().HaveCount(1);
            registry.Descriptors[0].Should().BeSameAs(descriptor);
        }

        [TestMethod]
        public void FindByPrefix_IsCaseSensitive()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            registry.FindByPrefix("api/v1").Should().NotBeNull();
            registry.FindByPrefix("API/V1").Should().BeNull();
            registry.FindByPrefix("api/v2").Should().BeNull();
        }

        [TestMethod]
        public void FindByGroupName_IsCaseInsensitive()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            registry.FindByGroupName("v1").Should().NotBeNull();
            registry.FindByGroupName("V1").Should().NotBeNull();
            registry.FindByGroupName("v2").Should().BeNull();
        }

        [TestMethod]
        public void FindByBasePrefix_ReturnsAllDescriptorsInGroup()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "orders", "orders/v1", typeof(OrdersApiV1), true, "orders-v1", null);
            registry.Add(new ApiVersion(2, 0), "orders", "orders/v2", typeof(OrdersApiV2), false, "orders-v2", null);
            registry.Add(new ApiVersion(1, 0), "inventory", "inventory/v1", typeof(InventoryApi), false, "inventory-v1", null);

            var ordersGroup = registry.FindByBasePrefix("orders");

            ordersGroup.Should().HaveCount(2);
            ordersGroup.Should().OnlyContain(d => d.BasePrefix == "orders");
        }

        [TestMethod]
        public void FindByBasePrefix_ReturnsEmptyListForUnknownGroup()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            registry.FindByBasePrefix("nonexistent").Should().BeEmpty();
        }

        private class SampleApi { }

        private class OrdersApiV1 { }

        private class OrdersApiV2 { }

        private class InventoryApi { }

    }

}
