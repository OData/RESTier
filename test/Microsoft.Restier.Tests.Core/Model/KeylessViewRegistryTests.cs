// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Restier.Core.Model;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Model;

public class KeylessViewRegistryTests
{
    [Fact]
    public void Register_StoresEntry_RetrievableByName()
    {
        var registry = new KeylessViewRegistry();
        Func<object, IQueryable> factory = _ => Enumerable.Empty<string>().AsQueryable();

        registry.Register("MyView", typeof(string), factory);

        registry.TryGet("MyView", out var entry).Should().BeTrue();
        entry.Should().NotBeNull();
        entry.FunctionImportName.Should().Be("MyView");
        entry.ClrType.Should().Be(typeof(string));
        entry.SourceFactory.Should().BeSameAs(factory);
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForUnknownName()
    {
        var registry = new KeylessViewRegistry();

        registry.TryGet("NotRegistered", out var entry).Should().BeFalse();
        entry.Should().BeNull();
    }

    [Fact]
    public void Register_Throws_OnDuplicateName()
    {
        var registry = new KeylessViewRegistry();
        registry.Register("MyView", typeof(string), _ => Enumerable.Empty<string>().AsQueryable());

        var act = () => registry.Register("MyView", typeof(int), _ => Enumerable.Empty<int>().AsQueryable());

        act.Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("MyView"));
    }

    [Fact]
    public void Register_RejectsNullName()
    {
        var registry = new KeylessViewRegistry();
        var act = () => registry.Register(null, typeof(string), _ => Enumerable.Empty<string>().AsQueryable());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Register_RejectsNullType()
    {
        var registry = new KeylessViewRegistry();
        var act = () => registry.Register("X", null, _ => Enumerable.Empty<string>().AsQueryable());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_RejectsNullFactory()
    {
        var registry = new KeylessViewRegistry();
        var act = () => registry.Register("X", typeof(string), null);
        act.Should().Throw<ArgumentNullException>();
    }
}
