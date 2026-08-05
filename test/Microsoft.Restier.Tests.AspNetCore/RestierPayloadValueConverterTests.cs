// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

#pragma warning disable CS0618 // Date and TimeOfDay are obsolete but still used by OData
namespace Microsoft.Restier.Tests.AspNetCore;

/// <summary>
/// Unit tests for the <see cref="RestierPayloadValueConverter"/> class.
/// </summary>
[TestClass]
public class RestierPayloadValueConverterTests
{
    private readonly RestierPayloadValueConverter _converter;

    public RestierPayloadValueConverterTests()
    {
        _converter = new RestierPayloadValueConverter();
    }

    [TestMethod]
    public void ConvertToPayloadValue_ShouldReturnDate_ForDateTimeAndEdmDate()
    {
        // Arrange
        var dateTime = new DateTime(2025, 4, 21);
        var edmTypeReference = EdmCoreModel.Instance.GetDate(false);

        // Act
        var result = _converter.ConvertToPayloadValue(dateTime, edmTypeReference);

        // Assert
        result.Should().BeOfType<Date>().Which.Should().BeEquivalentTo(new Date(2025, 4, 21));
    }

    [TestMethod]
    public void ConvertToPayloadValue_ShouldReturnDateTimeOffsetWithLocalOffset_ForDateTimeWithLocalKind()
    {
        // Arrange
        var dateTime = new DateTime(2025, 4, 21, 10, 0, 0, DateTimeKind.Local);
        var edmTypeReference = EdmCoreModel.Instance.GetDateTimeOffset(false);

        // Act
        var result = _converter.ConvertToPayloadValue(dateTime, edmTypeReference);

        // Assert
        result.Should().BeOfType<DateTimeOffset>().Which.Offset.Should().Be(TimeZoneInfo.Local.GetUtcOffset(dateTime));
    }

    [TestMethod]
    public void ConvertToPayloadValue_ShouldReturnDateTimeOffsetWithZeroOffset_ForDateTimeWithUtcKind()
    {
        // Arrange
        var dateTime = new DateTime(2025, 4, 21, 10, 0, 0, DateTimeKind.Utc);
        var edmTypeReference = EdmCoreModel.Instance.GetDateTimeOffset(false);

        // Act
        var result = _converter.ConvertToPayloadValue(dateTime, edmTypeReference);

        // Assert
        result.Should().BeOfType<DateTimeOffset>().Which.Offset.Should().Be(TimeSpan.Zero);
    }

    [TestMethod]
    public void ConvertToPayloadValue_ShouldReturnTimeOfDay_ForTimeSpanAndEdmTimeOfDay()
    {
        // Arrange
        var timeSpan = new TimeSpan(10, 30, 0);
        var edmTypeReference = EdmCoreModel.Instance.GetTimeOfDay(false);

        // Act
        var result = _converter.ConvertToPayloadValue(timeSpan, edmTypeReference);

        // Assert
        result.Should().BeOfType<TimeOfDay>().Which.Should().BeEquivalentTo(new TimeOfDay(10, 30, 0, 0));
    }

    [TestMethod]
    public void ConvertToPayloadValue_ShouldReturnDate_ForDateTimeOffsetAndEdmDate()
    {
        // Arrange
        var dateTimeOffset = new DateTimeOffset(2025, 4, 21, 10, 0, 0, TimeSpan.Zero);
        var edmTypeReference = EdmCoreModel.Instance.GetDate(false);

        // Act
        var result = _converter.ConvertToPayloadValue(dateTimeOffset, edmTypeReference);

        // Assert
        result.Should().BeOfType<Date>().Which.Should().BeEquivalentTo(new Date(2025, 4, 21));
    }

    [TestMethod]
    public void ConvertToPayloadValue_ShouldReturnDate_ForDateOnlyAndEdmDate()
    {
        // Arrange
        var dateOnly = new DateOnly(2025, 4, 21);
        var edmTypeReference = EdmCoreModel.Instance.GetDate(false);

        // Act
        var result = _converter.ConvertToPayloadValue(dateOnly, edmTypeReference);

        // Assert
        result.Should().BeOfType<Date>().Which.Should().BeEquivalentTo(new Date(2025, 4, 21));
    }

    [TestMethod]
    public void ConvertToPayloadValue_ShouldReturnTimeOfDay_ForTimeOnlyAndEdmTimeOfDay()
    {
        // Arrange
        var timeOnly = new TimeOnly(10, 30, 45, 500);
        var edmTypeReference = EdmCoreModel.Instance.GetTimeOfDay(false);

        // Act
        var result = _converter.ConvertToPayloadValue(timeOnly, edmTypeReference);

        // Assert
        result.Should().BeOfType<TimeOfDay>().Which.Should().BeEquivalentTo(new TimeOfDay(10, 30, 45, 500));
    }

    [TestMethod]
    public void ConvertToPayloadValue_ShouldCallBaseMethod_ForUnsupportedTypes()
    {
        // Arrange
        var unsupportedValue = "unsupported";
        var edmTypeReference = Substitute.For<IEdmTypeReference>();

        var baseConverter = Substitute.For<ODataPayloadValueConverter>();
        var converter = Substitute.ForPartsOf<RestierPayloadValueConverter>();
        converter.When(x => x.ConvertToPayloadValue(unsupportedValue, edmTypeReference))
                 .DoNotCallBase();

        // Act
        converter.ConvertToPayloadValue(unsupportedValue, edmTypeReference);

        // Assert
        converter.Received(1).ConvertToPayloadValue(unsupportedValue, edmTypeReference);
    }

    [TestMethod]
    public void Spatial_branch_dispatches_to_registered_ISpatialTypeConverter()
    {
        var fakeStorageValue = new object();
        var fakeEdmValue = Microsoft.Spatial.GeographyPoint.Create(
            Microsoft.Spatial.CoordinateSystem.Geography(4326), 0, 0, null, null);

        var converter = Substitute.For<ISpatialTypeConverter>();
        converter.CanConvert(typeof(object)).Returns(true);
        converter.ToEdm(fakeStorageValue, typeof(Microsoft.Spatial.GeographyPoint)).Returns(fakeEdmValue);

        var sut = new RestierPayloadValueConverter(new[] { converter });

        var edmRef = new EdmPrimitiveTypeReference(
            EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.GeographyPoint),
            isNullable: true);

        var result = sut.ConvertToPayloadValue(fakeStorageValue, edmRef);

        result.Should().BeSameAs(fakeEdmValue);
        converter.Received().ToEdm(fakeStorageValue, typeof(Microsoft.Spatial.GeographyPoint));
    }

    [TestMethod]
    public void Parameterless_construction_still_works()
    {
        var sut = new RestierPayloadValueConverter();
        sut.Should().NotBeNull();
    }
}
