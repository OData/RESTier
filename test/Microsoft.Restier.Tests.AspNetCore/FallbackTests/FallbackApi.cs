// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Tests.AspNetCore.FallbackTests;

public class FallbackApi : ApiBase
{
    [Resource]
    public IQueryable<Order> PreservedOrders => this.GetQueryableSource<Order>("Orders").Where(o => o.Id > 123);

    public FallbackApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }
}

internal class FallbackQueryExpressionSourcer : IQueryExpressionSourcer
{
    public IQueryExpressionSourcer Inner { get; set; }

    public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
    {
        var orders = new[]
        {
            new Order {Id = 234}
        };

        if (!embedded)
        {
            if (context.VisitedNode.ToString().StartsWith("GetQueryableSource(\"Orders\"", StringComparison.CurrentCulture))
            {
                return Expression.Constant(orders.AsQueryable());
            }
        }

        return context.VisitedNode;
    }
}

internal class FallbackModelMapper : IModelMapper
{
    public IModelMapper Inner { get; set; }

    public bool TryGetRelevantType(InvocationContext context, string name, out Type relevantType)
    {
        relevantType = name == "Person" ? typeof(Person) : typeof(Order);

        return true;
    }

    public bool TryGetRelevantType(InvocationContext context, string namespaceName, string name, out Type relevantType) => TryGetRelevantType(context, name, out relevantType);
}
