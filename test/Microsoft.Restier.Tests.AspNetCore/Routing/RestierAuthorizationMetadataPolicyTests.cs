// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Routing;

public partial class RestierAuthorizationMetadataPolicyTests
{
    #region Test model

    private class TestPerson
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    private static IEdmModel BuildTestModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<TestPerson>("People");
        builder.Singleton<TestPerson>("Me");
        builder.EntityType<TestPerson>().Collection.Action("DiscontinuePeople");
        builder.Action("ResetData");
        return builder.GetEdmModel();
    }

    private static ODataPath ParsePath(IEdmModel model, string odataPath)
    {
        var parser = new ODataUriParser(model, new Uri(odataPath, UriKind.Relative));
        parser.Resolver = new UnqualifiedODataUriResolver { EnableCaseInsensitive = true };
        return parser.ParsePath();
    }

    #endregion

    #region ComputeTargetKey

    [Fact]
    public void ComputeTargetKey_NullPath_ReturnsClass()
    {
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path: null);
        key.Should().Be("class");
    }

    [Fact]
    public void ComputeTargetKey_EmptyPath_ReturnsClass()
    {
        var path = new ODataPath(new List<ODataPathSegment>());
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [Fact]
    public void ComputeTargetKey_MetadataSegment_ReturnsClass()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "$metadata");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [Fact]
    public void ComputeTargetKey_EntitySet_ReturnsResource()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "People");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("resource:People");
    }

    [Fact]
    public void ComputeTargetKey_EntitySetWithKey_ReturnsResource()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "People(1)");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("resource:People");
    }

    [Fact]
    public void ComputeTargetKey_Singleton_ReturnsResource()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "Me");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("resource:Me");
    }

    [Fact]
    public void ComputeTargetKey_OperationImport_ReturnsOperation()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "ResetData");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("operation:ResetData");
    }

    [Fact]
    public void ComputeTargetKey_BoundOperationOnEntitySet_ReturnsOperation()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "People/Default.DiscontinuePeople");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("operation:DiscontinuePeople");
    }

    #endregion
}
