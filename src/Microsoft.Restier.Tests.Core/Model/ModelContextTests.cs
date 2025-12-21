// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core.Model
{
    /// <summary>
    /// Unit tests for the <see cref="ModelContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ModelContextTests
    {
        private ModelContext testClass;
        private ApiBase api;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelContextTests"/> class.
        /// </summary>
         public ModelContextTests()
        {
            var serviceProvider = new ServiceProviderMock().ServiceProvider.Object;
            api = new TestApi(serviceProvider);
            testClass = new ModelContext(api);
        }

        /// <summary>
        /// Tests that a model context can be constructed.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new ModelContext(api);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that a model context cannot be constructed without an ApiBase.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new ModelContext(default(ApiBase));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Tests that the ResourceMap can be retrieved.
        /// </summary>
        [TestMethod]
        public void CanGetResourceSetTypeMap()
        {
            testClass.ResourceSetTypeMap.Should().BeAssignableTo<IDictionary<string, Type>>();
        }

        /// <summary>
        /// Tests that the ResourceTypeKeyPropertiesMap can be retreived.
        /// </summary>
        [TestMethod]
        public void CanGetResourceTypeKeyPropertiesMap()
        {
            testClass.ResourceTypeKeyPropertiesMap.Should().BeAssignableTo<IDictionary<Type, ICollection<PropertyInfo>>>();
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