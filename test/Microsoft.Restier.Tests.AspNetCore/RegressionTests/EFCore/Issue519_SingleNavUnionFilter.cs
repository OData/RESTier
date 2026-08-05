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
/// A Library API whose single-navigation <c>OnFilter</c> expresses OR semantics via
/// <see cref="Queryable.Union{TSource}(IQueryable{TSource}, System.Collections.Generic.IEnumerable{TSource})"/>:
/// a Publisher is kept if it is Publisher1 OR Publisher2.
/// </summary>
public class UnionFilteredPublisherLibraryApi : EntityFrameworkApi<LibraryContext>
{
    public UnionFilteredPublisherLibraryApi(
        LibraryContext dbContext,
        IEdmModel model,
        IQueryHandler queryHandler,
        ISubmitHandler submitHandler)
        : base(dbContext, model, queryHandler, submitHandler)
    {
    }

    /// <summary>
    /// Keeps Publisher1 OR Publisher2 by unioning two filtered branches. Both branches must be
    /// honoured; dropping the second (issue in <c>ExtractCombinedPredicate</c>) would silently
    /// narrow the filter to Publisher1 only.
    /// </summary>
    internal protected IQueryable<Publisher> OnFilterPublishers(IQueryable<Publisher> entitySet)
        => entitySet.Where(p => p.Id == "Publisher1").Union(entitySet.Where(p => p.Id == "Publisher2"));
}

/// <summary>
/// Regression tests proving that a single-navigation <c>OnFilter</c> built from a
/// <c>Union</c> of two predicates enforces BOTH branches (OR), rather than silently dropping the
/// second. Requires SQL Server, like the other SQL Server-backed EF Core tests.
/// </summary>
[TestClass]
[DoNotParallelize]
public class Issue519_SingleNavUnionFilter : RestierTestBase<UnionFilteredPublisherLibraryApi>
{
    public Issue519_SingleNavUnionFilter()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<UnionFilteredPublisherLibraryApi>(WebApiConstants.RoutePrefix, services =>
            {
                services.AddEntityFrameworkServices<LibraryContext>();
            });
        };
        TestSetup();
    }

    /// <summary>
    /// Both Publisher1 and Publisher2 pass the unioned filter, so expanding Publisher on their
    /// books must keep both navigation objects. Publisher3 fails both branches and is nulled.
    /// </summary>
    [TestMethod]
    public async Task ExpandSingleNav_UnionedFilter_KeepsBothBranches()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Books?$expand=Publisher");
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // First Union branch: Publisher1 is kept.
        content.Should().Contain("\"Id\":\"Publisher1\"");

        // Second Union branch: Publisher2 must ALSO be kept (this is what dropping the branch breaks).
        content.Should().Contain("\"Id\":\"Publisher2\"");

        // Publisher3 matches neither branch, so its navigation is filtered out.
        content.Should().NotContain("\"Id\":\"Publisher3\"");
    }
}
