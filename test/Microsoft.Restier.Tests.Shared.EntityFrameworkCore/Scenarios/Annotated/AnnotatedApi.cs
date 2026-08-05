// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Annotated;

public class AnnotatedApi : EntityFrameworkApi<AnnotatedContext>
{
    public AnnotatedApi(AnnotatedContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(dbContext, model, queryHandler, submitHandler)
    {
    }

    [UnboundOperation]
    [Description("Returns the count of widgets currently stored.")]
    public int CountWidgets() => DbContext.AnnotatedEntities.Count();
}
