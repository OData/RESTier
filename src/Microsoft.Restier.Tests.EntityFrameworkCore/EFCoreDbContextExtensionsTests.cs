// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.IncorrectLibrary;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFrameworkCore
{

    [TestClass]
    public class EFCoreDbContextExtensionsTests
    {

        /// <summary>
        /// Tests that the IsDbSetMapped extension works as expected
        /// </summary>
        [TestMethod]
        public void IsDbSetMapped_CanFind_MappedDbSets()
        {
            using var context = new LibraryContext(new DbContextOptions<LibraryContext> { });
            context.Should().NotBeNull();

            context.IsDbSetMapped(typeof(Address)).Should().BeFalse();

            using var incorrectContext = new IncorrectLibraryContext(new DbContextOptions<IncorrectLibraryContext>());
            incorrectContext.Should().NotBeNull();

            incorrectContext.IsDbSetMapped(typeof(Address)).Should().BeTrue();
        }

    }

}
