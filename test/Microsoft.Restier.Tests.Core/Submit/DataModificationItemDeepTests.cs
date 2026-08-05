// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core.Submit;

[TestClass]
public class DataModificationItemDeepTests
{
    [TestMethod]
    public void NestedItems_DefaultsToEmptyList()
    {
        var item = CreateItem("Books", RestierEntitySetOperation.Insert);
        item.NestedItems.Should().NotBeNull();
        item.NestedItems.Should().BeEmpty();
    }

    [TestMethod]
    public void NavigationBindings_DefaultsToEmptyDictionary()
    {
        var item = CreateItem("Books", RestierEntitySetOperation.Insert);
        item.NavigationBindings.Should().NotBeNull();
        item.NavigationBindings.Should().BeEmpty();
    }

    [TestMethod]
    public void ParentItem_DefaultsToNull()
    {
        var item = CreateItem("Books", RestierEntitySetOperation.Insert);
        item.ParentItem.Should().BeNull();
        item.ParentNavigationPropertyName.Should().BeNull();
    }

    [TestMethod]
    public void ParentItem_CanBeSet()
    {
        var parent = CreateItem("Publishers", RestierEntitySetOperation.Insert);
        var child = CreateItem("Books", RestierEntitySetOperation.Insert);
        child.ParentItem = parent;
        child.ParentNavigationPropertyName = "Books";

        child.ParentItem.Should().BeSameAs(parent);
        child.ParentNavigationPropertyName.Should().Be("Books");
    }

    [TestMethod]
    public void FlattenDepthFirst_SingleItem_ReturnsSelf()
    {
        var item = CreateItem("Publishers", RestierEntitySetOperation.Insert);
        var flat = item.FlattenDepthFirst().ToList();
        flat.Should().HaveCount(1);
        flat[0].Should().BeSameAs(item);
    }

    [TestMethod]
    public void FlattenDepthFirst_WithChildren_ReturnsParentBeforeChildren()
    {
        var parent = CreateItem("Publishers", RestierEntitySetOperation.Insert);
        var child1 = CreateItem("Books", RestierEntitySetOperation.Insert);
        var child2 = CreateItem("Books", RestierEntitySetOperation.Insert);
        parent.NestedItems.Add(child1);
        parent.NestedItems.Add(child2);

        var flat = parent.FlattenDepthFirst().ToList();
        flat.Should().HaveCount(3);
        flat[0].Should().BeSameAs(parent);
        flat[1].Should().BeSameAs(child1);
        flat[2].Should().BeSameAs(child2);
    }

    [TestMethod]
    public void FlattenDepthFirst_MultiLevel_ReturnsCorrectOrder()
    {
        var root = CreateItem("Publishers", RestierEntitySetOperation.Insert);
        var child = CreateItem("Books", RestierEntitySetOperation.Insert);
        var grandchild = CreateItem("Reviews", RestierEntitySetOperation.Insert);
        root.NestedItems.Add(child);
        child.NestedItems.Add(grandchild);

        var flat = root.FlattenDepthFirst().ToList();
        flat.Should().HaveCount(3);
        flat[0].Should().BeSameAs(root);
        flat[1].Should().BeSameAs(child);
        flat[2].Should().BeSameAs(grandchild);
    }

    private static DataModificationItem CreateItem(string resourceSetName, RestierEntitySetOperation operation)
    {
        return new DataModificationItem(
            resourceSetName,
            typeof(object),
            typeof(object),
            operation,
            null,
            null,
            new Dictionary<string, object>());
    }
}
