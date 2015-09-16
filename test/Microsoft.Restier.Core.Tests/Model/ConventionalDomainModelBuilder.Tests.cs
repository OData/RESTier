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
    public class ConventionalDomainModelBuilderTests
    {
        [Fact]
        public async Task ConventionalDomainModelBuilderShouldProduceEmptyModelForEmptyDomain()
        {
            var model = await this.GetModelAsync<EmptyDomain>();
            Assert.Single(model.SchemaElements);
            Assert.Empty(model.EntityContainer.Elements);
        }

        [Fact]
        public async Task ConventionalDomainModelBuilderShouldProduceCorrectModelForBasicScenario()
        {
            var model = await this.GetModelAsync<DomainA>();
            Assert.DoesNotContain("DomainConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("DomainContext", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.NotNull(model.EntityContainer.FindEntitySet("People"));
            Assert.NotNull(model.EntityContainer.FindSingleton("Me"));
        }

        [Fact]
        public async Task ConventionalDomainModelBuilderShouldProduceCorrectModelForDerivedDomain()
        {
            var model = await this.GetModelAsync<DomainB>();
            Assert.DoesNotContain("DomainConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("DomainContext", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.NotNull(model.EntityContainer.FindEntitySet("People"));
            Assert.NotNull(model.EntityContainer.FindEntitySet("Customers"));
            Assert.NotNull(model.EntityContainer.FindSingleton("Me"));
        }

        [Fact]
        public async Task ConventionalDomainModelBuilderShouldProduceCorrectModelForOverridingProperty()
        {
            var model = await this.GetModelAsync<DomainC>();
            Assert.DoesNotContain("DomainConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("DomainContext", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.NotNull(model.EntityContainer.FindEntitySet("People"));
            Assert.Equal("Customer", model.EntityContainer.FindEntitySet("Customers").EntityType().Name);
            Assert.Equal("Customer", model.EntityContainer.FindSingleton("Me").EntityType().Name);
        }

        [Fact]
        public async Task ConventionalDomainModelBuilderShouldProduceCorrectModelForIgnoringInheritedProperty()
        {
            var model = await this.GetModelAsync<DomainD>();
            Assert.DoesNotContain("DomainConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("DomainContext", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("People", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.Equal("Customer", model.EntityContainer.FindEntitySet("Customers").EntityType().Name);
            Assert.Equal("Customer", model.EntityContainer.FindSingleton("Me").EntityType().Name);
        }

        [Fact]
        public async Task ConventionalDomainModelBuilderShouldSkipEntitySetWithUndeclaredType()
        {
            var model = await this.GetModelAsync<DomainE>();
            Assert.Equal("Person", model.EntityContainer.FindEntitySet("People").EntityType().Name);
            Assert.DoesNotContain("Orders", model.EntityContainer.Elements.Select(e => e.Name));
        }

        [Fact]
        public async Task ConventionalDomainModelBuilderShouldSkipExistingEntitySet()
        {
            var model = await this.GetModelAsync<DomainF>();
            Assert.Equal("Person", model.EntityContainer.FindEntitySet("VipCustomers").EntityType().Name);
        }

        private async Task<IEdmModel> GetModelAsync<T>() where T : BaseDomain, new()
        {
            var domain = (BaseDomain)Activator.CreateInstance<T>();
            return await Domain.GetModelAsync(domain.DomainContext);
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

    public class BaseDomain : DomainBase
    {
        public new DomainConfiguration DomainConfiguration
        {
            get { return base.DomainConfiguration; }
        }

        public new DomainContext DomainContext
        {
            get { return base.DomainContext; }
        }

        protected override DomainConfiguration CreateDomainConfiguration()
        {
            return base.CreateDomainConfiguration()
                .IgnoreProperty("DomainConfiguration")
                .IgnoreProperty("DomainContext");
        }
    }

    public class EmptyDomain : BaseDomain
    {
    }

    public class Person
    {
        public int PersonId { get; set; }
    }

    public class DomainA : BaseDomain
    {
        public IQueryable<Person> People { get; }
        public Person Me { get; }
        public IQueryable<Person> Invisible { get; }

        protected override DomainConfiguration CreateDomainConfiguration()
        {
            return base.CreateDomainConfiguration()
                .AddHookHandler<IModelBuilder>(new TestModelBuilder())
                .IgnoreProperty("Invisible");
        }
    }

    public class DomainB : DomainA
    {
        public IQueryable<Person> Customers { get; }
    }

    public class Customer
    {
        public int CustomerId { get; set; }
    }

    public class DomainC : DomainB
    {
        public new IQueryable<Customer> Customers { get; }
        public new Customer Me { get; }
    }

    public class DomainD : DomainC
    {
        protected override DomainConfiguration CreateDomainConfiguration()
        {
            return base.CreateDomainConfiguration().IgnoreProperty("People");
        }
    }

    public class Order
    {
        public int OrderId { get; set; }
    }

    public class DomainE : BaseDomain
    {
        public IQueryable<Person> People { get; }
        public IQueryable<Order> Orders { get; }

        protected override DomainConfiguration CreateDomainConfiguration()
        {
            return base.CreateDomainConfiguration()
                .AddHookHandler<IModelBuilder>(new TestModelBuilder());
        }
    }

    public class DomainF : BaseDomain
    {
        public IQueryable<Customer> VipCustomers { get; }

        protected override DomainConfiguration CreateDomainConfiguration()
        {
            return base.CreateDomainConfiguration()
                .AddHookHandler<IModelBuilder>(new TestModelBuilder());
        }
    }
}