// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Restier.Core;
using Microsoft.AspNetCore.Builder;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using Microsoft.Restier.Tests.AspNetCore.ClaimsPrincipalAccessor;

namespace Microsoft.Restier.Tests.AspNetCore
{

    [TestClass]
    [TestCategory("Endpoint Routing")]
    public class ClaimsPrincipalAccessorTests_EndpointRouting : ClaimsPrincipalAccessorTests
    {
        public ClaimsPrincipalAccessorTests_EndpointRouting() : base(true)
        {
        }
    }

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class ClaimsPrincipalAccessorTests_LegacyRouting : ClaimsPrincipalAccessorTests
    {
        public ClaimsPrincipalAccessorTests_LegacyRouting() : base(false)
        {
        }
    }

    #region Abstract Test Class (Actual Tests)

    [TestClass]
    public abstract class ClaimsPrincipalAccessorTests : RestierTestBase<ClaimsPrincipalApi>
    {

        public ClaimsPrincipalAccessorTests(bool useEndpointRouting) : base(useEndpointRouting)
        {
            ApplicationBuilderAction = app =>
            {
                app.UseClaimsPrincipals();
            };
            AddRestierAction = builder =>
            {
                builder.AddRestierApi<ClaimsPrincipalApi>(services => services.AddTestDefaultServices());
            };
            MapRestierAction = routeBuilder =>
            {
                routeBuilder.MapApiRoute<ClaimsPrincipalApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix, false);
            };
        }

        [TestInitialize]
        public void ClaimsTestSetup() => TestSetup();

        [TestMethod]
        public async Task NetCoreApi_ClaimsPrincipalCurrent_IsNotNull()
        {
            var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/ClaimsPrincipalCurrentIsNotNull()");
            await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4PrimitiveResult<bool>>();
            Response.Should().NotBeNull();
            Response.Value.Should().BeTrue();
        }

    }

    #endregion

}