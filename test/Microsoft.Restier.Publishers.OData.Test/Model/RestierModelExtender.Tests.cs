using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Publishers.OData.Model;
using Xunit;

namespace Microsoft.Restier.Publishers.OData.Test.Model
{
    public class RestierModelExtenderTests
    {
        [Fact]
        public async Task ApiModelBuilderShouldProduceEmptyModelForEmptyApi()
        {
            var model = await this.GetModelAsync<EmptyApi>();
            Assert.Single(model.SchemaElements);
            Assert.Empty(model.EntityContainer.Elements);
        }

        [Fact]
        public async Task ApiModelBuilderShouldProduceCorrectModelForBasicScenario()
        {
            var model = await this.GetModelAsync<ApiA>();
            Assert.DoesNotContain("ApiConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.NotNull(model.EntityContainer.FindEntitySet("People"));
            Assert.NotNull(model.EntityContainer.FindSingleton("Me"));
        }

        [Fact]
        public async Task ApiModelBuilderShouldProduceCorrectModelForDerivedApi()
        {
            var model = await this.GetModelAsync<ApiB>();
            Assert.DoesNotContain("ApiConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.NotNull(model.EntityContainer.FindEntitySet("People"));
            Assert.NotNull(model.EntityContainer.FindEntitySet("Customers"));
            Assert.NotNull(model.EntityContainer.FindSingleton("Me"));
        }

        [Fact]
        public async Task ApiModelBuilderShouldProduceCorrectModelForOverridingProperty()
        {
            var model = await this.GetModelAsync<ApiC>();
            Assert.DoesNotContain("ApiConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.NotNull(model.EntityContainer.FindEntitySet("People"));
            Assert.Equal("Customer", model.EntityContainer.FindEntitySet("Customers").EntityType().Name);
            Assert.Equal("Customer", model.EntityContainer.FindSingleton("Me").EntityType().Name);
        }

        [Fact]
        public async Task ApiModelBuilderShouldProduceCorrectModelForIgnoringInheritedProperty()
        {
            var model = await this.GetModelAsync<ApiD>();
            Assert.DoesNotContain("ApiConfiguration", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.DoesNotContain("Invisible", model.EntityContainer.Elements.Select(e => e.Name));
            Assert.Equal("Customer", model.EntityContainer.FindEntitySet("Customers").EntityType().Name);
            Assert.Equal("Customer", model.EntityContainer.FindSingleton("Me").EntityType().Name);
        }

        [Fact]
        public async Task ApiModelBuilderShouldSkipEntitySetWithUndeclaredType()
        {
            var model = await this.GetModelAsync<ApiE>();
            Assert.Equal("Person", model.EntityContainer.FindEntitySet("People").EntityType().Name);
            Assert.DoesNotContain("Orders", model.EntityContainer.Elements.Select(e => e.Name));
        }

        [Fact]
        public async Task ApiModelBuilderShouldSkipExistingEntitySet()
        {
            var model = await this.GetModelAsync<ApiF>();
            Assert.Equal("VipCustomer", model.EntityContainer.FindEntitySet("VipCustomers").EntityType().Name);
        }

        [Fact]
        public async Task ApiModelBuilderShouldCorrectlyAddBindingsForCollectionNavigationProperty()
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
        public async Task ApiModelBuilderShouldCorrectlyAddBindingsForSingletonNavigationProperty()
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
        public async Task ApiModelBuilderShouldNotAddAmbiguousNavigationPropertyBindings()
        {
            // In this case, two entity sets Employees and People have entity type Person.
            // Bindings for collection navigation property Customer.Friends should NOT be added.
            // Bindings for singleton navigation property Customer.BestFriend should NOT be added.
            var model = await GetModelAsync<ApiG>();
            Assert.Empty(model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings);
            Assert.Empty(model.EntityContainer.FindSingleton("Me").NavigationPropertyBindings);
        }

        private async Task<IEdmModel> GetModelAsync<T>() where T : BaseApi
        {
            HttpConfiguration config = new HttpConfiguration();
            await config.MapRestierRoute<T>(
                    "test", "api/test",null);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            request.SetConfiguration(config);
            var api = request.CreateRequestContainer("test").GetService<ApiBase>();
            return await api.GetModelAsync();
        }
    }

    public class TestModelBuilder : IModelBuilder
    {
        public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
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
        public BaseApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public class EmptyApi : BaseApi
    {
        public EmptyApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public class Person
    {
        public int PersonId { get; set; }
    }

    public class ApiA : BaseApi
    {
        [Resource]
        public IQueryable<Person> People { get; set; }
        [Resource]
        public Person Me { get; set; }
        public IQueryable<Person> Invisible { get; set; }

        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            services.AddService<IModelBuilder>((sp, next) => new TestModelBuilder());
            return BaseApi.ConfigureApi(apiType, services);
        }

        public ApiA(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public class ApiB : ApiA
    {
        [Resource]
        public IQueryable<Person> Customers { get; set; }

        public ApiB(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
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
        [Resource]
        public new IQueryable<Customer> Customers { get; set; }
        [Resource]
        public new Customer Me { get; set; }

        public ApiC(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public class ApiD : ApiC
    {
        public ApiD(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public class Order
    {
        public int OrderId { get; set; }
    }

    public class ApiE : BaseApi
    {
        [Resource]
        public IQueryable<Person> People { get; set; }
        [Resource]
        public IQueryable<Order> Orders { get; set; }

        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            services.AddService<IModelBuilder>((sp, next) => new TestModelBuilder());
            return BaseApi.ConfigureApi(apiType, services);
        }

        public ApiE(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public class ApiF : BaseApi
    {
        public IQueryable<Customer> VipCustomers { get; set; }

        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            services.AddService<IModelBuilder>((sp, next) => new TestModelBuilder());
            return BaseApi.ConfigureApi(apiType, services);
        }

        public ApiF(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public class ApiG : ApiC
    {
        [Resource]
        public IQueryable<Person> Employees { get; set; }

        public ApiG(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public class ApiH : BaseApi
    {
        [Resource]
        public Person Me { get; set; }
        [Resource]
        public IQueryable<Customer> Customers { get; set; }
        [Resource]
        public Customer Me2 { get; set; }

        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            services.AddService<IModelBuilder>((sp, next) => new TestModelBuilder());
            return BaseApi.ConfigureApi(apiType, services);
        }

        public ApiH(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}