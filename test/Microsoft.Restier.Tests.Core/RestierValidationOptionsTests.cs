// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core;

[TestClass]
public class RestierValidationOptionsTests
{
    [TestMethod]
    public void Defaults_AreAllNull()
    {
        var options = new RestierValidationOptions();

        options.MaxTop.Should().BeNull();
        options.MaxSkip.Should().BeNull();
        options.MaxExpansionDepth.Should().BeNull();
        options.MaxAnyAllExpressionDepth.Should().BeNull();
        options.MaxOrderByNodeCount.Should().BeNull();
        options.MaxNodeCount.Should().BeNull();
    }

    [TestMethod]
    public void Properties_AreMutable()
    {
        var options = new RestierValidationOptions
        {
            MaxTop = 100,
            MaxSkip = 1000,
            MaxExpansionDepth = 3,
            MaxAnyAllExpressionDepth = 2,
            MaxOrderByNodeCount = 4,
            MaxNodeCount = 50,
        };

        options.MaxTop.Should().Be(100);
        options.MaxSkip.Should().Be(1000);
        options.MaxExpansionDepth.Should().Be(3);
        options.MaxAnyAllExpressionDepth.Should().Be(2);
        options.MaxOrderByNodeCount.Should().Be(4);
        options.MaxNodeCount.Should().Be(50);
    }

    [TestMethod]
    public void Properties_AreReassignableAfterConstruction()
    {
        var options = new RestierValidationOptions { MaxTop = 50 };

        options.MaxTop = 100;

        options.MaxTop.Should().Be(100);
    }

    [TestMethod]
    public void RestierRouteOptions_Validation_DefaultsToNonNullEmptyBag()
    {
        var route = new RestierRouteOptions();

        route.Validation.Should().NotBeNull();
        route.Validation.MaxTop.Should().BeNull();
        route.Validation.MaxExpansionDepth.Should().BeNull();
    }

    [TestMethod]
    public void RestierRouteOptions_Validation_IsMutableViaPropertyAccess()
    {
        var route = new RestierRouteOptions();

        route.Validation.MaxTop = 25;
        route.Validation.MaxExpansionDepth = 3;

        route.Validation.MaxTop.Should().Be(25);
        route.Validation.MaxExpansionDepth.Should().Be(3);
    }
}
