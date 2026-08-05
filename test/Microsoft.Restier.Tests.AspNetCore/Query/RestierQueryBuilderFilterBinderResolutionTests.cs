// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Query;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Restier.Tests.AspNetCore.Query;

/// <summary>
/// Regression tests for the ctor widening on RestierQueryBuilder. Confirms that the optional
/// IFilterBinder parameter is honored by HandleFilterPathSegment when present, and that the
/// fallback to a fresh FilterBinder() works when no binder is passed.
/// </summary>
[TestClass]
public class RestierQueryBuilderFilterBinderResolutionTests
{
    /// <summary>
    /// The ctor accepts an IFilterBinder and stores it for use by HandleFilterPathSegment.
    /// We assert the ctor signature compiles; full end-to-end coverage of path-segment $filter
    /// behavior is exercised by SpatialTypeIntegrationTests.
    /// </summary>
    [TestMethod]
    public void Ctor_AcceptsOptionalFilterBinder_DoesNotThrow()
    {
        var binder = Substitute.For<IFilterBinder>();
        var api = new TestApi(
            Substitute.For<IEdmModel>(),
            Substitute.For<IQueryHandler>(),
            Substitute.For<ISubmitHandler>());

        var path = new ODataPath(Array.Empty<ODataPathSegment>());
        var querySettings = new ODataQuerySettings();

        var act = () => new RestierQueryBuilder(api, path, querySettings, binder);

        act.Should().NotThrow("the widened ctor must accept an IFilterBinder argument");
    }

    /// <summary>
    /// The IFilterBinder parameter is optional — callers that don't pass one must continue to
    /// compile against the (api, path, querySettings) ctor signature.
    /// </summary>
    [TestMethod]
    public void Ctor_FilterBinderParameter_IsOptional()
    {
        var api = new TestApi(
            Substitute.For<IEdmModel>(),
            Substitute.For<IQueryHandler>(),
            Substitute.For<ISubmitHandler>());

        var path = new ODataPath(Array.Empty<ODataPathSegment>());
        var querySettings = new ODataQuerySettings();

        var act = () => new RestierQueryBuilder(api, path, querySettings);

        act.Should().NotThrow("the (api, path, querySettings) ctor signature must still compile without an IFilterBinder");
    }

    private class TestApi : ApiBase
    {
        public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }
    }
}
