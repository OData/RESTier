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

#if NETCOREAPP3_1_OR_GREATER
using CloudNimble.Breakdance.AspNetCore.OData;

using Microsoft.Restier.Tests.AspNetCore.ClaimsPrincipalAccessor;

namespace Microsoft.Restier.Tests.AspNetCore
#else
using CloudNimble.Breakdance.WebApi.OData;

namespace Microsoft.Restier.Tests.AspNet
#endif
{

    [TestClass]
    public class ClaimsPrincipalAccessorTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <ClaimsPrincipalApi>
    {

        public ClaimsPrincipalAccessorTests() : base()
        {
            ApplicationBuilderAction = app =>
            {
                app.UseThreadPrincipals();
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

#endif

    }

}