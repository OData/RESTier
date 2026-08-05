// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EFCore;

/// <summary>
/// Relational (SQL Server) variant of the issue #519 single-navigation filter regression.
///
/// The sibling <see cref="Issue519_SingleNavPropertyFilter"/> runs against the EF Core
/// in-memory provider, which evaluates the expression tree as plain CLR code and therefore
/// cannot detect an <em>untranslatable</em> LINQ expression. BLR-7389 showed that the
/// conditional single-navigation filter (<c>predicate(entity) ? entity : null</c>) built by
/// <c>ConventionBasedQueryExpressionProcessor.ApplySingleNavigationFilter</c> throws
/// <see cref="InvalidOperationException"/> ("could not be translated") when a real relational
/// provider (EF Core 10) projects a scalar off that navigation during <c>$expand</c> —
/// surfacing as HTTP 500. Running the same request against SQL Server exercises the query
/// translator, so this fixture guards the whole class of "RESTier emits an untranslatable
/// expression" regressions that the in-memory test cannot see.
///
/// Requires a SQL Server instance; the connection string is resolved from user secrets or the
/// <c>ConnectionStrings__LibraryContext</c> environment variable, exactly like the other
/// SQL Server-backed EF Core feature tests (see <c>AddEntityFrameworkServices</c>).
/// </summary>
[TestClass]
[DoNotParallelize]
public class Issue519_SingleNavPropertyFilter_SqlServer
    : Issue519_SingleNavPropertyFilter<FilteredPublisherLibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
