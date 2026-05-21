// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CS0618 // Date and TimeOfDay are obsolete but still used by OData
namespace Microsoft.Restier.Tests.AspNetCore;

/// <summary>
/// Unit tests for the <see cref="EFChangeSetInitializer.ConvertToEfValue"/> method.
/// </summary>
[TestClass]
public class EFChangeSetInitializerTests
{
    private readonly EFChangeSetInitializer _initializer;

    public EFChangeSetInitializerTests()
    {
        _initializer = new EFChangeSetInitializer();
    }

    [TestMethod]
    public void ConvertToEfValue_ShouldReturnDateOnly_ForEdmDateAndDateOnlyTarget()
    {
        // Arrange
        var edmDate = new Date(2025, 4, 21);

        // Act
        var result = _initializer.ConvertToEfValue(typeof(DateOnly), edmDate);

        // Assert
        result.Should().BeOfType<DateOnly>().Which.Should().Be(new DateOnly(2025, 4, 21));
    }

    [TestMethod]
    public void ConvertToEfValue_ShouldReturnDateTime_ForEdmDateAndDateTimeTarget()
    {
        // Arrange
        var edmDate = new Date(2025, 4, 21);

        // Act
        var result = _initializer.ConvertToEfValue(typeof(DateTime), edmDate);

        // Assert
        result.Should().BeOfType<DateTime>().Which.Should().Be(new DateTime(2025, 4, 21));
    }

    [TestMethod]
    public void ConvertToEfValue_ShouldReturnTimeOnly_ForEdmTimeOfDayAndTimeOnlyTarget()
    {
        // Arrange
        var edmTimeOfDay = new TimeOfDay(10, 30, 45, 500);

        // Act
        var result = _initializer.ConvertToEfValue(typeof(TimeOnly), edmTimeOfDay);

        // Assert
        result.Should().BeOfType<TimeOnly>().Which.Should().Be(new TimeOnly(10, 30, 45, 500));
    }

    [TestMethod]
    public void ConvertToEfValue_ShouldReturnTimeSpan_ForEdmTimeOfDayAndTimeSpanTarget()
    {
        // Arrange
        var edmTimeOfDay = new TimeOfDay(10, 30, 45, 0);

        // Act
        var result = _initializer.ConvertToEfValue(typeof(TimeSpan), edmTimeOfDay);

        // Assert
        result.Should().BeOfType<TimeSpan>().Which.Should().Be(new TimeSpan(10, 30, 45));
    }
}
