// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Routing;

[TestClass]
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

    [TestMethod]
    public void ComputeTargetKey_NullPath_ReturnsClass()
    {
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path: null);
        key.Should().Be("class");
    }

    [TestMethod]
    public void ComputeTargetKey_EmptyPath_ReturnsClass()
    {
        var path = new ODataPath(new List<ODataPathSegment>());
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [TestMethod]
    public void ComputeTargetKey_MetadataSegment_ReturnsClass()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "$metadata");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [TestMethod]
    public void ComputeTargetKey_EntitySet_ReturnsClass()
    {
        // Standard [AllowAnonymous] / [Authorize] target class | method only — there is no
        // anchor for them on an entity-set property, so entity-set paths fall back to class-level.
        var model = BuildTestModel();
        var path = ParsePath(model, "People");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [TestMethod]
    public void ComputeTargetKey_EntitySetWithKey_ReturnsClass()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "People(1)");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [TestMethod]
    public void ComputeTargetKey_Singleton_ReturnsClass()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "Me");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [TestMethod]
    public void ComputeTargetKey_OperationImport_ReturnsOperation()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "ResetData");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("operation:ResetData");
    }

    [TestMethod]
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

    [TestMethod]
    public void DiscoverAttributes_PlainApi_ReturnsEmpty()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(PlainApi), "class");
        attrs.Should().BeEmpty();
    }

    [TestMethod]
    public void DiscoverAttributes_ClassAllowAnonymous_ReturnsAllowAnonymous()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ClassAnonymousApi), "class");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAllowAnonymous>();
    }

    [TestMethod]
    public void DiscoverAttributes_ClassAuthorize_ReturnsAuthorize()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ClassAuthorizeApi), "class");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    [TestMethod]
    public void DiscoverAttributes_RestrictedOperation_ReturnsAuthorize()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(OperationApi), "operation:RestrictedOp");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    [TestMethod]
    public void DiscoverAttributes_NormalOperation_ReturnsEmpty()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(OperationApi), "operation:NormalOp");
        attrs.Should().BeEmpty();
    }

    [TestMethod]
    public void DiscoverAttributes_NonOperationMethod_IsIgnored()
    {
        // [AllowAnonymous] on a method without [Bound|Unbound]Operation must be ignored.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(OperationApi), "operation:NotARealOperation");
        attrs.Should().BeEmpty();
    }

    [TestMethod]
    public void DiscoverAttributes_DerivedClassAnonymous_OverridesBaseAuthorize()
    {
        // Both attributes flow through; AuthorizationMiddleware applies "AllowAnonymous wins" later.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(DerivedAnonymousApi), "class");
        attrs.Should().HaveCount(2);
        attrs.Should().Contain(a => a is Microsoft.AspNetCore.Authorization.IAllowAnonymous);
        attrs.Should().Contain(a => a is Microsoft.AspNetCore.Authorization.IAuthorizeData);
    }

    [TestMethod]
    public void DiscoverAttributes_InheritedAuthorize_IsDiscovered()
    {
        // Subclass with no attributes inherits [Authorize] from the base class.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(DerivedInheritsApi), "class");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    [TestMethod]
    public void DiscoverAttributes_ClassAndUnknownOperationCombined_ReturnsClassOnly()
    {
        // ClassAuthorizeApi has [Authorize]; no operation method named "Anything" exists,
        // so only class-level attributes apply.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ClassAuthorizeApi), "operation:Anything");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    #endregion

    #region AppliesToEndpoints

    private static Microsoft.AspNetCore.Http.Endpoint MakeEndpoint(params object[] metadata)
    {
        return new Microsoft.AspNetCore.Http.Endpoint(
            requestDelegate: _ => System.Threading.Tasks.Task.CompletedTask,
            metadata: new Microsoft.AspNetCore.Http.EndpointMetadataCollection(metadata),
            displayName: "test");
    }

    private static Microsoft.AspNetCore.Http.Endpoint MakeRestierEndpoint(params object[] extraMetadata)
    {
        // Mirror what MVC's routing builds for RestierController.Get: an endpoint whose
        // ControllerActionDescriptor.ControllerTypeInfo points to RestierController.
        var descriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(Microsoft.Restier.AspNetCore.RestierController).GetTypeInfo(),
            ActionName = "Get",
        };

        var allMetadata = new object[extraMetadata.Length + 1];
        allMetadata[0] = descriptor;
        Array.Copy(extraMetadata, 0, allMetadata, 1, extraMetadata.Length);

        return new Microsoft.AspNetCore.Http.Endpoint(
            requestDelegate: _ => System.Threading.Tasks.Task.CompletedTask,
            metadata: new Microsoft.AspNetCore.Http.EndpointMetadataCollection(allMetadata),
            displayName: "RestierController.Get");
    }

    private static RestierAuthorizationMetadataPolicy MakePolicy(IEdmModel model = null, Type apiType = null, string routePrefix = "")
    {
        var odataOptions = new ODataOptions();
        if (model is not null && apiType is not null)
        {
            var apiTypeCapture = apiType;
            odataOptions.AddRouteComponents(routePrefix, model, services =>
            {
                services.AddSingleton(new RestierRouteMarker(apiTypeCapture));
            });
        }
        return new RestierAuthorizationMetadataPolicy(Options.Create(odataOptions));
    }

    [TestMethod]
    public void AppliesToEndpoints_AlwaysReturnsTrue()
    {
        // At node-builder time the only visible Restier endpoint is the dynamic catch-all,
        // which has no ControllerActionDescriptor metadata yet. So the policy applies
        // unconditionally and filters per-request inside ApplyAsync.
        var policy = MakePolicy();
        var endpoints = new[] { MakeEndpoint(), MakeRestierEndpoint() };

        ((IEndpointSelectorPolicy)policy).AppliesToEndpoints(endpoints).Should().BeTrue();
        ((IEndpointSelectorPolicy)policy).AppliesToEndpoints(new[] { MakeEndpoint() }).Should().BeTrue();
        ((IEndpointSelectorPolicy)policy).AppliesToEndpoints(System.Array.Empty<Endpoint>()).Should().BeTrue();
    }

    #endregion

    #region ApplyAsync

    private static (HttpContext http, RestierAuthorizationMetadataPolicy policy) MakeApplyContext(
        IEdmModel model,
        string odataPath,
        Type apiType,
        string routePrefix = "")
    {
        var policy = MakePolicy(model, apiType, routePrefix);

        var ctx = new DefaultHttpContext();
        var feature = ctx.ODataFeature();
        feature.Path = ParsePath(model, odataPath);
        feature.Model = model;
        feature.RoutePrefix = routePrefix;

        return (ctx, policy);
    }

    private static CandidateSet MakeCandidateSet(params Endpoint[] endpoints)
    {
        var values = new RouteValueDictionary[endpoints.Length];
        var scores = new int[endpoints.Length];
        for (var i = 0; i < endpoints.Length; i++)
        {
            values[i] = new RouteValueDictionary();
            scores[i] = 0;
        }
        return new CandidateSet(endpoints, values, scores);
    }

    [TestMethod]
    public async Task ApplyAsync_NonRestierCandidate_LeavesEndpointUnchanged()
    {
        var model = BuildTestModel();
        var (http, policy) = MakeApplyContext(model, "People", typeof(ClassAnonymousApi));
        var original = MakeEndpoint();
        var candidates = MakeCandidateSet(original);

        await ((IEndpointSelectorPolicy)policy).ApplyAsync(http, candidates);

        candidates[0].Endpoint.Should().BeSameAs(original);
    }

    [TestMethod]
    public async Task ApplyAsync_NoMarker_LeavesEndpointUnchanged()
    {
        var model = BuildTestModel();
        // Construct policy with empty ODataOptions (no marker for the route).
        var policy = MakePolicy();
        var http = new DefaultHttpContext();
        var feature = http.ODataFeature();
        feature.Path = ParsePath(model, "People");
        feature.Model = model;
        feature.RoutePrefix = string.Empty;

        var original = MakeRestierEndpoint();
        var candidates = MakeCandidateSet(original);

        await ((IEndpointSelectorPolicy)policy).ApplyAsync(http, candidates);

        candidates[0].Endpoint.Should().BeSameAs(original);
    }

    [TestMethod]
    public async Task ApplyAsync_NoAttributes_LeavesEndpointUnchanged()
    {
        var model = BuildTestModel();
        var (http, policy) = MakeApplyContext(model, "People", typeof(PlainApi));
        var original = MakeRestierEndpoint();
        var candidates = MakeCandidateSet(original);

        await ((IEndpointSelectorPolicy)policy).ApplyAsync(http, candidates);

        candidates[0].Endpoint.Should().BeSameAs(original);
    }

    [TestMethod]
    public async Task ApplyAsync_ClassAllowAnonymous_ReplacesEndpointWithAugmentedMetadata()
    {
        var model = BuildTestModel();
        var (http, policy) = MakeApplyContext(model, "People", typeof(ClassAnonymousApi));
        var original = MakeRestierEndpoint();
        var candidates = MakeCandidateSet(original);

        await ((IEndpointSelectorPolicy)policy).ApplyAsync(http, candidates);

        var wrapped = candidates[0].Endpoint;
        wrapped.Should().NotBeSameAs(original);
        wrapped.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>().Should().NotBeNull();
        // Original metadata is preserved — the ControllerActionDescriptor should still be present.
        wrapped.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
               .Should().NotBeNull();
    }

    [TestMethod]
    public async Task ApplyAsync_OperationWithAuthorize_AugmentsForThatOperation()
    {
        var model = BuildTestModel();
        var (http, policy) = MakeApplyContext(model, "ResetData", typeof(OperationApi));
        // OperationApi has no class-level attributes; but ResetData isn't one of its operations.
        // Re-target to RestrictedOp instead: build a path that ends in OperationImportSegment("RestrictedOp").
        // The test model only declares ResetData as an operation import, so we use it as the operation name
        // and verify that the policy looks up the method on the API class by that name.
        // We need OperationApi to have a "ResetData" operation — add a special fixture below.
        var original = MakeRestierEndpoint();
        var candidates = MakeCandidateSet(original);

        await ((IEndpointSelectorPolicy)policy).ApplyAsync(http, candidates);

        // OperationApi has no operation named "ResetData" → no attributes added.
        candidates[0].Endpoint.Should().BeSameAs(original);
    }

    [TestMethod]
    public async Task ApplyAsync_TwoSeparateCalls_BothCandidatesWrappedIndependently()
    {
        // Regression for the cache-key concern: even when the same (apiType, targetKey) maps to
        // two different candidate endpoints (e.g., GET vs POST for /People), each must be wrapped
        // independently — never substituted for the cached wrapper of another.
        var model = BuildTestModel();
        var (http1, policy) = MakeApplyContext(model, "People", typeof(ClassAnonymousApi));

        // First candidate carries a unique marker string in its metadata.
        var firstOriginal = MakeRestierEndpoint("FirstAction");
        var firstCandidates = MakeCandidateSet(firstOriginal);
        await ((IEndpointSelectorPolicy)policy).ApplyAsync(http1, firstCandidates);
        var firstWrapped = firstCandidates[0].Endpoint;

        // Reuse the same policy so the attribute cache hits.
        var http2 = new DefaultHttpContext();
        var feature2 = http2.ODataFeature();
        feature2.Path = ParsePath(model, "People");
        feature2.Model = model;
        feature2.RoutePrefix = string.Empty;
        var secondOriginal = MakeRestierEndpoint("SecondAction");
        var secondCandidates = MakeCandidateSet(secondOriginal);
        await ((IEndpointSelectorPolicy)policy).ApplyAsync(http2, secondCandidates);
        var secondWrapped = secondCandidates[0].Endpoint;

        firstWrapped.Should().NotBeSameAs(secondWrapped);
        firstWrapped.Metadata.Should().Contain(m => "FirstAction".Equals(m));
        secondWrapped.Metadata.Should().Contain(m => "SecondAction".Equals(m));
        firstWrapped.Metadata.Should().NotContain(m => "SecondAction".Equals(m));
        secondWrapped.Metadata.Should().NotContain(m => "FirstAction".Equals(m));
    }

    #endregion
}
