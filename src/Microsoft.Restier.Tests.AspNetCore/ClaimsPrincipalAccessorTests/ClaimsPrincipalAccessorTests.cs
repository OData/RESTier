// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        [TestMethod]
        public async Task NetCoreApi_Accessor_IsNotNull()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<ClaimsPrincipalApi>(HttpMethod.Get, resource: "/AccessorIsNotNull()", serviceCollection: services => services.AddTestDefaultServices());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4PrimitiveResult<bool>>();
            Response.Should().NotBeNull();
            Response.Value.Should().BeTrue();
        }

        [TestMethod]
        public async Task NetCoreApi_AccessorClaimsPrincipal_IsNotNull()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<ClaimsPrincipalApi>(HttpMethod.Get, resource: "/AccessorClaimsPrincipalIsNotNull()", serviceCollection: services => services.AddTestDefaultServices());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4PrimitiveResult<bool>>();
            Response.Should().NotBeNull();
            Response.Value.Should().BeTrue();
        }

        [TestMethod]
        public async Task NetCoreApi_ClaimsPrincipalCurrent_IsNotNull()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<ClaimsPrincipalApi>(HttpMethod.Get, resource: "/ClaimsPrincipalCurrentIsNotNull()", serviceCollection: services => services.AddTestDefaultServices());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4PrimitiveResult<bool>>();
            Response.Should().NotBeNull();
            Response.Value.Should().BeTrue();
        }

#endif


    }

}