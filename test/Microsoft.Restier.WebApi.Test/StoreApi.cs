using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.WebApi.Test
{
    internal static class StoreModel
    {
        public static EdmModel Model { get; private set; }

        public static IEdmEntityType Product { get; private set; }

        static StoreModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.Namespace = "Microsoft.Restier.WebApi.Test";
            builder.EntitySet<Product>("Products");
            builder.Function("GetBestProduct").ReturnsFromEntitySet<Product>("Products");
            builder.Action("RemoveWorstProduct").ReturnsFromEntitySet<Product>("Products");
            Model = (EdmModel)builder.GetEdmModel();
            Product = (IEdmEntityType)Model.FindType("Microsoft.Restier.WebApi.Test.Product");
        }
    }

    internal class StoreApi : ApiBase
    {
        protected override ApiBuilder ConfigureApiBuilder(ApiBuilder builder)
        {
            builder = base.ConfigureApiBuilder(builder);
            builder.AddHookHandler<IModelBuilder>(new TestModelProducer(StoreModel.Model));
            builder.AddHookHandler<IModelMapper>(new TestModelMapper());
            builder.AddHookHandler<IQueryExpressionSourcer>(new TestQueryExpressionSourcer());
            builder.AddHookHandler<IChangeSetPreparer>(new TestChangeSetPreparer());
            builder.AddHookHandler<ISubmitExecutor>(new TestSubmitExecutor());
            return builder;
        }
    }

    class Product
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [Required]
        public Address Addr { get; set; }

        public Address Addr2 { get; set; }

        public Address Addr3 { get; set; }
    }

    class Address
    {
        public int Zip { get; set; }
    }

    class TestModelMapper : IModelMapper
    {
        public bool TryGetRelevantType(ApiContext context, string name, out Type relevantType)
        {
            relevantType = typeof(Product);
            return true;
        }

        public bool TryGetRelevantType(ApiContext context, string namespaceName, string name, out Type relevantType)
        {
            relevantType = typeof(Product);
            return true;
        }
    }

    class TestModelProducer : IModelBuilder
    {
        private EdmModel model;

        public TestModelProducer(EdmModel model)
        {
            this.model = model;
        }

        public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEdmModel>(model);
        }
    }

    class TestQueryExpressionSourcer : IQueryExpressionSourcer
    {
        public Expression Source(QueryExpressionContext context, bool embedded)
        {
            var a = new[] { new Product
            {
                Id = 1,
                Addr = new Address { Zip = 0001 },
                Addr2= new Address { Zip = 0002 }
            } };

            if (!embedded)
            {
                if (context.VisitedNode.ToString() == "Source(\"Products\", null)")
                {
                    return Expression.Constant(a.AsQueryable());
                }
            }

            return context.VisitedNode;
        }
    }

    class TestChangeSetPreparer : IChangeSetPreparer
    {
        public Task PrepareAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            var changeSetEntry = context.ChangeSet.Entries.Single();

            var dataModificationEntry = changeSetEntry as DataModificationEntry;
            if (dataModificationEntry != null)
            {
                dataModificationEntry.Entity = new Product()
                {
                    Name = "var1",
                    Addr = new Address()
                    {
                        Zip = 330
                    }
                };
            }

            return Task.FromResult<object>(null);
        }
    }

    class TestSubmitExecutor : ISubmitExecutor
    {
        public Task<SubmitResult> ExecuteSubmitAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SubmitResult(context.ChangeSet));
        }
    }
}
