// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core.Spatial
{
    /// <summary>
    /// Identifies whether a spatial property uses geodesic (Geography) or planar (Geometry) coordinates.
    /// </summary>
    public enum SpatialGenus
    {
        /// <summary>Geodesic / curved-earth coordinates (latitude / longitude).</summary>
        Geography,

        /// <summary>Planar / cartesian coordinates (X / Y in some projection).</summary>
        Geometry,
    }
}
