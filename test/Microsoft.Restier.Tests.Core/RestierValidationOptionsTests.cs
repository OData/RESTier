// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Restier.Core;
using Xunit;

namespace Microsoft.Restier.Tests.Core;

public class RestierValidationOptionsTests
{
    [Fact]
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

    [Fact]
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
}
