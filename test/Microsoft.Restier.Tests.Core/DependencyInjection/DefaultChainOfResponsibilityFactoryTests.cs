// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using NSubstitute;

namespace Microsoft.Restier.Core.DependencyInjection.Tests;

/// <summary>
/// Unit tests for the <see cref="DefaultChainOfResponsibilityFactory{T}"/> class.
/// </summary>
[TestClass]
public class DefaultChainOfResponsibilityFactoryTests
{
    public interface ITestChainedService : IChainedService<ITestChainedService> { }

    [TestMethod]
    public void Create_ShouldReturnNull_WhenNoServicesAreRegistered()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService<IEnumerable<IChainedService<ITestChainedService>>>().Returns(new List<IChainedService<ITestChainedService>>());

        var factory = new DefaultChainOfResponsibilityFactory<ITestChainedService>(serviceProvider);

        // Act
        var result = factory.Create();

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void Create_ShouldReturnSingleService_WhenOneServiceIsRegistered()
    {
        // Arrange
        var service = Substitute.For<ITestChainedService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService<IEnumerable<IChainedService<ITestChainedService>>>().Returns(new List<IChainedService<ITestChainedService>> { service });

        var factory = new DefaultChainOfResponsibilityFactory<ITestChainedService>(serviceProvider);

        // Act
        var result = factory.Create();

        // Assert
        result.Should().Be(service);
        result.Inner.Should().BeNull();
    }

    [TestMethod]
    public void Create_ShouldChainServicesInOrder_WhenMultipleServicesAreRegistered()
    {
        // Arrange
        var service1 = Substitute.For<ITestChainedService>();
        var service2 = Substitute.For<ITestChainedService>();
        var service3 = Substitute.For<ITestChainedService>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService<IEnumerable<IChainedService<ITestChainedService>>>().Returns(new List<IChainedService<ITestChainedService>> { service1, service2, service3 });

        var factory = new DefaultChainOfResponsibilityFactory<ITestChainedService>(serviceProvider);

        // Act
        var result = factory.Create();

        // Assert
        result.Should().Be(service3);
        result.Inner.Should().Be(service2);
        result.Inner.Inner.Should().Be(service1);
        result.Inner.Inner.Inner.Should().BeNull();
    }
}
