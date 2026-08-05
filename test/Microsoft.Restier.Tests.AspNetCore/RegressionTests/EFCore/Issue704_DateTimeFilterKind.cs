// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EFCore;

// Regression for https://github.com/OData/RESTier/issues/704.
//
// Background: when an entity exposes a CLR DateTime (not DateTimeOffset) property and a request
// filters on it with a UTC literal (e.g. "ge 2000-01-01T00:00:00Z"), the AspNetCore.OData filter
// binder must produce a System.DateTime constant whose Kind is Utc — otherwise Npgsql 6+ rejects
// the parameter against a "timestamp with time zone" column ("only UTC is supported").
//
// We can verify the binder's output without any database by intercepting the IQueryable that
// RESTier hands to its IQueryExecutor and walking the LINQ expression tree for DateTime
// constants. The EF Core in-memory provider gives us a valid IQueryable<T> to bind against; we
// don't depend on its runtime semantics, only on the bound expression that lands at the executor.

/// <summary>
/// Holds the captured filter expression for a single test request. Registered as a singleton in
/// the route's service container so the executor can write to it and the test can read it.
/// </summary>
internal sealed class ExpressionCaptureSink
{
    public Expression Captured { get; set; }
}

/// <summary>
/// IQueryExecutor that captures the composed IQueryable's Expression on the way through, then
/// delegates to the inner executor.
/// </summary>
internal sealed class ExpressionCapturingQueryExecutor : IQueryExecutor
{
    private readonly ExpressionCaptureSink sink;

    public ExpressionCapturingQueryExecutor(ExpressionCaptureSink sink)
    {
        this.sink = sink;
    }

    public IQueryExecutor Inner { get; set; }

    public Task<QueryResult> ExecuteQueryAsync<TElement>(QueryContext context, IQueryable<TElement> query, CancellationToken cancellationToken)
    {
        sink.Captured = query.Expression;
        return Inner.ExecuteQueryAsync(context, query, cancellationToken);
    }

    public Task<QueryResult> ExecuteExpressionAsync<TResult>(QueryContext context, IQueryProvider queryProvider, Expression expression, CancellationToken cancellationToken)
        => Inner.ExecuteExpressionAsync<TResult>(context, queryProvider, expression, cancellationToken);
}

/// <summary>
/// Collects the DateTimeKind of every System.DateTime literal in an expression tree, whether it
/// appears as a direct <see cref="ConstantExpression"/> or — as the OData filter binder typically
/// produces — hoisted into a closure object referenced through one or more
/// <see cref="MemberExpression"/> hops. DateTimeOffset literals are also captured (their
/// UtcDateTime.Kind is recorded as the equivalent Kind) since EF will end up converting them when
/// comparing against a CLR DateTime column.
/// </summary>
internal sealed class DateTimeKindVisitor : ExpressionVisitor
{
    public List<DateTimeKind> Kinds { get; } = new();

    protected override Expression VisitConstant(ConstantExpression node)
    {
        AddIfDateLiteral(node.Value);
        return base.VisitConstant(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if ((node.Type == typeof(DateTime) || node.Type == typeof(DateTime?)
            || node.Type == typeof(DateTimeOffset) || node.Type == typeof(DateTimeOffset?))
            && IsClosureBound(node))
        {
            try
            {
                var value = Expression.Lambda(node).Compile().DynamicInvoke();
                AddIfDateLiteral(value);
            }
            catch
            {
                // Not evaluable — fall through to normal traversal.
            }
        }
        return base.VisitMember(node);
    }

    private void AddIfDateLiteral(object value)
    {
        switch (value)
        {
            case DateTime dt:
                Kinds.Add(dt.Kind);
                break;
            case DateTimeOffset dto:
                Kinds.Add(dto.Offset == TimeSpan.Zero ? DateTimeKind.Utc : DateTimeKind.Unspecified);
                break;
        }
    }

    private static bool IsClosureBound(Expression node)
    {
        while (node is MemberExpression member)
        {
            node = member.Expression;
        }
        return node is ConstantExpression;
    }
}

/// <summary>
/// Positive case: with options.TimeZone = TimeZoneInfo.Utc (RestierBreakdanceTestBase's default),
/// a UTC filter literal must reach the executor as a DateTime constant with Kind == Utc.
/// </summary>
[TestClass]
[DoNotParallelize]
public class Issue704_DateTimeFilterKind_UtcTimeZone : RestierTestBase<LibraryApi>
{
    private readonly ExpressionCaptureSink sink = new();

    public Issue704_DateTimeFilterKind_UtcTimeZone()
    {
        AddRestierAction = options =>
        {
            // RestierBreakdanceTestBase already sets options.TimeZone = TimeZoneInfo.Utc.
            options.AddRestierRoute<LibraryApi>(WebApiConstants.RoutePrefix, services =>
            {
                services.AddDbContext<LibraryContext>(dbOptions =>
                    dbOptions.UseInMemoryDatabase(nameof(LibraryContext)));
                services.AddEFCoreProviderServices<LibraryContext>((Action<DbContextOptionsBuilder>)null);
                services.SeedDatabase<LibraryContext, LibraryTestInitializer>();
                services.AddSingleton(sink);
                services.AddChainedService<IQueryExecutor>((sp, next) =>
                    new ExpressionCapturingQueryExecutor(sp.GetRequiredService<ExpressionCaptureSink>()) { Inner = next });
            });
        };
        TestSetup();
    }

    [TestMethod]
    public async Task UtcLiteral_should_bind_as_DateTime_with_Kind_Utc()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Books?$filter=PublishDate ge 2000-01-01T00:00:00Z");

        response.IsSuccessStatusCode.Should().BeTrue();
        sink.Captured.Should().NotBeNull("the custom IQueryExecutor must have observed the filtered IQueryable");

        var visitor = new DateTimeKindVisitor();
        visitor.Visit(sink.Captured);

        visitor.Kinds.Should().NotBeEmpty("the bound filter expression must contain at least one DateTime constant");
        visitor.Kinds.Should().AllBeEquivalentTo(DateTimeKind.Utc,
            "options.TimeZone = TimeZoneInfo.Utc must make AspNetCore.OData emit Kind=Utc DateTime constants — otherwise Npgsql 6+ rejects the value against a 'timestamp with time zone' column (issue #704)");
    }

    // Path-segment filter syntax (OData 4.01) is bound by Restier's own RestierQueryBuilder
    // rather than the AspNetCore.OData filter binder, so it needs its own coverage to make sure
    // the per-route ODataQuerySettings (and its TimeZone) actually reaches that code path.
    [TestMethod]
    public async Task UtcLiteral_in_pathSegment_filter_should_bind_as_DateTime_with_Kind_Utc()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Books/$filter(PublishDate ge 2000-01-01T00:00:00Z)");

        response.IsSuccessStatusCode.Should().BeTrue();
        sink.Captured.Should().NotBeNull();

        var visitor = new DateTimeKindVisitor();
        visitor.Visit(sink.Captured);

        visitor.Kinds.Should().NotBeEmpty();
        visitor.Kinds.Should().AllBeEquivalentTo(DateTimeKind.Utc,
            "the path-segment $filter binder in RestierQueryBuilder must use the route-scoped ODataQuerySettings — not a fresh `new ODataQuerySettings()` — so TimeZone propagates here too (issue #704)");
    }
}

/// <summary>
/// Negative case: with a non-UTC options.TimeZone the binder produces a DateTime constant whose
/// Kind is NOT Utc — proving the positive test would actually catch a regression. We pin the
/// time zone to a fixed offset so the assertion is deterministic on any machine.
/// </summary>
[TestClass]
[DoNotParallelize]
public class Issue704_DateTimeFilterKind_NonUtcTimeZone : RestierTestBase<LibraryApi>
{
    private readonly ExpressionCaptureSink sink = new();

    public Issue704_DateTimeFilterKind_NonUtcTimeZone()
    {
        AddRestierAction = options =>
        {
            // Override the test base's TimeZone = Utc with a fixed-offset non-UTC zone so the
            // assertion is independent of the host's local time zone.
            options.TimeZone = TimeZoneInfo.CreateCustomTimeZone(
                "Issue704+05:00", TimeSpan.FromHours(5), "Issue704+05:00", "Issue704+05:00");

            options.AddRestierRoute<LibraryApi>(WebApiConstants.RoutePrefix, services =>
            {
                services.AddDbContext<LibraryContext>(dbOptions =>
                    dbOptions.UseInMemoryDatabase(nameof(LibraryContext)));
                services.AddEFCoreProviderServices<LibraryContext>((Action<DbContextOptionsBuilder>)null);
                services.SeedDatabase<LibraryContext, LibraryTestInitializer>();
                services.AddSingleton(sink);
                services.AddChainedService<IQueryExecutor>((sp, next) =>
                    new ExpressionCapturingQueryExecutor(sp.GetRequiredService<ExpressionCaptureSink>()) { Inner = next });
            });
        };
        TestSetup();
    }

    [TestMethod]
    public async Task NonUtcTimeZone_should_strip_Utc_kind_from_filter_DateTime()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Books?$filter=PublishDate ge 2000-01-01T00:00:00Z");

        response.IsSuccessStatusCode.Should().BeTrue();
        sink.Captured.Should().NotBeNull();

        var visitor = new DateTimeKindVisitor();
        visitor.Visit(sink.Captured);

        visitor.Kinds.Should().NotBeEmpty();
        visitor.Kinds.Should().NotContain(DateTimeKind.Utc,
            "with a non-UTC ODataOptions.TimeZone the binder produces non-UTC DateTime constants — this is the bug surface that the positive test guards against");
    }
}
