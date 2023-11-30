// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else
namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

#if NET6_0_OR_GREATER

    [TestClass]
    [TestCategory("Endpoint Routing")]
    public class InTests_EndpointRouting : InTests
    {
        public InTests_EndpointRouting() : base(true)
        {
        }
    }

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class InTests_LegacyRouting : InTests
    {
        public InTests_LegacyRouting() : base(false)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public abstract class InTests : RestierTestBase<LibraryApi>
    {

        public InTests(bool useEndpointRouting) : base(useEndpointRouting)
        {
            //AddRestierAction = builder =>
            //{
            //    builder.AddRestierApi<LibraryApi>(services => services.AddEntityFrameworkServices<LibraryContext>());
            //};
            //MapRestierAction = routeBuilder =>
            //{
            //    routeBuilder.MapApiRoute<LibraryApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix, false);
            //};
        }

        //[TestInitialize]
        //public void ClaimsTestSetup() => TestSetup();

#else

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class InTests : RestierTestBase
    {

#endif

        [TestMethod]
        public async Task InQueries_IdInList()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books?$filter=Id in ['c2081e58-21a5-4a15-b0bd-fff03ebadd30','0697576b-d616-4057-9d28-ed359775129e']",
                serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(), useEndpointRouting: UseEndpointRouting);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Jungle Book, The");
            content.Should().Contain("Color Purple, The");
            content.Should().NotContain("A Clockwork Orange");
        }

    }

}