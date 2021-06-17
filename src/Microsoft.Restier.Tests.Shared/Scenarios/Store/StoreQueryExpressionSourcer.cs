// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Tests.Shared
{
    internal class StoreQueryExpressionSourcer : IQueryExpressionSourcer
    {
        public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
        {
            var a = new[] {
                new Product
                {
                    Id = 1,
                    Name = "Widget 1",
                    IsActive = false,
                    Addr = new Address { Zip = 0001 },
                    Addr2= new Address { Zip = 0002 }
                },
                new Product
                {
                    Id = 2,
                    Name = "Widget 2",
                    IsActive = true,
                    Addr = new Address { Zip = 0001 },
                    Addr2= new Address { Zip = 0002 }
                },
            };

            var b = new[] { new Customer
            {
                Id = 1,
                FavoriteProducts = new List<Product>
                {
                    new Product { Id = 1, Name = "Widget 1", IsActive = true },
                    new Product { Id = 2, Name = "Widget 2", IsActive = true },
                    new Product { Id = 3, Name = "Widget 3", IsActive = true },
                    new Product { Id = 3, Name = "Widget 4", IsActive = false },
                }
            } };

            var c = new[] { new Store
            {
                Id = 1,
            } };

            if (!embedded)
            {
                if (context.VisitedNode.ToString() == "GetQueryableSource(\"Products\", null)")
                {
                    return Expression.Constant(a.AsQueryable());
                }

                if (context.VisitedNode.ToString() == "GetQueryableSource(\"Customers\", null)")
                {
                    return Expression.Constant(b.AsQueryable());
                }

                if (context.VisitedNode.ToString() == "GetQueryableSource(\"Stores\", null)")
                {
                    return Expression.Constant(c.AsQueryable());
                }
            }

            return context.VisitedNode;
        }
    }
}
