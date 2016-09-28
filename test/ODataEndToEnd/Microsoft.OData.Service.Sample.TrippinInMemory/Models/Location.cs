// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Spatial;

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Models
{
    public class Location
    {
        public string Address { get; set; }

        public City City { get; set; }
    }

    public class EventLocation : Location
    {
        public string BuildingInfo { get; set; }
    }

    public class AirportLocation : Location
    {
        // TODO: the type of field does not support serialization
        public GeographyPoint Loc { get; set; }
    }
}