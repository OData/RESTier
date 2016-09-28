// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Providers.EntityFramework.Tests.Models.Library;
using Xunit;

namespace Microsoft.Restier.Providers.EntityFramework.Tests
{
    public class ChangeSetPreparerTests
    {
        [Fact]
        public async Task ComplexTypeUpdate()
        {
            // Arrange
            var container = new RestierContainerBuilder(typeof(LibraryApi));
            var provider = container.BuildContainer();
            var libraryApi = provider.GetService<ApiBase>();

            var item = new DataModificationItem(
                "Readers",
                typeof(Person),
                null,
                DataModificationItemAction.Update, 
                new Dictionary<string, object> { { "Id", new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461") } },
                new Dictionary<string, object>(),
                new Dictionary<string, object> { { "Addr", new Dictionary<string, object> { { "Zip", "332" } } } });
            var changeSet = new ChangeSet(new[] { item });
            var sc = new SubmitContext(provider, changeSet);

            // Act
            var changeSetPreparer = libraryApi.GetApiService<IChangeSetInitializer>();
            await changeSetPreparer.InitializeAsync(sc, CancellationToken.None);
            var person = item.Resource as Person;

            // Assert
            Assert.NotNull(person);
            Assert.Equal("332", person.Addr.Zip);
        }
    }
}
