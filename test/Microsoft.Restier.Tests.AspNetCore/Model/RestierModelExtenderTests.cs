// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

/// <summary>
/// Tests for the <see cref="RestierWebApiModelExtender"/> verifying entity set/singleton
/// discovery, inheritance, navigation property bindings, and property overriding.
/// </summary>
[TestClass]
public class RestierModelExtenderTests
{
    private static void ConfigureWithModelBuilder(IServiceCollection services)
    {
        services.AddTestDefaultServices();
        services.AddChainedService<IModelBuilder>((sp, next) => new ExtenderTestModelBuilder());
    }

    private static void ConfigureEmpty(IServiceCollection services)
    {
        services.AddTestDefaultServices();
    }

    [TestMethod]
    public async Task ApiModelBuilder_ShouldProduceEmptyModelForEmptyApi()
    {
        var model = await RestierTestHelpers.GetTestableModelAsync<ExtenderTestEmptyApi>(
            serviceCollection: ConfigureEmpty);
        model.SchemaElements.Should().HaveCount(1);
        model.EntityContainer.Elements.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ApiModelBuilder_ShouldProduceCorrectModelForBasicScenario()
    {
        var model = await RestierTestHelpers.GetTestableModelAsync<ExtenderTestApiA>(
            serviceCollection: ConfigureWithModelBuilder);
        model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
        model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
        model.EntityContainer.FindEntitySet("People").Should().NotBeNull();
        model.EntityContainer.FindSingleton("Me").Should().NotBeNull();
    }

    [TestMethod]
    public async Task ApiModelBuilder_ShouldProduceCorrectModelForDerivedApi()
    {
        var model = await RestierTestHelpers.GetTestableModelAsync<ExtenderTestApiB>(
            serviceCollection: ConfigureWithModelBuilder);
        model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
        model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
        model.EntityContainer.FindEntitySet("Customers").Should().NotBeNull();
        model.EntityContainer.FindSingleton("Me").Should().NotBeNull();
        model.EntityContainer.FindEntitySet("People").Should().NotBeNull();
    }

    [TestMethod]
    public async Task ApiModelBuilder_ShouldProduceCorrectModelForOverridingProperty()
    {
        var model = await RestierTestHelpers.GetTestableModelAsync<ExtenderTestApiC>(
            serviceCollection: ConfigureWithModelBuilder);
        model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
        model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
        model.EntityContainer.FindEntitySet("People").Should().NotBeNull();
        model.EntityContainer.FindEntitySet("Customers").EntityType.Name.Should().Be("ExtenderTestCustomer");
        model.EntityContainer.FindSingleton("Me").EntityType.Name.Should().Be("ExtenderTestCustomer");
    }

    [TestMethod]
    public async Task ApiModelBuilder_ShouldProduceCorrectModelForIgnoringInheritedProperty()
    {
        var model = await RestierTestHelpers.GetTestableModelAsync<ExtenderTestApiD>(
            serviceCollection: ConfigureWithModelBuilder);
        model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("ApiConfiguration");
        model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Invisible");
        model.EntityContainer.FindEntitySet("Customers").EntityType.Name.Should().Be("ExtenderTestCustomer");
        model.EntityContainer.FindSingleton("Me").EntityType.Name.Should().Be("ExtenderTestCustomer");
    }

    [TestMethod]
    public async Task ApiModelBuilder_ShouldSkipEntitySetWithUndeclaredType()
    {
        var model = await RestierTestHelpers.GetTestableModelAsync<ExtenderTestApiE>(
            serviceCollection: ConfigureWithModelBuilder);
        model.EntityContainer.FindEntitySet("People").EntityType.Name.Should().Be("ExtenderTestPerson");
        model.EntityContainer.Elements.Select(e => e.Name).Should().NotContain("Orders");
    }

    [TestMethod]
    public async Task ApiModelBuilder_ShouldSkipExistingEntitySet()
    {
        var model = await RestierTestHelpers.GetTestableModelAsync<ExtenderTestApiF>(
            serviceCollection: ConfigureWithModelBuilder);
        model.EntityContainer.FindEntitySet("VipCustomers").EntityType.Name.Should().Be("ExtenderTestVipCustomer");
    }

    [TestMethod]
    public async Task ApiModelBuilder_ShouldCorrectlyAddBindingsForCollectionNavigationProperty()
    {
        // In this case, only one entity set People has entity type Person.
        // Bindings for collection navigation property Customer.Friends should be added.
        // Bindings for singleton navigation property Customer.BestFriend should be added.
        var model = await RestierTestHelpers.GetTestableModelAsync<ExtenderTestApiC>(
            serviceCollection: ConfigureWithModelBuilder);

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
        var model = await RestierTestHelpers.GetTestableModelAsync<ExtenderTestApiH>(
            serviceCollection: ConfigureWithModelBuilder);
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
        var model = await RestierTestHelpers.GetTestableModelAsync<ExtenderTestApiG>(
            serviceCollection: ConfigureWithModelBuilder);
        model.EntityContainer.FindEntitySet("Customers").NavigationPropertyBindings.Should().BeEmpty();
        model.EntityContainer.FindSingleton("Me").NavigationPropertyBindings.Should().BeEmpty();
    }
}

#region Test Resources

public class ExtenderTestModelBuilder : IModelBuilder
{
    public IModelBuilder Inner { get; set; }

    public IEdmModel GetEdmModel()
    {
        var model = new EdmModel();
        var ns = typeof(ExtenderTestPerson).Namespace;
        var personType = new EdmEntityType(ns, "ExtenderTestPerson");
        personType.AddKeys(personType.AddStructuralProperty("PersonId", EdmPrimitiveTypeKind.Int32));
        model.AddElement(personType);
        var customerType = new EdmEntityType(ns, "ExtenderTestCustomer");
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
        var vipCustomerType = new EdmEntityType(ns, "ExtenderTestVipCustomer", customerType);
        model.AddElement(vipCustomerType);
        var container = new EdmEntityContainer(ns, "DefaultContainer");
        container.AddEntitySet("VipCustomers", vipCustomerType);
        model.AddElement(container);
        return model;
    }
}

public class ExtenderTestPerson
{
    public int PersonId { get; set; }
}

public class ExtenderTestCustomer
{
    public int CustomerId { get; set; }
    public ICollection<ExtenderTestPerson> Friends { get; set; }
    public ExtenderTestPerson BestFriend { get; set; }
}

public class ExtenderTestVipCustomer : ExtenderTestCustomer
{
}

public class ExtenderTestOrder
{
    public int OrderId { get; set; }
}

public class ExtenderTestEmptyApi : ApiBase
{
    public ExtenderTestEmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }
}

public class ExtenderTestApiA : ExtenderTestEmptyApi
{
    [Resource]
    public IQueryable<ExtenderTestPerson> People { get; set; }

    [Resource]
    public ExtenderTestPerson Me { get; set; }

    public IQueryable<ExtenderTestPerson> Invisible { get; set; }

    public ExtenderTestApiA(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }
}

public class ExtenderTestApiB : ExtenderTestApiA
{
    [Resource]
    public IQueryable<ExtenderTestPerson> Customers { get; set; }

    public ExtenderTestApiB(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }
}

public class ExtenderTestApiC : ExtenderTestApiB
{
    [Resource]
    public new IQueryable<ExtenderTestCustomer> Customers { get; set; }

    [Resource]
    public new ExtenderTestCustomer Me { get; set; }

    public ExtenderTestApiC(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }
}

public class ExtenderTestApiD : ExtenderTestApiC
{
    public ExtenderTestApiD(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }
}

public class ExtenderTestApiE : ExtenderTestEmptyApi
{
    [Resource]
    public IQueryable<ExtenderTestPerson> People { get; set; }

    [Resource]
    public IQueryable<ExtenderTestOrder> Orders { get; set; }

    public ExtenderTestApiE(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }
}

public class ExtenderTestApiF : ExtenderTestEmptyApi
{
    public IQueryable<ExtenderTestCustomer> VipCustomers { get; set; }

    public ExtenderTestApiF(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }
}

public class ExtenderTestApiG : ExtenderTestApiC
{
    [Resource]
    public IQueryable<ExtenderTestPerson> Employees { get; set; }

    public ExtenderTestApiG(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }
}

public class ExtenderTestApiH : ExtenderTestEmptyApi
{
    [Resource]
    public ExtenderTestPerson Me { get; set; }

    [Resource]
    public IQueryable<ExtenderTestCustomer> Customers { get; set; }

    [Resource]
    public ExtenderTestCustomer Me2 { get; set; }

    public ExtenderTestApiH(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }
}

#endregion
