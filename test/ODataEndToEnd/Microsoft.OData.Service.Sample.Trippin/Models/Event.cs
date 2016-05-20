// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    public class Event
    {
        public int Id { get; set; }
        public Location OccursAt { get; set; }
        public string Description { get; set; }
    }
}