using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Restier.AspNetCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.EndpointRouting
{

    [TestClass]
    public class Restier_IEndpointRouteBuilderExtensionsTests
    {

        [TestMethod]
        public void GetCleanRouteName_RemovesSlashes()
        {
            var name = Restier_IEndpointRouteBuilderExtensions.GetCleanRouteName(WebApiConstants.RouteName);
            name.Should().NotBeNullOrWhiteSpace();
            name.Should().NotContainAny("/", "{", "}");
        }

        [TestMethod]
        public void FormatRoutingPattern_WithCleanedName_Succeeds()
        {
            var name = Restier_IEndpointRouteBuilderExtensions.GetCleanRouteName(WebApiConstants.RouteName);
            name.Should().NotBeNullOrWhiteSpace();
            name.Should().NotContainAny("/", "{", "}");

            var routingPattern = Restier_IEndpointRouteBuilderExtensions.FormatRoutingPattern(name, WebApiConstants.RoutePrefix);
            routingPattern.Should().NotBeNullOrWhiteSpace();
            routingPattern[routingPattern.IndexOf("**")..^1].Should().NotContainAny("/", "{", "}");
        }

        [TestMethod]
        public void FormatRoutingPattern_WithoutCleaningName_Fails()
        {
            var routingPattern = Restier_IEndpointRouteBuilderExtensions.FormatRoutingPattern(WebApiConstants.RouteName, WebApiConstants.RoutePrefix);
            routingPattern.Should().NotBeNullOrWhiteSpace();
            routingPattern[routingPattern.IndexOf("**")..^1].Should().ContainAny("/", "{", "}");
        }

    }

}
