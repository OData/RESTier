// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.Breakdance
{

    [TestClass]
    public class RestierBreakdanceTestBase_CoreTests : TestHarnessBase
    {

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void TestSetup_ServerAndServicesAreAvailable()
        {
            var testBase = GetTestBaseInstance<LibraryApi>();
            testBase.TestServer.Should().NotBeNull();
            testBase.TestServer.Services.Should().NotBeNull();
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void TestSetup_ScopeFactoryIsPresent()
        {
            var testBase = GetTestBaseInstance<LibraryApi>();

            var factory = testBase.TestServer.Services.GetRequiredService<IServiceScopeFactory>();
            factory.Should().NotBeNull();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task HttpClient_ShouldReturnRootContent()
        {
            var testBase = GetTestBaseInstance<LibraryApi>();

            var client = testBase.GetHttpClient();
            var result = await client.GetAsync("");
            var resultContent = await result.Content.ReadAsStringAsync();

            resultContent.Should().ContainAll("$metadata", "Books", "LibraryCards", "Publishers", "Readers");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetApiMetadataAsync_ReturnsXDocument()
        {
            var testBase = GetTestBaseInstance<LibraryApi>();

            var metadata = await testBase.GetApiMetadataAsync();
            metadata.Should().NotBeNull();
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void GetScopedRequestContainer_ReturnsInstance()
        {
            var testBase = GetTestBaseInstance<LibraryApi>();

            var container = testBase.GetScopedRequestContainer();
            container.Should().NotBeNull();
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void GetApiInstance_ReturnsInstanceFromRequestScope()
        {
            var testBase = GetTestBaseInstance<LibraryApi>();

            var api = testBase.GetApiInstance();
            api.Should().NotBeNull();
        }

    }

}

#endif