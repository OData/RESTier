// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core.Submit
{
    /// <summary>
    /// Unit tests for the <see cref="DataModificationItem{T}"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class DataModificationItemOfTTests
    {
        private DataModificationItem<Test> testClass;
        private string resourceSetName;
        private Type expectedResourceType;
        private Type actualResourceType;
        private RestierEntitySetOperation action;
        private Dictionary<string, object> resourceKey;
        private Dictionary<string, object> originalValues;
        private Dictionary<string, object> localValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationItemOfTTests"/> class.
        /// </summary>
        public DataModificationItemOfTTests()
        {
            resourceSetName = "Tests";
            expectedResourceType = typeof(Test);
            actualResourceType = typeof(Test);
            action = RestierEntitySetOperation.Update;
            resourceKey = new Dictionary<string, object>();
            originalValues = new Dictionary<string, object>();
            localValues = new Dictionary<string, object>();
            testClass = new DataModificationItem<Test>(
                resourceSetName,
                expectedResourceType,
                actualResourceType,
                action,
                resourceKey,
                originalValues,
                localValues);
        }

        /// <summary>
        /// Can construct the <see cref="DataModificationItem"/> instance.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new DataModificationItem(
                resourceSetName,
                expectedResourceType,
                actualResourceType,
                action,
                resourceKey,
                originalValues,
                localValues);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with null expected resource type.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullExpectedResourceType()
        {
            Action act = () => new DataModificationItem(
                resourceSetName,
                default(Type),
                typeof(Test),
                action,
                resourceKey,
                originalValues,
                localValues);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can set and get Resource.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetResource()
        {
            var testValue = new Test { Name = "LoremIpsum", Order = 1 };
            testClass.Resource = testValue;
            testClass.Resource.Should().Be(testValue);
        }

        private class Test
        {
            public string Name { get; set; }

            public int Order { get; set; }
        }
    }
}