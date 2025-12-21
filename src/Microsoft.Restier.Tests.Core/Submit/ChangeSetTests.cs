// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core.Submit
{

    /// <summary>
    /// Unit tests for the <see cref="ChangeSet"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ChangeSetTests
    {
        private ChangeSet testClass;
        private IEnumerable<ChangeSetItem> entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetTests"/> class.
        /// </summary>
        public ChangeSetTests()
        {
            entries = new[]
            {
                new DataModificationItem<string>(
                    "Tests",
                    typeof(Test),
                    typeof(Test),
                    RestierEntitySetOperation.Insert,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object),
                new DataModificationItem<string>(
                    "People",
                    typeof(Person),
                    typeof(Person),
                    RestierEntitySetOperation.Filter,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object),
                new DataModificationItem<string>(
                    "Orders",
                    typeof(Order),
                    typeof(Order),
                    RestierEntitySetOperation.Update,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object,
                    new Mock<IReadOnlyDictionary<string, object>>().Object),
            };
            testClass = new ChangeSet(entries);
        }

        /// <summary>
        /// Can construct.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new ChangeSet(entries);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot constructo with null entries.
        /// </summary>
        [TestMethod]
        public void CanConstructWithNullEntries()
        {
            var instance = new ChangeSet();
            instance.Should().NotBeNull();
            instance.Entries.Should().NotBeNull();
        }

        /// <summary>
        /// Entries is initialized correctly.
        /// </summary>
        [TestMethod]
        public void EntriesIsInitializedCorrectly()
        {
            testClass.Entries.Should().BeEquivalentTo(entries);
        }

        private class Test
        {
        }

        private class Person
        {
        }

        private class Order
        {
        }
    }
}