using System.Linq;
using System.Linq.Expressions;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Tests.Shared
{
    internal class StoreQueryExpressionSourcer : IQueryExpressionSourcer
    {
        public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
        {
            var a = new[] { new Product
            {
                Id = 1,
                Addr = new Address { Zip = 0001 },
                Addr2= new Address { Zip = 0002 }
            } };

            var b = new[] { new Customer
            {
                Id = 1,
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
