// <copyright file="InvocationContextTests.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// Unit tests for the <see cref="InvocationContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class InvocationContextTests
    {
        private InvocationContext testClass;
        private ApiBase api;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationContextTests"/> class.
        /// </summary>
       public InvocationContextTests()
        {
            var serviceProvider = new ServiceProviderMock();
            api = new TestApi(serviceProvider.ServiceProvider.Object);
            testClass = new InvocationContext(api);
        }

        /// <summary>
        /// Can construct an InvocationContext.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new InvocationContext(api);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct an InvocationContext with a null api.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new InvocationContext(default(ApiBase));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call GetApiService().
        /// </summary>
        [TestMethod]
        public void CanCallGetApiService()
        {
            var result = testClass.GetApiService<IQueryExecutor>();
            result.Should().NotBeNull();
        }

        /// <summary>
        /// Api is initialized correctly.
        /// </summary>
        [TestMethod]
        public void ApiIsInitializedCorrectly()
        {
            testClass.Api.Should().Be(api);
        }

        private class TestApi : ApiBase
        {
            public TestApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }
    }
}