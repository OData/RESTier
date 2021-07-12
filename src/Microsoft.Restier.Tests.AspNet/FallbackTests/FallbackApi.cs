// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using System.Linq.Expressions;
using Microsoft.Restier.Core.Model;

#if NETCORE3 || NETCOREAPP3_1_OR_GREATER
using Microsoft.Restier.AspNetCore.Model;

namespace Microsoft.Restier.Tests.AspNetCore.FallbackTests

#else
using Microsoft.Restier.AspNet.Model;

namespace Microsoft.Restier.Tests.AspNet.FallbackTests
#endif

{

    public class FallbackApi : ApiBase
    {

        [Resource]
        public IQueryable<Order> PreservedOrders => this.GetQueryableSource<Order>("Orders").Where(o => o.Id > 123);

        public FallbackApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

    }

    internal class FallbackQueryExpressionSourcer : IQueryExpressionSourcer
    {
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
        public bool TryGetRelevantType(ModelContext context, string name, out Type relevantType)
        {
            relevantType = name == "Person" ? typeof(Person) : typeof(Order);

            return true;
        }

        public bool TryGetRelevantType(ModelContext context, string namespaceName, string name, out Type relevantType) => TryGetRelevantType(context, name, out relevantType);
    }

}