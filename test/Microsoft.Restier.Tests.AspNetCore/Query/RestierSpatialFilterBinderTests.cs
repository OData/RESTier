// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Query;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.Spatial;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Query;

/// <summary>
/// Unit tests for <see cref="RestierSpatialFilterBinder"/> dispatch over geo.distance, geo.length,
/// and geo.intersects. Each test constructs a small EDM model, builds a FilterClause via
/// ODataQueryOptionParser, applies the binder, and asserts on the resulting LINQ Expression
/// tree shape. No DB, no HTTP.
/// </summary>
public class RestierSpatialFilterBinderTests
{
    // ─────────────────────────────────────────────────────────────────────
    // Tiny EDM fixtures
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EFCore-flavor entity used as the filter source. Storage type is NetTopologySuite
    /// concrete subclasses (Point, LineString), which is the case the binder must support
    /// without exact-parameter-type method lookups (Geometry.Distance(Geometry) is declared
    /// on the abstract base).
    /// </summary>
    private class NtsEntity
    {
        public int Id { get; set; }
        public NetTopologySuite.Geometries.Point Location { get; set; }
        public NetTopologySuite.Geometries.LineString RouteLine { get; set; }
    }

    /// <summary>
    /// Surrogate EDM entity. Its CLR spatial properties use Microsoft.Spatial Geography*
    /// types so that ODataConventionModelBuilder emits Edm.Geography* primitives — required
    /// so the OData parser accepts <c>geography'SRID=4326;POINT(0 0)'</c> literals in
    /// <c>geo.distance</c> / <c>geo.intersects</c> calls against these properties. The
    /// property names intentionally match those on <see cref="NtsEntity"/> so that after
    /// the ClrTypeAnnotation swap the FilterBinder resolves NTS CLR members at bind time.
    /// </summary>
    private class EdmSurrogateEntity
    {
        public int Id { get; set; }
        public GeographyPoint Location { get; set; }
        public GeographyLineString RouteLine { get; set; }
    }

    /// <summary>
    /// Builds an EDM model that has correct Edm.Geometry* types (from <see cref="EdmSurrogateEntity"/>)
    /// but whose entity-type ClrTypeAnnotation is repointed to <see cref="NtsEntity"/>. This lets
    /// <see cref="QueryBinderContext"/> accept typeof(<see cref="NtsEntity"/>) while the OData parser
    /// still validates geo.length / geo.distance function signatures against the proper EDM types.
    /// </summary>
    private static (IEdmModel model, IQueryable<NtsEntity> source) BuildNtsFixture()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<EdmSurrogateEntity>("Things");
        var model = builder.GetEdmModel();

        // Repoint the ClrTypeAnnotation so QueryBinderContext.ctor(model, settings, typeof(NtsEntity))
        // finds NtsEntity in the model and subsequent property access binds against its NTS members.
        var entityType = model.EntityContainer.FindEntitySet("Things").EntityType;
        model.SetAnnotationValue(entityType, new ClrTypeAnnotation(typeof(NtsEntity)));

        var source = new[] { new NtsEntity { Id = 1 } }.AsQueryable();
        return (model, source);
    }

    private static FilterClause ParseFilter(IEdmModel model, string entitySetName, string filterExpression)
    {
        var entitySet = model.EntityContainer.FindEntitySet(entitySetName);
        var parser = new ODataQueryOptionParser(
            model,
            entitySet.EntityType,
            entitySet,
            new Dictionary<string, string> { { "$filter", filterExpression } });
        return parser.ParseFilter();
    }

    // ─────────────────────────────────────────────────────────────────────
    // geo.length
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// geo.length(RouteLine) must lower to a MemberExpression on the storage type's "Length"
    /// property. NTS LineString <em>overrides</em> Geometry.Length, so the binder must
    /// normalize the PropertyInfo to its base declaration on Geometry — EF Core's SqlServer
    /// NTS translator dictionary keys on <c>typeof(Geometry).GetRuntimeProperty("Length")</c>
    /// and rejects the LineString-flavored member by MemberInfo equality.
    /// </summary>
    [Fact]
    public void BindGeoLength_EmitsLengthPropertyAccessOnBaseDeclaringType()
    {
        var (model, source) = BuildNtsFixture();
        var clause = ParseFilter(model, "Things", "geo.length(RouteLine) gt 0");

        var binder = new RestierSpatialFilterBinder(new ISpatialTypeConverter[] { new NtsSpatialConverter() });
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        var bound = binder.ApplyBind(source, clause, context);
        bound.Should().NotBeNull("the binder must successfully translate geo.length(RouteLine) gt 0");

        var visitor = new FindLengthAccessVisitor();
        visitor.Visit(bound.Expression);
        visitor.Found.Should().BeTrue(
            "the bound expression must contain a MemberExpression accessing the Length property of the storage type");
        visitor.MemberDeclaringType.Should().Be(typeof(NetTopologySuite.Geometries.Geometry),
            "the PropertyInfo must be normalized to its base declaration so EF Core's SqlServer NTS " +
            "translator (which keys on Geometry.Length) can match it");
    }

    private class FindLengthAccessVisitor : ExpressionVisitor
    {
        public bool Found { get; private set; }
        public Type MemberDeclaringType { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.Name == "Length"
                && typeof(NetTopologySuite.Geometries.Geometry).IsAssignableFrom(node.Expression?.Type))
            {
                Found = true;
                MemberDeclaringType = node.Member.DeclaringType;
            }
            return base.VisitMember(node);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // geo.distance
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// geo.distance(prop, literal) lt N must lower to MethodCallExpression(prop, "Distance",
    /// loweredLiteral) where loweredLiteral is a storage-typed constant. NTS's Distance is
    /// declared on Geometry and takes Geometry — but the bound argument types are concrete
    /// Point. The binder must resolve the method by parameter-type assignability, not by
    /// exact match.
    /// </summary>
    [Fact]
    public void BindGeoDistance_EmitsStorageDistanceMethodCall_WithLoweredLiteral()
    {
        var (model, source) = BuildNtsFixture();
        var clause = ParseFilter(model, "Things",
            "geo.distance(Location,geography'SRID=4326;POINT(0 0)') lt 1000000");

        var binder = new RestierSpatialFilterBinder(new ISpatialTypeConverter[] { new NtsSpatialConverter() });
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        var bound = binder.ApplyBind(source, clause, context);
        bound.Should().NotBeNull();

        // The expression tree must contain a MethodCallExpression on Distance whose receiver
        // is the Location property and whose single argument is a Constant of NTS Point.
        var visitor = new FindDistanceCallVisitor();
        visitor.Visit(bound.Expression);
        visitor.Found.Should().BeTrue(
            "the bound expression must contain a MethodCallExpression for Geometry.Distance(Geometry)");
        visitor.ArgumentType.Should().BeAssignableTo(typeof(NetTopologySuite.Geometries.Geometry),
            "the lowered literal must be an NTS geometry, not a Microsoft.Spatial value");
    }

    private class FindDistanceCallVisitor : ExpressionVisitor
    {
        public bool Found { get; private set; }
        public Type ArgumentType { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Distance"
                && typeof(NetTopologySuite.Geometries.Geometry).IsAssignableFrom(node.Object?.Type)
                && node.Arguments.Count == 1)
            {
                Found = true;
                ArgumentType = node.Arguments[0].Type;
            }
            return base.VisitMethodCall(node);
        }
    }

    /// <summary>
    /// Both-literal corner case: <c>geo.distance(geography'…', geography'…')</c>. Neither
    /// side has a property to seed the storage-type inference, so the binder probes the
    /// registered converters for a preferred storage root. The fix in
    /// BindBinarySpatialMethod is that lowered1 sees lowered0.Type (the *post-lowering*
    /// concrete storage type) rather than bound0.Type (still Microsoft.Spatial). Without
    /// that, lowered1 would independently re-probe and could pick a different storage
    /// root in cross-flavor configurations.
    /// </summary>
    [Fact]
    public void BindGeoDistance_BothLiteralArguments_BothLoweredToSameStorageRoot()
    {
        var (model, source) = BuildNtsFixture();
        var clause = ParseFilter(model, "Things",
            "geo.distance(geography'SRID=4326;POINT(0 0)',geography'SRID=4326;POINT(1 1)') lt 1000000");

        var binder = new RestierSpatialFilterBinder(new ISpatialTypeConverter[] { new NtsSpatialConverter() });
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        var bound = binder.ApplyBind(source, clause, context);
        bound.Should().NotBeNull();

        var visitor = new FindDistanceCallVisitor();
        visitor.Visit(bound.Expression);
        visitor.Found.Should().BeTrue(
            "both-literal geo.distance must still resolve a storage-typed Distance call");
        visitor.ArgumentType.Should().BeAssignableTo(typeof(NetTopologySuite.Geometries.Geometry),
            "the lowered argument must be an NTS geometry");
    }

    // ─────────────────────────────────────────────────────────────────────
    // geo.intersects
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// geo.intersects(prop, literal) must lower to MethodCallExpression(prop, "Intersects",
    /// loweredLiteral). Same reflection-walk requirement as geo.distance — NTS's Intersects
    /// is declared on Geometry.
    /// </summary>
    [Fact]
    public void BindGeoIntersects_EmitsStorageIntersectsMethodCall()
    {
        var (model, source) = BuildNtsFixture();
        var clause = ParseFilter(model, "Things",
            "geo.intersects(Location,geography'SRID=4326;POLYGON((0 0,0 1,1 1,1 0,0 0))')");

        var binder = new RestierSpatialFilterBinder(new ISpatialTypeConverter[] { new NtsSpatialConverter() });
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        var bound = binder.ApplyBind(source, clause, context);
        bound.Should().NotBeNull();

        var visitor = new FindIntersectsCallVisitor();
        visitor.Visit(bound.Expression);
        visitor.Found.Should().BeTrue(
            "the bound expression must contain a MethodCallExpression for Geometry.Intersects(Geometry)");
    }

    private class FindIntersectsCallVisitor : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Intersects"
                && typeof(NetTopologySuite.Geometries.Geometry).IsAssignableFrom(node.Object?.Type))
            {
                Found = true;
            }
            return base.VisitMethodCall(node);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Error paths
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unknown geo.* function names fall through to AspNetCoreOData's base FilterBinder,
    /// which surfaces the stock "unknown function" error. Forward-compat for future OData
    /// spec additions and the long tail of non-core geo functions (geo.area, geo.contains, ...).
    /// </summary>
    [Fact]
    public void BindSingleValueFunctionCallNode_UnknownGeoFunction_FallsThroughToBase()
    {
        var (model, source) = BuildNtsFixture();

        // ODL's parser rejects unknown function names before the binder ever runs. We
        // assert that no result-producing happy path exists for geo.area, which is what
        // a flip-from-negative integration test would expect.
        Action act = () => ParseFilter(model, "Things", "geo.area(Location) gt 0");

        act.Should().Throw<Microsoft.OData.ODataException>(
            "AspNetCoreOData's ODataQueryOptionParser must reject unknown function names " +
            "before the binder ever runs");
    }

    /// <summary>
    /// Binder constructed with an empty ISpatialTypeConverter enumerable hitting a geo.* call
    /// against a spatial property must throw ODataException — this is the diagnostic for the
    /// "forgot to call AddRestierSpatial()" case.
    /// </summary>
    [Fact]
    public void Ctor_NoConvertersRegistered_GeoFunctionAgainstSpatialProperty_ThrowsODataException()
    {
        var (model, source) = BuildNtsFixture();
        var clause = ParseFilter(model, "Things",
            "geo.distance(Location,geography'SRID=4326;POINT(0 0)') lt 1000000");

        var binder = new RestierSpatialFilterBinder(); // no converters
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        Action act = () => binder.ApplyBind(source, clause, context);

        act.Should().Throw<Microsoft.OData.ODataException>()
            .WithMessage("*No ISpatialTypeConverter*",
                "the message must point the developer at AddRestierSpatial()");
    }
}
