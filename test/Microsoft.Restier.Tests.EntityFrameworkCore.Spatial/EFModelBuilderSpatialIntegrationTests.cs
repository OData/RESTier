// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Spatial
{
    [TestClass]
    public class EFModelBuilderSpatialIntegrationTests
    {
        public class Place
        {
            public int Id { get; set; }

            public NetTopologySuite.Geometries.Point Location { get; set; }
        }

        public class IntegrationContext : DbContext
        {
            public DbSet<Place> Places { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseInMemoryDatabase("ef-modelbuilder-spatial-integration");

            protected override void OnModelCreating(ModelBuilder modelBuilder)
                => modelBuilder.Entity<Place>(e => e.Property(x => x.Location).HasColumnType("geography"));
        }

        [TestMethod]
        public void EFModelBuilder_publishes_spatial_property_as_GeographyPoint()
        {
            using var ctx = new IntegrationContext();
            var providers = new ISpatialModelMetadataProvider[] { new NtsSpatialModelMetadataProvider() };
            var modelMerger = new ModelMerger();
            var builder = new EFModelBuilder<IntegrationContext>(ctx, modelMerger, new KeylessViewRegistry(), RestierNamingConvention.PascalCase, providers);

            var model = builder.GetEdmModel();

            model.Should().BeOfType<EdmModel>();

            var placeType = (IEdmEntityType)model.FindDeclaredType($"{typeof(IntegrationContext).Namespace}.Place");
            placeType.Should().NotBeNull();

            var loc = placeType.FindProperty(nameof(Place.Location));
            loc.Should().NotBeNull();
            loc.Type.Definition.FullTypeName().Should().Be("Edm.GeographyPoint");
        }

        [TestMethod]
        public void EFModelBuilder_without_spatial_providers_is_a_noop_for_non_spatial_entities()
        {
            using var ctx = new IntegrationContext();
            var modelMerger = new ModelMerger();
            var builder = new EFModelBuilder<IntegrationContext>(ctx, modelMerger, new KeylessViewRegistry());

            var model = builder.GetEdmModel();

            model.Should().BeOfType<EdmModel>();

            var placeType = (IEdmEntityType)model.FindDeclaredType($"{typeof(IntegrationContext).Namespace}.Place");
            placeType.Should().NotBeNull();
            // With no spatial providers registered, the convention is a no-op and the storage-typed property
            // is published by the underlying ODataConventionModelBuilder. We only assert the key is intact;
            // we don't pin the storage-typed property's EDM representation since that's outside the scope
            // of this change.
            placeType.FindProperty(nameof(Place.Id)).Should().NotBeNull();
        }
    }
}
