// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Models
{
    public class PlanItem
    {
        public int PlanItemId { get; set; }

        public string ConfirmationCode { get; set; }

        public DateTimeOffset StartsAt { get; set; }

        public DateTimeOffset EndsAt { get; set; }

        public TimeSpan Duration { get; set; }

        public virtual object Clone()
        {
            var newPlan = new PlanItem()
            {
                ConfirmationCode = this.ConfirmationCode,
                Duration = this.Duration,
                EndsAt = this.EndsAt,
                PlanItemId = this.PlanItemId,
                StartsAt = this.StartsAt
            };

            return newPlan;
        }
    }
}