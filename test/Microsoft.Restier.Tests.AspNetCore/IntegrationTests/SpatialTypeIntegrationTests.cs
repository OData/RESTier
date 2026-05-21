// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.IntegrationTests;

/// <summary>
/// End-to-end integration tests that verify spatial type support in the Library API.
/// These tests exercise the EDM model metadata and HTTP query surface for the
/// <c>SpatialPlaces</c> entity set introduced in H1.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SpatialTypeIntegrationTests : RestierTestBase<LibraryApi>
{
    private readonly Action<IServiceCollection> _configureServices
        = services => services.AddEntityFrameworkServices<LibraryContext>();

    /// <summary>
    /// Probes whether the test SQL Server instance has CLR enabled (required for the
    /// geography spatial methods that geo.* filters translate to). SQL Server Express
    /// does not support CLR; other editions require sp_configure 'clr enabled', 1.
    /// </summary>
    public static bool SqlServerClrEnabled
    {
        get
        {
            if (_clrProbeResult.HasValue)
            {
                return _clrProbeResult.Value;
            }

            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddUserSecrets(typeof(LibraryContext).Assembly, optional: true)
                    .Build();
                var raw = configuration.GetConnectionString(nameof(LibraryContext));
                if (string.IsNullOrEmpty(raw))
                {
                    _clrProbeResult = false;
                    return false;
                }

                // Probe against master so the check does not depend on the LibraryApiEFCore
                // collection fixture having created the test database yet.
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(raw)
                {
                    InitialCatalog = "master",
                };

                using var connection = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                // STDistance is a CLR-routed geography method. If CLR is disabled, this
                // throws SqlException ("Common Language Runtime (CLR) is not enabled").
                command.CommandText =
                    "SELECT geography::STGeomFromText('POINT(0 0)', 4326).STDistance(geography::STGeomFromText('POINT(1 1)', 4326))";
                _ = command.ExecuteScalar();
                _clrProbeResult = true;
                return true;
            }
            catch (System.Exception)
            {
                _clrProbeResult = false;
                return false;
            }
        }
    }

    private static bool? _clrProbeResult;

    // ─────────────────────────────────────────────────────────────────────────
    // EDM / metadata assertions (EFCore)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EFCore: $metadata must expose a SpatialPlace EntityType with the expected Edm spatial types.
    /// HeadquartersLocation and IndoorOrigin are NTS Point columns with HasColumnType("geography"),
    /// so they resolve to Edm.GeographyPoint.  ServiceArea is an NTS Polygon geography column,
    /// so it resolves to Edm.GeographyPolygon.
    /// </summary>
    [TestMethod]
    public async Task EFCore_Metadata_SpatialPlace_HasCorrectEdmTypes()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<LibraryApi>(
            serviceCollection: _configureServices);

        metadata.Should().NotBeNull("$metadata request must succeed");

        var xml = metadata.ToString();

        xml.Should().Contain(
            "EntityType Name=\"SpatialPlace\"",
            "SpatialPlace must be an EntityType in the EDM");

        xml.Should().Contain(
            "Name=\"HeadquartersLocation\" Type=\"Edm.GeographyPoint\"",
            "NTS Point with HasColumnType(\"geography\") must map to Edm.GeographyPoint");

        xml.Should().Contain(
            "Name=\"ServiceArea\" Type=\"Edm.GeographyPolygon\"",
            "NTS Polygon with HasColumnType(\"geography\") must map to Edm.GeographyPolygon");

        xml.Should().Contain(
            "Name=\"IndoorOrigin\" Type=\"Edm.GeographyPoint\"",
            "[Spatial(typeof(GeographyPoint))] with geography column type must map to Edm.GeographyPoint");

        xml.Should().Contain(
            "EntitySet Name=\"SpatialPlaces\"",
            "SpatialPlaces EntitySet must be exposed in the EDM container");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HTTP GET — collection and single-entity
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EFCore: GET /SpatialPlaces must return 200 OK (entity set is routable).
    /// </summary>
    [TestMethod]
    public async Task EFCore_Get_SpatialPlaces_Returns200()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces",
            serviceCollection: _configureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// EFCore: GET /SpatialPlaces(1) must return 200 OK.
    /// The seeded record always exists (spatial values may be null when SQL CLR is disabled,
    /// but the record itself is always inserted).
    /// </summary>
    [TestMethod]
    public async Task EFCore_Get_SpatialPlaces_ByKey_Returns200()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces(1)",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the seeded SpatialPlace record (Id=1) must be retrievable via key lookup");

        content.Should().Contain("\"Id\":1",
            "the returned entity must have the expected Id");

        content.Should().Contain("\"Name\":\"Spatial Place 1\"",
            "the returned entity must have the expected Name");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Positive — geo.distance $filter (spec B)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EFCore: $filter using geo.distance must return 200 OK and include the seeded
    /// HeadquartersLocation row (Amsterdam, ~5570 km from POINT(0 0)).  Spec B flips
    /// the previous spec-A negative assertion to a positive one. Requires CLR on the
    /// SQL Server instance.
    /// </summary>
    [TestMethod, Ignore("Requires SQL Server CLR for geography spatial method execution (sp_configure 'clr enabled', 1).")]
    public async Task EFCore_Filter_GeoDistance_TranslatesAndReturnsSeededRow()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.distance(HeadquartersLocation,geography'SRID=4326;POINT(0 0)') lt 10000000",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "EFCore + NTS now translates geo.distance to a server-side spatial operator");

        content.Should().Contain("\"Name\":\"Spatial Place 1\"",
            "the Amsterdam row is well inside 10000 km from POINT(0 0)");
    }

    /// <summary>
    /// EFCore: $filter using geo.length must return 200 OK and include the seeded RouteLine row.
    /// The seeded LineString (0,0)->(1,1)->(2,2) has positive length, so it survives the filter.
    /// </summary>
    [TestMethod, Ignore("Requires SQL Server CLR for geography spatial method execution (sp_configure 'clr enabled', 1).")]
    public async Task EFCore_Filter_GeoLength_TranslatesPropertyAccess()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.length(RouteLine) gt 0",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"Name\":\"Spatial Place 1\"",
            "the seeded RouteLine LINESTRING(0 0, 1 1, 2 2) has positive length");
    }

    /// <summary>
    /// EFCore: $filter using geo.intersects must return 200 OK and include the seeded
    /// ServiceArea row when the test point lies inside the polygon. The seeded polygon
    /// covers (0,0)–(1,1) so a query point at (0.5, 0.5) intersects.
    /// </summary>
    [TestMethod, Ignore("Requires SQL Server CLR for geography spatial method execution (sp_configure 'clr enabled', 1).")]
    public async Task EFCore_Filter_GeoIntersects_TranslatesMethodCall()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.intersects(ServiceArea,geography'SRID=4326;POINT(0.5 0.5)')",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"Name\":\"Spatial Place 1\"",
            "POINT(0.5 0.5) lies inside the seeded ServiceArea polygon");
    }

    /// <summary>
    /// EFCore: path-segment $filter syntax (/Entities/$filter(...)) must also translate
    /// geo.distance.  Exercises the RestierQueryBuilder.HandleFilterPathSegment change.
    /// </summary>
    [TestMethod, Ignore("Requires SQL Server CLR for geography spatial method execution (sp_configure 'clr enabled', 1).")]
    public async Task EFCore_Filter_GeoDistance_PathSegmentSyntax_TranslatesToo()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces/$filter(geo.distance(HeadquartersLocation,geography'SRID=4326;POINT(0 0)') lt 10000000)",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "path-segment $filter must use the same DI-resolved IFilterBinder as the URL-query form");
        content.Should().Contain("\"Name\":\"Spatial Place 1\"");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Negative — error handling (spec B)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mixing Geography property with a Geometry literal must return 4xx. The ODL parser's
    /// function signature matching rejects cross-genus calls at parse time before the
    /// binder ever sees the call — see the implementation note in the spec.
    /// </summary>
    [TestMethod]
    public async Task EFCore_Filter_GeoDistance_GenusMismatch_Returns400()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.distance(HeadquartersLocation,geometry'SRID=0;POINT(0 0)') lt 10000000",
            serviceCollection: _configureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeFalse(
            "cross-genus geo.distance must be rejected (ODL parser's function signature " +
            "matching enforces same-genus arguments)");
    }

    /// <summary>
    /// Unknown geo.* function names (geo.area, etc.) must be rejected with 4xx —
    /// proves the binder's default: arm forwards to AspNetCoreOData's base FilterBinder.
    /// </summary>
    [TestMethod]
    public async Task EFCore_Filter_GeoArea_UnknownFunction_Returns400()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.area(ServiceArea) gt 0",
            serviceCollection: _configureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeFalse(
            "unknown geo.* functions (not in OData v4 core) must be rejected");
    }
}
