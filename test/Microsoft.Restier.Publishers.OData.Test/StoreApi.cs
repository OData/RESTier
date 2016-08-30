using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Publishers.OData.Test
{
    internal static class StoreModel
    {
        public static EdmModel Model { get; private set; }

        public static IEdmEntityType Product { get; private set; }

        static StoreModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.Namespace = "Microsoft.Restier.Publishers.OData.Test";
            builder.EntitySet<Product>("Products");
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Store>("Stores");
            builder.Function("GetBestProduct").ReturnsFromEntitySet<Product>("Products");
            builder.Action("RemoveWorstProduct").ReturnsFromEntitySet<Product>("Products");
            Model = (EdmModel)builder.GetEdmModel();
            Product = (IEdmEntityType)Model.FindType("Microsoft.Restier.Publishers.OData.Test.Product");
        }
    }

    internal class StoreApi : ApiBase
    {
        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            services = ApiBase.ConfigureApi(apiType, services);
            services.AddService<IModelBuilder>((sp, next) => new TestModelProducer(StoreModel.Model));
            services.AddService<IModelMapper>((sp, next) => new TestModelMapper());
            services.AddService<IQueryExpressionSourcer>((sp, next) => new TestQueryExpressionSourcer());
            services.AddService<IChangeSetInitializer>((sp, next) => new TestChangeSetInitializer());
            services.AddService<ISubmitExecutor>((sp, next) => new TestSubmitExecutor());
            return services;
        }

        public StoreApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
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

    class Customer
    {
        public short Id { get; set; }
    }

    class Store
    {
        public long Id { get; set; }
    }

    class Address
    {
        public int Zip { get; set; }
    }

    class TestModelMapper : IModelMapper
    {
        public bool TryGetRelevantType(ModelContext context, string name, out Type relevantType)
        {
            if (name == "Products")
            {
                relevantType = typeof(Product);
            }
            else if (name == "Customers")
            {
                relevantType = typeof(Customer);
            }
            else if (name == "Stores")
            {
                relevantType = typeof(Store);
            }
            else
            {
                relevantType = null;
            }
            
            return true;
        }

        public bool TryGetRelevantType(ModelContext context, string namespaceName, string name, out Type relevantType)
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

        public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEdmModel>(model);
        }
    }

    class TestQueryExpressionSourcer : IQueryExpressionSourcer
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

    class TestChangeSetInitializer : IChangeSetInitializer
    {
        public Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            var changeSetEntry = context.ChangeSet.Entries.Single();

            var dataModificationEntry = changeSetEntry as DataModificationItem;
            if (dataModificationEntry != null)
            {
                dataModificationEntry.Resource = new Product()
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
