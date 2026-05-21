// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFrameworkCore;

[TestClass]
public class EFModelMapperTests
{
    [TestMethod]
    public async Task TryGetRelevantType_KnownEntitySet_ReturnsTrue_AndCorrectType()
    {
        var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        api.Should().NotBeNull();

        var mapperFactory = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, IChainOfResponsibilityFactory<IModelMapper>>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        mapperFactory.Should().NotBeNull();

        var mapper = mapperFactory.Create();
        mapper.Should().NotBeNull();

        var context = new InvocationContext(api);

        var result = mapper.TryGetRelevantType(context, "Books", out var relevantType);

        result.Should().BeTrue();
        relevantType.Should().Be(typeof(Book));
    }

    [TestMethod]
    public async Task TryGetRelevantType_UnknownName_ReturnsFalse()
    {
        var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        api.Should().NotBeNull();

        var mapperFactory = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, IChainOfResponsibilityFactory<IModelMapper>>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        mapperFactory.Should().NotBeNull();

        var mapper = mapperFactory.Create();
        var context = new InvocationContext(api);

        var result = mapper.TryGetRelevantType(context, "NonExistent", out var relevantType);

        result.Should().BeFalse();
        relevantType.Should().BeNull();
    }

    [TestMethod]
    public async Task TryGetRelevantType_NamespaceOverload_ReturnsFalse()
    {
        var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        api.Should().NotBeNull();

        var mapperFactory = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, IChainOfResponsibilityFactory<IModelMapper>>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        mapperFactory.Should().NotBeNull();

        var mapper = mapperFactory.Create();
        var context = new InvocationContext(api);

        var result = mapper.TryGetRelevantType(context, "Microsoft.Restier.Tests", "Books", out var relevantType);

        result.Should().BeFalse();
        relevantType.Should().BeNull();
    }

    [TestMethod]
    public async Task TryGetRelevantType_AllKnownEntitySets_ReturnCorrectTypes()
    {
        var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        api.Should().NotBeNull();

        var mapperFactory = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, IChainOfResponsibilityFactory<IModelMapper>>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());

        mapperFactory.Should().NotBeNull();

        var mapper = mapperFactory.Create();
        var context = new InvocationContext(api);

        mapper.TryGetRelevantType(context, "Publishers", out var publisherType).Should().BeTrue();
        publisherType.Should().Be(typeof(Publisher));

        mapper.TryGetRelevantType(context, "Readers", out var readersType).Should().BeTrue();
        readersType.Should().Be(typeof(Employee));

        mapper.TryGetRelevantType(context, "LibraryCards", out var libraryCardsType).Should().BeTrue();
        libraryCardsType.Should().Be(typeof(LibraryCard));
    }
}
