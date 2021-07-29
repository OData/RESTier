// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;

#if NETCOREAPP3_1_OR_GREATER
using CloudNimble.Breakdance.AspNetCore.OData;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else

using CloudNimble.Breakdance.WebApi;
using CloudNimble.Breakdance.WebApi.OData;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

    [TestClass]
    public class NavigationPropertyTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <LibraryApi>
#endif
    {

        [TestMethod]
        public async Task NavigationProperties_ChildrenShouldFilter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Publishers('Publisher1')/Books", serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            Response.Items.Should().HaveCount(2);
        }

    }

}