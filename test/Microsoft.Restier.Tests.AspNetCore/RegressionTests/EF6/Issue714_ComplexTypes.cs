// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EF6;

[TestClass]
[DoNotParallelize]
public class Issue714_ComplexTypes : Issue714_ComplexTypes<ComplexTypesApiEF6>
{
    protected override Action<ODataOptions> ConfigureRoute => options =>
    {
        options.AddRestierRoute<ComplexTypesApiEF6>(WebApiConstants.RoutePrefix, routeServices =>
        {
            routeServices
                .AddEntityFrameworkServices<MarvelContext>()
                .AddSingleton<IChainedService<IModelBuilder>, ComplexTypesModelBuilder>();
        });
    };
}

public class ComplexTypesApiEF6 : MarvelApi
{
    public ComplexTypesApiEF6(MarvelContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(dbContext, model, queryHandler, submitHandler)
    {
    }

    [UnboundOperation(OperationType = OperationType.Function)]
    public LibraryCard ComplexTypeTest()
    {
        return new()
        {
            Id = Guid.NewGuid()
        };
    }
}
