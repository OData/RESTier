// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

[TestClass]
[DoNotParallelize]
public class BatchTests : BatchTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    protected override async Task CleanupBatchBooksAsync()
    {
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: ConfigureServices);
        var books = context.Books.Where(book => book.Title.StartsWith("Batch Test")).ToList();
        foreach (var book in books)
        {
            context.Books.Remove(book);
        }

        await context.SaveChangesAsync();
    }

    protected override async Task CleanupBatchBindAsync()
    {
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: ConfigureServices);

        // Restore the orphan Book ("Sea of Rust") to its seed state by clearing any FK left
        // behind by a prior test run, then drop the test publisher if it survived.
        var orphanBookId = new Guid("2d760f15-974d-4556-8cdf-d610128b537e");
        var orphan = context.Books.FirstOrDefault(b => b.Id == orphanBookId);
        if (orphan is not null && orphan.PublisherId is not null)
        {
            orphan.PublisherId = null;
            await context.SaveChangesAsync();
        }

        var publisher = context.Publishers.FirstOrDefault(p => p.Id == "BatchBindPub");
        if (publisher is not null)
        {
            context.Publishers.Remove(publisher);
            await context.SaveChangesAsync();
        }
    }
}
