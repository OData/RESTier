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
    public void ComputeTargetKey_EntitySet_ReturnsClass()
    {
        // Standard [AllowAnonymous] / [Authorize] target class | method only — there is no
        // anchor for them on an entity-set property, so entity-set paths fall back to class-level.
        var model = BuildTestModel();
        var path = ParsePath(model, "People");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [Fact]
    public void ComputeTargetKey_EntitySetWithKey_ReturnsClass()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "People(1)");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [Fact]
    public void ComputeTargetKey_Singleton_ReturnsClass()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "Me");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
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

    #region DiscoverAttributes fixtures

    private class PlainApi
    {
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    private class ClassAnonymousApi
    {
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    private class ClassAuthorizeApi
    {
    }

    private class OperationApi
    {
        [Microsoft.Restier.AspNetCore.Model.UnboundOperation]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Admin")]
        public void RestrictedOp() { }

        [Microsoft.Restier.AspNetCore.Model.UnboundOperation]
        public void NormalOp() { }

        // Method NOT decorated with [Bound|Unbound]Operation — even though it has [AllowAnonymous],
        // it must be ignored: it's not actually a Restier operation.
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public void NotARealOperation() { }
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    private class BaseRestrictedApi
    {
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    private class DerivedAnonymousApi : BaseRestrictedApi
    {
    }

    private class DerivedInheritsApi : BaseRestrictedApi
    {
    }

    #endregion

    #region DiscoverAttributes

    [Fact]
    public void DiscoverAttributes_PlainApi_ReturnsEmpty()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(PlainApi), "class");
        attrs.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverAttributes_ClassAllowAnonymous_ReturnsAllowAnonymous()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ClassAnonymousApi), "class");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAllowAnonymous>();
    }

    [Fact]
    public void DiscoverAttributes_ClassAuthorize_ReturnsAuthorize()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ClassAuthorizeApi), "class");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    [Fact]
    public void DiscoverAttributes_RestrictedOperation_ReturnsAuthorize()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(OperationApi), "operation:RestrictedOp");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    [Fact]
    public void DiscoverAttributes_NormalOperation_ReturnsEmpty()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(OperationApi), "operation:NormalOp");
        attrs.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverAttributes_NonOperationMethod_IsIgnored()
    {
        // [AllowAnonymous] on a method without [Bound|Unbound]Operation must be ignored.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(OperationApi), "operation:NotARealOperation");
        attrs.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverAttributes_DerivedClassAnonymous_OverridesBaseAuthorize()
    {
        // Both attributes flow through; AuthorizationMiddleware applies "AllowAnonymous wins" later.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(DerivedAnonymousApi), "class");
        attrs.Should().HaveCount(2);
        attrs.Should().Contain(a => a is Microsoft.AspNetCore.Authorization.IAllowAnonymous);
        attrs.Should().Contain(a => a is Microsoft.AspNetCore.Authorization.IAuthorizeData);
    }

    [Fact]
    public void DiscoverAttributes_InheritedAuthorize_IsDiscovered()
    {
        // Subclass with no attributes inherits [Authorize] from the base class.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(DerivedInheritsApi), "class");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    [Fact]
    public void DiscoverAttributes_ClassAndUnknownOperationCombined_ReturnsClassOnly()
    {
        // ClassAuthorizeApi has [Authorize]; no operation method named "Anything" exists,
        // so only class-level attributes apply.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ClassAuthorizeApi), "operation:Anything");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    #endregion
}
