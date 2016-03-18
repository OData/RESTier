using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Xunit;

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
            Assert.Equal("VipCustomer", model.EntityContainer.FindEntitySet("VipCustomers").EntityType().Name);
        }

        [Fact]
        public async Task ConventionBasedApiModelBuilderShouldCorrectlyAddBindingsForCollectionNavigationProperty()
        {
            // In this case, only one entity set People has entity type Person.
            // Bindings for collection navigation property Customer.Friends should be added.
            // Bindings for singleton navigation property Customer.BestFriend should be added.
            var model = await this.GetModelAsync<ApiC>();
            var bindings = model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings.ToArray();
            Assert.Equal("Friends", bindings[0].NavigationProperty.Name);
            Assert.Equal("People", bindings[0].Target.Name);
            Assert.Equal("BestFriend", bindings[1].NavigationProperty.Name);
            Assert.Equal("People", bindings[1].Target.Name);
            bindings = model.EntityContainer.FindSingleton("Me").NavigationPropertyBindings.ToArray();
            Assert.Equal("Friends", bindings[0].NavigationProperty.Name);
            Assert.Equal("People", bindings[0].Target.Name);
            Assert.Equal("BestFriend", bindings[1].NavigationProperty.Name);
            Assert.Equal("People", bindings[1].Target.Name);
        }

        [Fact]
        public async Task ConventionBasedApiModelBuilderShouldCorrectlyAddBindingsForSingletonNavigationProperty()
        {
            // In this case, only one singleton Me has entity type Person.
            // Bindings for collection navigation property Customer.Friends should NOT be added.
            // Bindings for singleton navigation property Customer.BestFriend should be added.
            var model = await this.GetModelAsync<ApiH>();
            var binding = model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings.Single();
            Assert.Equal("BestFriend", binding.NavigationProperty.Name);
            Assert.Equal("Me", binding.Target.Name);
            binding = model.EntityContainer.FindSingleton("Me2").NavigationPropertyBindings.Single();
            Assert.Equal("BestFriend", binding.NavigationProperty.Name);
            Assert.Equal("Me", binding.Target.Name);
        }

        [Fact]
        public async Task ConventionBasedApiModelBuilderShouldNotAddAmbiguousNavigationPropertyBindings()
        {
            // In this case, two entity sets Employees and People have entity type Person.
            // Bindings for collection navigation property Customer.Friends should NOT be added.
            // Bindings for singleton navigation property Customer.BestFriend should NOT be added.
            var model = await this.GetModelAsync<ApiG>();
            Assert.Empty(model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings);
            Assert.Empty(model.EntityContainer.FindSingleton("Me").NavigationPropertyBindings);
        }

        private async Task<IEdmModel> GetModelAsync<T>() where T : BaseApi, new()
        {
            var api = (BaseApi)Activator.CreateInstance<T>();
            return await api.Context.GetModelAsync();
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
            customerType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Friends",
                Target = personType,
                TargetMultiplicity = EdmMultiplicity.Many
            });
            customerType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "BestFriend",
                Target = personType,
                TargetMultiplicity = EdmMultiplicity.One
            });
            model.AddElement(customerType);
            var vipCustomerType = new EdmEntityType(ns, "VipCustomer", customerType);
            model.AddElement(vipCustomerType);
            var container = new EdmEntityContainer(ns, "DefaultContainer");
            container.AddEntitySet("VipCustomers", vipCustomerType);
            model.AddElement(container);
            return Task.FromResult<IEdmModel>(model);
        }
    }

    public class BaseApi : ApiBase
    {
        public ApiConfiguration ApiConfiguration
        {
            get { return base.Configuration; }
        }

        protected override ApiConfiguration CreateApiConfiguration(IServiceCollection services)
        {
            return base.CreateApiConfiguration(services)
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

        protected override ApiConfiguration CreateApiConfiguration(IServiceCollection services)
        {
            return base.CreateApiConfiguration(services)
                .IgnoreProperty("Invisible");
        }

        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .CutoffPrevious<IModelBuilder>(new TestModelBuilder());
        }
    }

    public class ApiB : ApiA
    {
        public IQueryable<Person> Customers { get; set; }
    }

    public class Customer
    {
        public int CustomerId { get; set; }
        public ICollection<Person> Friends { get; set; }
        public Person BestFriend { get; set; }
    }

    public class VipCustomer : Customer
    {
    }

    public class ApiC : ApiB
    {
        public new IQueryable<Customer> Customers { get; set; }
        public new Customer Me { get; set; }
    }

    public class ApiD : ApiC
    {
        protected override ApiConfiguration CreateApiConfiguration(IServiceCollection services)
        {
            return base.CreateApiConfiguration(services).IgnoreProperty("People");
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

        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .CutoffPrevious<IModelBuilder>(new TestModelBuilder());
        }
    }

    public class ApiF : BaseApi
    {
        public IQueryable<Customer> VipCustomers { get; set; }

        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .CutoffPrevious<IModelBuilder>(new TestModelBuilder());
        }
    }

    public class ApiG : ApiC
    {
        public IQueryable<Person> Employees { get; set; }
    }

    public class ApiH : BaseApi
    {
        public Person Me { get; set; }
        public IQueryable<Customer> Customers { get; set; }
        public Customer Me2 { get; set; }

        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .CutoffPrevious<IModelBuilder>(new TestModelBuilder());
        }
    }
}