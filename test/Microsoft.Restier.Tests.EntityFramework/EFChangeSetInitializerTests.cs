// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.EntityFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CS0618 // Date and TimeOfDay are obsolete but still used by OData
namespace Microsoft.Restier.Tests.EntityFramework;

[TestClass]
public class EFChangeSetInitializerTests
{
    private readonly EFChangeSetInitializer _initializer = new();

    public enum SampleEnum
    {
        Value1,
        Value2,
    }

    [TestMethod]
    public void ConvertToEfValue_ShouldReturnDateTime_ForEdmDate()
    {
        var edmDate = new Date(2025, 4, 21);

        var result = _initializer.ConvertToEfValue(typeof(DateTime), edmDate);

        result.Should().BeOfType<DateTime>().Which.Should().Be(new DateTime(2025, 4, 21));
    }

    [TestMethod]
    public void ConvertToEfValue_ShouldReturnDateTime_ForDateTimeOffset()
    {
        var dateTimeOffset = new DateTimeOffset(2025, 4, 21, 10, 30, 0, TimeSpan.FromHours(2));

        var result = _initializer.ConvertToEfValue(typeof(DateTime), dateTimeOffset);

        result.Should().BeOfType<DateTime>().Which.Should().Be(new DateTime(2025, 4, 21, 10, 30, 0));
    }

    [TestMethod]
    public void ConvertToEfValue_ShouldReturnTimeSpan_ForEdmTimeOfDay()
    {
        var edmTimeOfDay = new TimeOfDay(10, 30, 45, 0);

        var result = _initializer.ConvertToEfValue(typeof(TimeSpan), edmTimeOfDay);

        result.Should().BeOfType<TimeSpan>().Which.Should().Be(new TimeSpan(10, 30, 45));
    }

    [TestMethod]
    public void ConvertToEfValue_ShouldParseEnum_ForStringValue()
    {
        var result = _initializer.ConvertToEfValue(typeof(SampleEnum), "Value2");

        result.Should().Be(SampleEnum.Value2);
    }

    [TestMethod]
    public void ConvertToEfValue_ShouldReturnLong_ForIntValue()
    {
        var result = _initializer.ConvertToEfValue(typeof(long), 42);

        result.Should().BeOfType<long>().Which.Should().Be(42L);
    }

    [TestMethod]
    public void ConvertToEfValue_ShouldReturnOriginalValue_ForUnmappedType()
    {
        var result = _initializer.ConvertToEfValue(typeof(string), "hello");

        result.Should().Be("hello");
    }
}
