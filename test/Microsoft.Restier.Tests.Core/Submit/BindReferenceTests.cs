// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core.Submit;

[TestClass]
public class BindReferenceTests
{
    [TestMethod]
    public void BindReference_CanStoreResourceSetAndKey()
    {
        var bindRef = new BindReference
        {
            ResourceSetName = "Publishers",
            ResourceKey = new Dictionary<string, object> { { "Id", "PUB01" } },
        };

        bindRef.ResourceSetName.Should().Be("Publishers");
        bindRef.ResourceKey.Should().ContainKey("Id").WhoseValue.Should().Be("PUB01");
    }

    [TestMethod]
    public void BindReference_ResolvedEntity_DefaultsToNull()
    {
        var bindRef = new BindReference();
        bindRef.ResolvedEntity.Should().BeNull();
    }

    [TestMethod]
    public void NavigationBindings_CanStoreMultipleReferences()
    {
        var item = new DataModificationItem(
            "Publishers", typeof(object), typeof(object),
            RestierEntitySetOperation.Insert, null, null,
            new Dictionary<string, object>());

        var refs = new List<BindReference>
        {
            new() { ResourceSetName = "Books", ResourceKey = new Dictionary<string, object> { { "Id", System.Guid.NewGuid() } } },
            new() { ResourceSetName = "Books", ResourceKey = new Dictionary<string, object> { { "Id", System.Guid.NewGuid() } } },
        };

        item.NavigationBindings["Books"] = refs;
        item.NavigationBindings["Books"].Should().HaveCount(2);
    }
}
