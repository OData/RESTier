// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core
{

    /// <summary>
    /// Tests methods of the Core ServiceCOllectionExtensions.
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionTests
#if NET6_0_OR_GREATER
        : RestierTestBase<TestableEmptyApi>
#else
        : RestierTestBase
#endif
    {

        [TestMethod]
        public void HasService_ReturnsTrueCorrectly()
        {
            var services = new ServiceCollection();
            services.AddTestDefaultServices();
            services.Should().HaveCount(4);
            services.HasService<IChangeSetInitializer>().Should().Be(true);
        }

        [TestMethod]
        public void HasService_ReturnsFalseCorrectly()
        {
            var services = new ServiceCollection();
            services.AddTestDefaultServices();
            services.Should().HaveCount(4);
            services.HasService<StoreModelMapper>().Should().Be(false);
        }

        [TestMethod]
        public void HasServiceCount_Returns0Correctly()
        {
            var services = new ServiceCollection();
            services.AddTestDefaultServices();
            services.Should().HaveCount(4);
            services.HasServiceCount<StoreModelMapper>().Should().Be(0);
        }

        [TestMethod]
        public void HasServiceCount_Returns1Correctly()
        {
            var services = new ServiceCollection();
            services.AddTestDefaultServices();
            services.Should().HaveCount(4);
            services.HasServiceCount<IChangeSetInitializer>().Should().Be(1);
        }

        [TestMethod]
        public void HasServiceCount_Returns2Correctly()
        {
            var services = new ServiceCollection();
            services.AddTestDefaultServices();
            services.AddSingleton<ISubmitExecutor, DefaultSubmitExecutor>();
            services.Should().HaveCount(5);
            services.HasServiceCount<ISubmitExecutor>().Should().Be(2);
        }

    }

}