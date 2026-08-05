// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFramework.Shared.Model;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Spatial
{
    [TestClass]
    public class SpatialModelConventionTests
    {
        private class City
        {
            public int Id { get; set; }

            public NetTopologySuite.Geometries.Point HeadquartersLocation { get; set; }

            [Spatial(typeof(GeometryPoint))]
            public NetTopologySuite.Geometries.Point IndoorOrigin { get; set; }
        }

        private class BadAttribute
        {
            public int Id { get; set; }

            [Spatial(typeof(string))]
            public NetTopologySuite.Geometries.Point Location { get; set; }
        }

        private class GenusMismatch
        {
            public int Id { get; set; }

            [Spatial(typeof(GeometryPoint))]
            public NetTopologySuite.Geometries.Point Location { get; set; }
        }

        private class CityContext : DbContext
        {
            public DbSet<City> Cities { get; set; }

            public DbSet<GenusMismatch> Mismatches { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseInMemoryDatabase("convention-tests");

            protected override void OnModelCreating(ModelBuilder b)
            {
                b.Entity<City>(e =>
                {
                    e.Property(x => x.HeadquartersLocation).HasColumnType("geography");
                });
                b.Entity<GenusMismatch>(e =>
                {
                    e.Property(x => x.Location).HasColumnType("geography");
                });
            }
        }

        [TestMethod]
        public void Phase1_captures_spatial_properties_with_resolved_edm_types()
        {
            using var ctx = new CityContext();
            var providers = new ISpatialModelMetadataProvider[] { new NtsSpatialModelMetadataProvider() };
            var convention = new SpatialModelConvention(providers);
            var builder = new ODataConventionModelBuilder { Namespace = "Test" };
            builder.EntitySet<City>("Cities");

            var captures = convention.CapturePhase(builder, new[] { typeof(City) }, ctx);

            captures.Should().HaveCount(2);
            captures.Should().Contain(c => c.PropertyInfo.Name == nameof(City.HeadquartersLocation) && c.ResolvedEdmType == typeof(GeographyPoint));
            captures.Should().Contain(c => c.PropertyInfo.Name == nameof(City.IndoorOrigin) && c.ResolvedEdmType == typeof(GeometryPoint));
        }

        [TestMethod]
        public void Phase1_calls_Ignore_for_storage_types()
        {
            using var ctx = new CityContext();
            var providers = new ISpatialModelMetadataProvider[] { new NtsSpatialModelMetadataProvider() };
            var convention = new SpatialModelConvention(providers);
            var builder = new ODataConventionModelBuilder { Namespace = "Test" };
            builder.EntitySet<City>("Cities");

            convention.CapturePhase(builder, new[] { typeof(City) }, ctx);

            var model = builder.GetEdmModel();
            var cityType = model.FindDeclaredType("Test.City") as IEdmStructuredType;
            cityType.Should().NotBeNull();
            cityType.DeclaredProperties.Select(p => p.Name)
                .Should().NotContain(new[] { nameof(City.HeadquartersLocation), nameof(City.IndoorOrigin) });
        }

        [TestMethod]
        public void Phase2_adds_structural_properties_with_resolved_edm_types_PascalCase()
        {
            using var ctx = new CityContext();
            var providers = new ISpatialModelMetadataProvider[] { new NtsSpatialModelMetadataProvider() };
            var convention = new SpatialModelConvention(providers);

            var builder = new ODataConventionModelBuilder { Namespace = "Test" };
            builder.EntitySet<City>("Cities");

            var captures = convention.CapturePhase(builder, new[] { typeof(City) }, ctx);
            var model = (EdmModel)builder.GetEdmModel();

            convention.AugmentPhase(model, captures, Microsoft.Restier.Core.RestierNamingConvention.PascalCase);

            var cityType = (IEdmStructuredType)model.FindDeclaredType("Test.City");
            var headquarters = cityType.FindProperty(nameof(City.HeadquartersLocation));
            headquarters.Should().NotBeNull();
            headquarters.Type.Definition.FullTypeName().Should().Be("Edm.GeographyPoint");

            var indoor = cityType.FindProperty(nameof(City.IndoorOrigin));
            indoor.Should().NotBeNull();
            indoor.Type.Definition.FullTypeName().Should().Be("Edm.GeometryPoint");
        }

        [TestMethod]
        public void Phase2_lowercases_property_names_under_LowerCamelCase()
        {
            using var ctx = new CityContext();
            var providers = new ISpatialModelMetadataProvider[] { new NtsSpatialModelMetadataProvider() };
            var convention = new SpatialModelConvention(providers);

            var builder = new ODataConventionModelBuilder { Namespace = "Test" };
            builder.EntitySet<City>("Cities");
            builder.EnableLowerCamelCase();

            var captures = convention.CapturePhase(builder, new[] { typeof(City) }, ctx);
            var model = (EdmModel)builder.GetEdmModel();

            convention.AugmentPhase(model, captures, Microsoft.Restier.Core.RestierNamingConvention.LowerCamelCase);

            var cityType = (IEdmStructuredType)model.FindDeclaredType("Test.City");
            cityType.FindProperty("headquartersLocation").Should().NotBeNull();
            cityType.FindProperty("indoorOrigin").Should().NotBeNull();
        }

        [TestMethod]
        public void Phase2_attaches_ClrPropertyInfoAnnotation_so_EdmClrPropertyMapper_resolves_original_name()
        {
            using var ctx = new CityContext();
            var providers = new ISpatialModelMetadataProvider[] { new NtsSpatialModelMetadataProvider() };
            var convention = new SpatialModelConvention(providers);

            var builder = new ODataConventionModelBuilder { Namespace = "Test" };
            builder.EntitySet<City>("Cities");
            builder.EnableLowerCamelCase();

            var captures = convention.CapturePhase(builder, new[] { typeof(City) }, ctx);
            var model = (EdmModel)builder.GetEdmModel();
            convention.AugmentPhase(model, captures, Microsoft.Restier.Core.RestierNamingConvention.LowerCamelCase);

            var cityType = (IEdmStructuredType)model.FindDeclaredType("Test.City");
            var prop = cityType.FindProperty("headquartersLocation");

            var clrName = Microsoft.Restier.AspNetCore.EdmClrPropertyMapper.GetClrPropertyName(prop, model);
            clrName.Should().Be(nameof(City.HeadquartersLocation));
        }

        [TestMethod]
        public void Spatial_attribute_with_non_Microsoft_Spatial_type_throws()
        {
            var convention = new SpatialModelConvention(new ISpatialModelMetadataProvider[] { new NtsSpatialModelMetadataProvider() });
            var builder = new ODataConventionModelBuilder { Namespace = "Test" };
            builder.EntitySet<BadAttribute>("Bads");

            var act = () => convention.CapturePhase(builder, new[] { typeof(BadAttribute) }, providerContext: null);

            act.Should().Throw<Microsoft.Restier.Core.EdmModelValidationException>()
                .WithMessage("*not a Microsoft.Spatial primitive type*");
        }

        [TestMethod]
        public void Spatial_attribute_genus_mismatch_throws()
        {
            using var ctx = new CityContext();
            var convention = new SpatialModelConvention(new ISpatialModelMetadataProvider[] { new NtsSpatialModelMetadataProvider() });
            var builder = new ODataConventionModelBuilder { Namespace = "Test" };
            builder.EntitySet<GenusMismatch>("Mismatches");

            var act = () => convention.CapturePhase(builder, new[] { typeof(GenusMismatch) }, ctx);

            act.Should().Throw<Microsoft.Restier.Core.EdmModelValidationException>()
                .WithMessage("*genus*");
        }
    }
}
