// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Microsoft.Restier.Tests.AspNetCore.FallbackTests;

public static class FallbackModel
{
    public static EdmModel Model { get; private set; }

    static FallbackModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<Order>("Orders");
        builder.EntitySet<Person>("People");
        Model = (EdmModel)builder.GetEdmModel();
    }
}

public class Person
{
    public int Id { get; set; }

    public IEnumerable<Order> Orders { get; set; }
}

public class Order
{
    public int Id { get; set; }
}
