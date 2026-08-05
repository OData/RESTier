// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EF6

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    /// <summary>
    /// EF6 spatial test entity. Persists DbGeography and DbGeometry columns mapped natively by EF6's
    /// SQL Server provider. Used by spatial round-trip integration tests.
    /// </summary>
    public class SpatialPlace
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public System.Data.Entity.Spatial.DbGeography HeadquartersLocation { get; set; }

        [Microsoft.Restier.Core.Spatial.Spatial(typeof(Microsoft.Spatial.GeographyPolygon))]
        public System.Data.Entity.Spatial.DbGeography ServiceArea { get; set; }

        public System.Data.Entity.Spatial.DbGeometry FloorPlan { get; set; }

        public System.Data.Entity.Spatial.DbGeography RouteLine { get; set; }
    }
}

#endif

#if EFCore

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    /// <summary>
    /// EFCore spatial test entity. Persists NetTopologySuite geometry columns via the
    /// Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite provider. Used by spatial
    /// round-trip integration tests.
    /// </summary>
    public class SpatialPlace
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public NetTopologySuite.Geometries.Point HeadquartersLocation { get; set; }

        public NetTopologySuite.Geometries.Polygon ServiceArea { get; set; }

        [Microsoft.Restier.Core.Spatial.Spatial(typeof(Microsoft.Spatial.GeographyPoint))]
        public NetTopologySuite.Geometries.Point IndoorOrigin { get; set; }

        public NetTopologySuite.Geometries.LineString RouteLine { get; set; }
    }
}

#endif
