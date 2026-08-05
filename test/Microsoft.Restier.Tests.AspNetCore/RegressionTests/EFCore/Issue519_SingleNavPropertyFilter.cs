// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EFCore;

/// <summary>
/// A LibraryApi variant that adds an OnFilter interceptor for Publishers.
/// Only Publisher1 passes the filter; Publisher2 is excluded.
/// </summary>
public class FilteredPublisherLibraryApi : EntityFrameworkApi<LibraryContext>
{
    public FilteredPublisherLibraryApi(
        LibraryContext dbContext,
        IEdmModel model,
        IQueryHandler queryHandler,
        ISubmitHandler submitHandler)
        : base(dbContext, model, queryHandler, submitHandler)
    {
    }

    /// <summary>
    /// Filters Books to only active ones (same as LibraryApi).
    /// </summary>
    internal protected IQueryable<Book> OnFilterBooks(IQueryable<Book> entitySet)
        => entitySet.Where(c => c.IsActive);

    /// <summary>
    /// Filters Publishers to only include Publisher1.
    /// This is used to verify that single navigation property expansion
    /// respects OnFilter interceptors (GitHub issue #519).
    /// </summary>
    internal protected IQueryable<Publisher> OnFilterPublishers(IQueryable<Publisher> entitySet)
        => entitySet.Where(p => p.Id == "Publisher1");
}

[TestClass]
[DoNotParallelize]
public class Issue519_SingleNavPropertyFilter
    : Issue519_SingleNavPropertyFilter<FilteredPublisherLibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services =>
        {
            services.AddDbContext<LibraryContext>(options =>
                options.UseInMemoryDatabase(nameof(LibraryContext)));

            services.AddEFCoreProviderServices<LibraryContext>((Action<DbContextOptionsBuilder>)null);
            services.SeedDatabase<LibraryContext, LibraryTestInitializer>();
        };
}
