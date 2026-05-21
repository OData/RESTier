// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[TestClass]
[DoNotParallelize]
public class NavigationPropertyTests : NavigationPropertyTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    protected override async Task<object> AddPublisherAndSaveAsync(Publisher publisher)
    {
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: ConfigureServices);
        context.Publishers.Add(publisher);
        context.SaveChanges();
        return context;
    }

    protected override async Task<object> AddPublishersAndSaveAsync(Publisher p1, Publisher p2)
    {
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: ConfigureServices);
        context.Publishers.Add(p1);
        context.Publishers.Add(p2);
        context.SaveChanges();
        return context;
    }

    protected override void CleanupPublisherData(object contextObj, Publisher publisher)
    {
        var context = (LibraryContext)contextObj;
        foreach (var book in publisher.Books.ToList())
        {
            context.Books.Remove(book);
        }

        context.Publishers.Remove(publisher);
        context.SaveChanges();
    }
}
