// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Models
{
    public class Event : PlanItem
    {
        public EventLocation OccursAt { get; set; }

        public string Description { get; set; }

        public override object Clone()
        {
            var newPlan = new Event()
            {
                ConfirmationCode = this.ConfirmationCode,
                Duration = this.Duration,
                EndsAt = this.EndsAt,
                PlanItemId = this.PlanItemId,
                StartsAt = this.StartsAt,
                Description = this.Description,
                OccursAt = new EventLocation()
                {
                    Address = this.OccursAt.Address,
                    BuildingInfo = this.OccursAt.BuildingInfo,
                    City = new City()
                    {
                        CountryRegion = this.OccursAt.City.CountryRegion,
                        Name = this.OccursAt.City.Name,
                        Region = this.OccursAt.City.Region,
                    }
                },
            };

            return newPlan;
        }
    }
}