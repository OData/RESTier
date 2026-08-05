// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore.MultiTenancy;

public class MultiTenantApi : EntityFrameworkApi<TenantDbContext>
{
    public MultiTenantApi(TenantDbContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(dbContext, model, queryHandler, submitHandler)
    {
    }
}
