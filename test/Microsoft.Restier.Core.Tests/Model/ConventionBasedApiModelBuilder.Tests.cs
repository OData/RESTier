using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Xunit;
using Microsoft.Restier.Core.Model;
using System.Threading;
using Microsoft.OData.Edm.Library;

namespace Microsoft.Restier.Core.Tests.Model
{
    public class ConventionBasedApiModelBuilderTests
    {
        [Fact]
        public async Task ConventionBasedApiModelBuilderShouldProduceEmptyModelForEmptyApi()
        {
            var model = await this.GetModelAsync<EmptyApi>();
            Assert.Single(model.SchemaElements);
            Assert.Empty(model.EntityContainer.Elements);
        }

        [Fact]
        public async Task ConventionBasedApiModelBuilderShouldProduceCorrectModelForBasicScenario()
        {
            var model = await this.GetModelAsync<ApiA>();
            Assert.DoesNotContain("ApiConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("ApiContext", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.NotNull(model.EntityContainer.FindEntitySet("People"));
            Assert.NotNull(model.EntityContainer.FindSingleton("Me"));
        }

        [Fact]
        public async Task ConventionBasedApiModelBuilderShouldProduceCorrectModelForDerivedApi()
        {
            var model = await this.GetModelAsync<ApiB>();
            Assert.DoesNotContain("ApiConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("ApiContext", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.NotNull(model.EntityContainer.FindEntitySet("People"));
            Assert.NotNull(model.EntityContainer.FindEntitySet("Customers"));
            Assert.NotNull(model.EntityContainer.FindSingleton("Me"));
        }

        [Fact]
        public async Task ConventionBasedApiModelBuilderShouldProduceCorrectModelForOverridingProperty()
        {
            var model = await this.GetModelAsync<ApiC>();
            Assert.DoesNotContain("ApiConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("ApiContext", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.NotNull(model.EntityContainer.FindEntitySet("People"));
            Assert.Equal("Customer", model.EntityContainer.FindEntitySet("Customers").EntityType().Name);
            Assert.Equal("Customer", model.EntityContainer.FindSingleton("Me").EntityType().Name);
        }

        [Fact]
        public async Task ConventionBasedApiModelBuilderShouldProduceCorrectModelForIgnoringInheritedProperty()
        {
            var model = await this.GetModelAsync<ApiD>();
            Assert.DoesNotContain("ApiConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("ApiContext", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("People", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.Equal("Customer", model.EntityContainer.FindEntitySet("Customers").EntityType().Name);
            Assert.Equal("Customer", model.EntityContainer.FindSingleton("Me").EntityType().Name);
        }

        [Fact]
        public async Task ConventionBasedApiModelBuilderShouldSkipEntitySetWithUndeclaredType()
        {
            var model = await this.GetModelAsync<ApiE>();
            Assert.Equal("Person", model.EntityContainer.FindEntitySet("People").EntityType().Name);
            Assert.DoesNotContain("Orders", model.EntityContainer.Elements.Select(e => e.Name));
        }

        [Fact]
        public async Task ConventionBasedApiModelBuilderShouldSkipExistingEntitySet()
        {
            var model = await this.GetModelAsync<ApiF>();
            Assert.Equal("Person", model.EntityContainer.FindEntitySet("VipCustomers").EntityType().Name);
        }

        private async Task<IEdmModel> GetModelAsync<T>() where T : BaseApi, new()
        {
            var api = (BaseApi)Activator.CreateInstance<T>();
            return await Api.GetModelAsync(api.ApiContext);
        }
    }

    public class TestModelBuilder : IModelBuilder
    {
        public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            var model = new EdmModel();
            var ns = typeof(Person).Namespace;
            var personType = new EdmEntityType(ns, "Person");
            personType.AddKeys(personType.AddStructuralProperty("PersonId", EdmPrimitiveTypeKind.Int32));
            model.AddElement(personType);
            var customerType = new EdmEntityType(ns, "Customer");
            customerType.AddKeys(customerType.AddStructuralProperty("CustomerId", EdmPrimitiveTypeKind.Int32));
            model.AddElement(customerType);
            var container = new EdmEntityContainer(ns, "DefaultContainer");
            container.AddEntitySet("VipCustomers", personType);
            model.AddElement(container);
            return Task.FromResult<IEdmModel>(model);
        }
    }

    public class BaseApi : ApiBase
    {
        public new ApiConfiguration ApiConfiguration
        {
            get { return base.ApiConfiguration; }
        }

        public new ApiContext ApiContext
        {
            get { return base.ApiContext; }
        }

        protected override ApiConfiguration CreateApiConfiguration()
        {
            return base.CreateApiConfiguration()
                .IgnoreProperty("ApiConfiguration")
                .IgnoreProperty("ApiContext");
        }
    }

    public class EmptyApi : BaseApi
    {
    }

    public class Person
    {
        public int PersonId { get; set; }
    }

    public class ApiA : BaseApi
    {
        public IQueryable<Person> People { get; set; }
        public Person Me { get; set; }
        public IQueryable<Person> Invisible { get; set; }

        protected override ApiConfiguration CreateApiConfiguration()
        {
            return base.CreateApiConfiguration()
                .AddHookHandler<IModelBuilder>(new TestModelBuilder())
                .IgnoreProperty("Invisible");
        }
    }

    public class ApiB : ApiA
    {
        public IQueryable<Person> Customers { get; set; }
    }

    public class Customer
    {
        public int CustomerId { get; set; }
    }

    public class ApiC : ApiB
    {
        public new IQueryable<Customer> Customers { get; set; }
        public new Customer Me { get; set; }
    }

    public class ApiD : ApiC
    {
        protected override ApiConfiguration CreateApiConfiguration()
        {
            return base.CreateApiConfiguration().IgnoreProperty("People");
        }
    }

    public class Order
    {
        public int OrderId { get; set; }
    }

    public class ApiE : BaseApi
    {
        public IQueryable<Person> People { get; set; }
        public IQueryable<Order> Orders { get; set; }

        protected override ApiConfiguration CreateApiConfiguration()
        {
            return base.CreateApiConfiguration()
                .AddHookHandler<IModelBuilder>(new TestModelBuilder());
        }
    }

    public class ApiF : BaseApi
    {
        public IQueryable<Customer> VipCustomers { get; set; }

        protected override ApiConfiguration CreateApiConfiguration()
        {
            return base.CreateApiConfiguration()
                .AddHookHandler<IModelBuilder>(new TestModelBuilder());
        }
    }
}