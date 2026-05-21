// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EF6;

[Collection("LibraryApiEF6")]
public class Issue759_BatchInsertWithRelatedEntities : Issue759_BatchInsertWithRelatedEntities<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    protected override async Task CleanupIssue759Async()
    {
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: ConfigureServices);

        // Drop test Books (FK first to avoid orphan/constraint issues). Match by the fixed
        // child-book id used in the batch payload AND by PublisherId, since the batch path
        // currently saves Books with PublisherId=null even when posted to /Publishers('X')/Books.
        var childBookId = new Guid("d7591759-7591-7591-7591-759175917591");
        var testBooks = context.Books
            .Where(b => b.Id == childBookId || b.PublisherId == "Issue759Pub" || b.Title.StartsWith("Issue759"))
            .ToList();
        foreach (var book in testBooks)
        {
            context.Books.Remove(book);
        }

        if (testBooks.Count > 0)
        {
            await context.SaveChangesAsync();
        }

        var publisher = context.Publishers.FirstOrDefault(p => p.Id == "Issue759Pub");
        if (publisher is not null)
        {
            context.Publishers.Remove(publisher);
            await context.SaveChangesAsync();
        }
    }
}
