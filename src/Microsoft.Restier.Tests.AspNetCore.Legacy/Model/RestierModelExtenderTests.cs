// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET6_0_OR_GREATER
using Microsoft.Restier.AspNetCore.Model;

namespace Microsoft.Restier.Tests.AspNetCore.Model
#else
using Microsoft.Restier.AspNet.Model;

namespace Microsoft.Restier.Tests.AspNet.Model
#endif
{

#if NET6_0_OR_GREATER

    [TestClass]
    [TestCategory("Endpoint Routing")]
    public class RestierModelExtenderTests_EndpointRouting : RestierModelExtenderTests
    {
        public RestierModelExtenderTests_EndpointRouting() : base(true)
        {
        }
    }

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class RestierModelExtenderTests_LegacyRouting : RestierModelExtenderTests
    {
        public RestierModelExtenderTests_LegacyRouting() : base(false)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public abstract class RestierModelExtenderTests : RestierTestBase<ApiBase>
    {

        public RestierModelExtenderTests(bool useEndpointRouting) : base(useEndpointRouting)
        {
            //AddRestierAction = builder =>
            //{
            //    builder.AddRestierApi<LibraryApi>(services => services.AddEntityFrameworkServices<LibraryContext>());
            //};
            //MapRestierAction = routeBuilder =>
            //{
            //    routeBuilder.MapApiRoute<LibraryApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix, false);
            //};
        }

        //[TestInitialize]
        //public void ClaimsTestSetup() => TestSetup();

#else

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class RestierModelExtenderTests : RestierTestBase
    {

#endif

            void Api<TApi>(IServiceCollection services) where TApi : ApiBase
            {
                di(services);
            }

            void di(IServiceCollection services)
            {
                diEmpty(services);
                services.AddChainedService<IModelBuilder>((sp, next) => new TestModelBuilder());
            }

            void diEmpty(IServiceCollection services)
            {
                services.AddTestDefaultServices();
            }

            [TestMethod]
            public async Task ApiModelBuilder_ShouldProduceEmptyModelForEmptyApi()
            {
                var model = await RestierTestHelpers.GetTestableModelAsync<TestableEmptyApi>(serviceCollection: diEmpty, useEndpointRouting: UseEndpointRouting);
                model.SchemaElements.Should().HaveCount(1);
                model.EntityContainer.Elements.Should().BeEmpty();
            }

            [TestMethod]
            public async Task ApiModelBuilder_ShouldProduceCorrectModelForBasicScenario()
            {
                var model = await RestierTestHelpers.GetTestableModelAsync<ApiA>(serviceCollection: Api<ApiA>, useEndpointRouting: UseEndpointRouting);
                model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
                model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
                model.EntityContainer.FindEntitySet("People").Should().NotBeNull();
                model.EntityContainer.FindSingleton("Me").Should().NotBeNull();
            }

            [TestMethod]
            public async Task ApiModelBuilder_ShouldProduceCorrectModelForDerivedApi()
            {
                var model = await RestierTestHelpers.GetTestableModelAsync<ApiB>(serviceCollection: Api<ApiB>, useEndpointRouting: UseEndpointRouting);
                model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
                model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
                model.EntityContainer.FindEntitySet("Customers").Should().NotBeNull();
                model.EntityContainer.FindSingleton("Me").Should().NotBeNull();
                model.EntityContainer.FindEntitySet("People").Should().NotBeNull();
            }

            [TestMethod]
            public async Task ApiModelBuilder_ShouldProduceCorrectModelForOverridingProperty()
            {
                var model = await RestierTestHelpers.GetTestableModelAsync<ApiC>(serviceCollection: Api<ApiC>, useEndpointRouting: UseEndpointRouting);
                model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
                model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
                model.EntityContainer.FindEntitySet("People").Should().NotBeNull();
                model.EntityContainer.FindEntitySet("Customers").EntityType().Name.Should().Be("Customer");
                model.EntityContainer.FindSingleton("Me").EntityType().Name.Should().Be("Customer");
            }

            [TestMethod]
            public async Task ApiModelBuilder_ShouldProduceCorrectModelForIgnoringInheritedProperty()
            {
                var model = await RestierTestHelpers.GetTestableModelAsync<ApiD>(serviceCollection: Api<ApiD>, useEndpointRouting: UseEndpointRouting);
                model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
                model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
                model.EntityContainer.FindEntitySet("Customers").EntityType().Name.Should().Be("Customer");
                model.EntityContainer.FindSingleton("Me").EntityType().Name.Should().Be("Customer");
            }

            [TestMethod]
            public async Task ApiModelBuilder_ShouldSkipEntitySetWithUndeclaredType()
            {
                var model = await RestierTestHelpers.GetTestableModelAsync<ApiE>(serviceCollection: Api<ApiE>, useEndpointRouting: UseEndpointRouting);
                model.EntityContainer.FindEntitySet("People").EntityType().Name.Should().Be("Person");
                model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Orders");
            }

            [TestMethod]
            public async Task ApiModelBuilder_ShouldSkipExistingEntitySet()
            {
                var model = await RestierTestHelpers.GetTestableModelAsync<ApiF>(serviceCollection: Api<ApiF>, useEndpointRouting: UseEndpointRouting);
                model.EntityContainer.FindEntitySet("VipCustomers").EntityType().Name.Should().Be("VipCustomer");
            }

            [TestMethod]
            public async Task ApiModelBuilder_ShouldCorrectlyAddBindingsForCollectionNavigationProperty()
            {
                // In this case, only one entity set People has entity type Person.
                // Bindings for collection navigation property Customer.Friends should be added.
                // Bindings for singleton navigation property Customer.BestFriend should be added.
                var model = await RestierTestHelpers.GetTestableModelAsync<ApiC>(serviceCollection: Api<ApiC>, useEndpointRouting: UseEndpointRouting);

                var customersBindings = model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings.ToArray();

                var friendsBinding = customersBindings.FirstOrDefault(c => c.NavigationProperty.Name == "Friends");
                friendsBinding.Should().NotBeNull();
                friendsBinding.Target.Name.Should().Be("People");

                var bestFriendBinding = customersBindings.FirstOrDefault(c => c.NavigationProperty.Name == "BestFriend");
                bestFriendBinding.Should().NotBeNull();
                bestFriendBinding.Target.Name.Should().Be("People");

                var meBindings = model.EntityContainer.FindSingleton("Me").NavigationPropertyBindings.ToArray();

                var friendsBinding2 = meBindings.FirstOrDefault(c => c.NavigationProperty.Name == "Friends");
                friendsBinding2.Should().NotBeNull();
                friendsBinding2.Target.Name.Should().Be("People");

                var bestFriendBinding2 = meBindings.FirstOrDefault(c => c.NavigationProperty.Name == "BestFriend");
                bestFriendBinding2.Should().NotBeNull();
                bestFriendBinding2.Target.Name.Should().Be("People");
            }

            [TestMethod]
            public async Task ApiModelBuilder_ShouldCorrectlyAddBindingsForSingletonNavigationProperty()
            {
                // In this case, only one singleton Me has entity type Person.
                // Bindings for collection navigation property Customer.Friends should NOT be added.
                // Bindings for singleton navigation property Customer.BestFriend should be added.
                var model = await RestierTestHelpers.GetTestableModelAsync<ApiH>(serviceCollection: Api<ApiH>, useEndpointRouting: UseEndpointRouting);
                var binding = model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings.Single();
                binding.NavigationProperty.Name.Should().Be("BestFriend");
                binding.Target.Name.Should().Be("Me");
                binding = model.EntityContainer.FindSingleton("Me2").NavigationPropertyBindings.Single();
                binding.NavigationProperty.Name.Should().Be("BestFriend");
                binding.Target.Name.Should().Be("Me");
            }

            [TestMethod]
            public async Task ApiModelBuilder_ShouldNotAddAmbiguousNavigationPropertyBindings()
            {
                // In this case, two entity sets Employees and People have entity type Person.
                // Bindings for collection navigation property Customer.Friends should NOT be added.
                // Bindings for singleton navigation property Customer.BestFriend should NOT be added.
                var model = await RestierTestHelpers.GetTestableModelAsync<ApiG>(serviceCollection: Api<ApiG>, useEndpointRouting: UseEndpointRouting);
                model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings.Should().BeEmpty();
                model.EntityContainer.FindSingleton("Me").NavigationPropertyBindings.Should().BeEmpty();
            }

        }

        #region Test Resources

        public class TestModelBuilder : IModelBuilder
        {
            public IEdmModel GetModel(ModelContext context)
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
                return model;
            }
        }

        public class Person
        {
            public int PersonId { get; set; }
        }

        public class ApiA : TestableEmptyApi
        {

            [Resource]
            public IQueryable<Person> People { get; set; }
            [Resource]
            public Person Me { get; set; }
            public IQueryable<Person> Invisible { get; set; }

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

        public class ApiE : TestableEmptyApi
        {

            [Resource]
            public IQueryable<Person> People { get; set; }
            [Resource]
            public IQueryable<Order> Orders { get; set; }

            public ApiE(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

        }

        public class ApiF : TestableEmptyApi
        {

            public IQueryable<Customer> VipCustomers { get; set; }

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

        public class ApiH : TestableEmptyApi
        {

            [Resource]
            public Person Me { get; set; }
            [Resource]
            public IQueryable<Customer> Customers { get; set; }
            [Resource]
            public Customer Me2 { get; set; }

            public ApiH(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

        }

        #endregion

    }