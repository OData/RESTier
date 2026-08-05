// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Asp.Versioning;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore
{

    [ApiVersion("2.0")]
    public class NorthwindApiV2 : EntityFrameworkApi<NorthwindContextV2>
    {

        public NorthwindApiV2(NorthwindContextV2 dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

    }

}
