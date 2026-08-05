// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[TestClass]
[DoNotParallelize]
public class UpdateTests : UpdateTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    protected override async Task Cleanup(Guid bookId, string title)
    {
        var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(
            serviceCollection: ConfigureServices);
        var book = api.DbContext.Books.First(candidate => candidate.Id == bookId);
        book.Title = title;
        await api.DbContext.SaveChangesAsync();
    }
}
