// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EFCore;

/// <summary>
/// A Library API where an entity-set filter navigates into a single-navigation property's scalar
/// (<c>book.Publisher.Id</c>) while that same navigation (<c>Publisher</c>) also has its own
/// <c>OnFilter</c>. This is the BLR-7389 shape: applying the single-navigation row filter
/// (issue #519) rewrites the <c>book.Publisher</c> node inside the entity-set predicate into the
/// conditional <c>predicate ? book.Publisher : null</c>, yielding
/// <c>(predicate ? book.Publisher : null).Id</c> — which EF Core cannot translate.
/// </summary>
public class NavScalarFilteredLibraryApi : EntityFrameworkApi<LibraryContext>
{
    public NavScalarFilteredLibraryApi(
        LibraryContext dbContext,
        IEdmModel model,
        IQueryHandler queryHandler,
        ISubmitHandler submitHandler)
        : base(dbContext, model, queryHandler, submitHandler)
    {
    }

    /// <summary>
    /// Filters the Books set by navigating into the single Publisher navigation's scalar key.
    /// </summary>
    internal protected IQueryable<Book> OnFilterBooks(IQueryable<Book> entitySet)
        => entitySet.Where(b => b.Publisher.Id == "Publisher1");

    /// <summary>
    /// Applies a row filter to the single Publisher navigation (issue #519).
    /// </summary>
    internal protected IQueryable<Publisher> OnFilterPublishers(IQueryable<Publisher> entitySet)
        => entitySet.Where(p => p.Id == "Publisher1");
}

/// <summary>
/// Regression tests for BLR-7389: a filtered query whose predicate reaches a single-navigation
/// property's scalar must remain translatable by a real relational provider (EF Core 10 on
/// SQL Server). Requires a SQL Server instance — the connection string is resolved from user
/// secrets / <c>ConnectionStrings__LibraryContext</c>, like the other SQL Server EF Core tests.
/// </summary>
[TestClass]
[DoNotParallelize]
public class Issue519_SingleNavFilterTranslation : RestierTestBase<NavScalarFilteredLibraryApi>
{
    public Issue519_SingleNavFilterTranslation()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<NavScalarFilteredLibraryApi>(WebApiConstants.RoutePrefix, services =>
            {
                services.AddEntityFrameworkServices<LibraryContext>();
            });
        };
        TestSetup();
    }

    /// <summary>
    /// The entity-set filter <c>book.Publisher.Id == "Publisher1"</c> must be applied and
    /// translated to SQL, returning only Publisher1's books — not throw an untranslatable-
    /// expression 500 because the #519 single-navigation filter injected a conditional into it.
    /// </summary>
    [TestMethod]
    public async Task FilterNavigatingIntoFilteredSingleNav_IsTranslatable()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Books");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Publisher1's books survive the OnFilterBooks predicate.
        content.Should().Contain("A Clockwork Orange");

        // Publisher2's book is excluded by book.Publisher.Id == "Publisher1".
        content.Should().NotContain("Color Purple");
    }
}
