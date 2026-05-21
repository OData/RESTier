// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Microsoft.Restier.Samples.Postgres.AspNetCore.Models
{
    public partial class RestierTestContext
    {
        private static readonly Guid AdminTypeId = new("a1b2c3d4-0001-0001-0001-000000000001");
        private static readonly Guid EditorTypeId = new("a1b2c3d4-0001-0001-0001-000000000002");
        private static readonly Guid ViewerTypeId = new("a1b2c3d4-0001-0001-0001-000000000003");

        private static Point CreatePoint(double longitude, double latitude)
        {
            var factory = new GeometryFactory(new PrecisionModel(), 4326);
            return factory.CreatePoint(new Coordinate(longitude, latitude));
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            // Keyless view: HasNoKey().ToView wires the read path; the underlying
            // database view itself is created by the AddUsersByTypeView migration.
            modelBuilder.Entity<UsersByType>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("UsersByType");
            });

            modelBuilder.Entity<UserType>().HasData(
                new UserType { Id = AdminTypeId, DisplayName = "Administrator", IsActive = true, DateCreated = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserType { Id = EditorTypeId, DisplayName = "Editor", IsActive = true, DateCreated = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserType { Id = ViewerTypeId, DisplayName = "Viewer", IsActive = true, DateCreated = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            modelBuilder.Entity<User>().HasData(
                new User { Id = new Guid("b2c3d4e5-0002-0002-0002-000000000001"), EmailAddress = "admin@example.com", UserTypeId = AdminTypeId, HomeLocation = CreatePoint(4.9041, 52.3676) },
                new User { Id = new Guid("b2c3d4e5-0002-0002-0002-000000000002"), EmailAddress = "editor@example.com", UserTypeId = EditorTypeId },
                new User { Id = new Guid("b2c3d4e5-0002-0002-0002-000000000003"), EmailAddress = "viewer@example.com", UserTypeId = ViewerTypeId },
                new User { Id = new Guid("b2c3d4e5-0002-0002-0002-000000000004"), EmailAddress = "another.admin@example.com", UserTypeId = AdminTypeId }
            );
        }
    }
}
