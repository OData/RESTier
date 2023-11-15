// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;

#if NETCOREAPP3_1_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests
#else
namespace Microsoft.Restier.Tests.AspNet.RegressionTests
#endif
{

    /// <summary>
    /// Regression tests for https://github.com/OData/RESTier/issues/541.
    /// </summary>
    [TestClass]
    public class Issue541_CountPlusParametersFails : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <LibraryApi>
#endif
    {

        [TestMethod]
        public async Task CountShouldntThrowExceptions()
        {
            //var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            //var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$count=true");
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Readers?$count=true", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        [TestMethod]
        public async Task CountPlusTopShouldntThrowExceptions()
        {
            //var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            //var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true");
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Readers?$top=5&$count=true", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        [TestMethod]
        public async Task CountPlusTopPlusFilterShouldntThrowExceptions()
        {
            //var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            //var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true&$filter=FullName eq 'p1'");
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Readers?$top=5&$count=true&$filter=FullName eq 'p1'", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":1,");
        }

        [TestMethod]
        public async Task CountPlusTopPlusProjectionShouldntThrowExceptions()
        {
            //var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            //var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true&$select=Id,FullName");
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Readers?$top=5&$count=true&$select=Id,FullName", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        [TestMethod]
        public async Task CountPlusSelectShouldntThrowExceptions()
        {
            //var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            //var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$count=true&$select=Id,FullName");
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Readers?$count=true&$select=Id,FullName", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        [TestMethod]
        public async Task CountPlusExpandShouldntThrowExceptions()
        {
            //var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            //var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Publishers?$top=5&$count=true&$expand=Books");
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Publishers?$top=5&$count=true&$expand=Books", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

    }

}