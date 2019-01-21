// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.EntityFramework.Tests
{

    [TestClass]
    public class ChangeSetPreparerTests : RestierTestBase
    {

        [TestMethod]
        public async Task ComplexTypeUpdate()
        {
            // Arrange
            var provider = await RestierTestHelpers.GetTestableInjectionContainer<LibraryApi>();
            var api = provider.GetTestableApiInstance<LibraryApi>();

            var item = new DataModificationItem(
                "Readers",
                typeof(Employee),
                null,
                RestierEntitySetOperation.Update, 
                new Dictionary<string, object> { { "Id", new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461") } },
                new Dictionary<string, object>(),
                new Dictionary<string, object> { { "Addr", new Dictionary<string, object> { { "Zip", "332" } } } });
            var changeSet = new ChangeSet(new[] { item });
            var sc = new SubmitContext(provider, changeSet);

            // Act
            var changeSetPreparer = api.GetApiService<IChangeSetInitializer>();
            await changeSetPreparer.InitializeAsync(sc, CancellationToken.None).ConfigureAwait(false);
            var person = item.Resource as Employee;

            // Assert
            person.Should().NotBeNull();
            person.Addr.Zip.Should().Be("332");
        }
    }
}
