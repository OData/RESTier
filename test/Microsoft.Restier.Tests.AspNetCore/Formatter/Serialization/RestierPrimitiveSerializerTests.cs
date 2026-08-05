// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Formatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Restier.Tests.AspNetCore.Formatter;

/// <summary>
/// Unit tests for the <see cref="RestierPrimitiveSerializer"/> class.
/// </summary>
[TestClass]
public class RestierPrimitiveSerializerTests
{
    public TestContext TestContext { get; set; }

    private readonly ODataPayloadValueConverter _mockPayloadValueConverter;
    private readonly RestierPrimitiveSerializer _serializer;

    public RestierPrimitiveSerializerTests()
    {
        _mockPayloadValueConverter = Substitute.For<ODataPayloadValueConverter>();
        _serializer = new RestierPrimitiveSerializer(_mockPayloadValueConverter);
    }

    [TestMethod]
    public async Task WriteObjectAsync_ShouldHandlePrimitiveResult()
    {
        // Arrange
        var value = 42;
        var queryable = (new List<int>() { value }).AsQueryable();
        var edmType = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.Int32);
        var edmTypeReference = new EdmStringTypeReference(edmType, false);
        var stream = new MemoryStream();
        var message = Substitute.For<IODataRequestMessageAsync>();
        message.GetStreamAsync().Returns(Task.FromResult((Stream)stream));
        var messageWriter = new ODataMessageWriter(message);
        var segment = Substitute.For<ODataPathSegment>();
        var writeContext = new ODataSerializerContext
        {
            Path = Substitute.For<ODataPath>([segment])
        };
        writeContext.Path.LastSegment.EdmType.Returns(edmType);
        var model = new EdmModel();
        model.AddElement(edmType);
        writeContext.Model = model;
        writeContext.RootElementName = "System_Int32";
        _mockPayloadValueConverter
    .ConvertToPayloadValue(value, Arg.Any<EdmTypeReference>())
    .Returns(value);

        var primitiveResult = new PrimitiveResult(queryable, edmTypeReference);
        var expected = @"{""value"":42}";

        // Act
        await _serializer.WriteObjectAsync(primitiveResult, typeof(PrimitiveResult), messageWriter, writeContext);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync(TestContext.CancellationTokenSource.Token);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void CreateODataPrimitiveValue_ShouldConvertDateTimeToDateTimeOffset()
    {
        // Arrange
        var dateTime = new DateTime(2025, 4, 21, 12, 0, 0, DateTimeKind.Utc);
        var primitiveType = Substitute.For<IEdmPrimitiveTypeReference>();
        var primitiveTypeDefinition = Substitute.For<IEdmPrimitiveType>();
        primitiveType.Definition.Returns(primitiveTypeDefinition);
        primitiveTypeDefinition.TypeKind.Returns(EdmTypeKind.Primitive);
        primitiveTypeDefinition.PrimitiveKind.Returns(EdmPrimitiveTypeKind.DateTimeOffset);
        var writeContext = new ODataSerializerContext();

        // Act
        var result = _serializer.CreateODataPrimitiveValue(dateTime, primitiveType, writeContext);

        // Assert
        result.Should().BeOfType<ODataPrimitiveValue>();
        ((ODataPrimitiveValue)result).Value.Should().Be(new DateTimeOffset(dateTime, TimeSpan.Zero));
    }

    [TestMethod]
    public void ConvertToPayloadValue_ShouldUsePayloadValueConverter()
    {
        // Arrange
        var value = 42;
        var segment = Substitute.For<ODataPathSegment>();
        var writeContext = new ODataSerializerContext
        {
            Path = Substitute.For<ODataPath>([segment])
        };
        var edmType = Substitute.For<IEdmPrimitiveType>();
        writeContext.Path.LastSegment.EdmType.Returns(edmType);

        _mockPayloadValueConverter
            .ConvertToPayloadValue(value, Arg.Any<EdmTypeReference>())
            .Returns(value);

        // Act
        var result = RestierPrimitiveSerializer.ConvertToPayloadValue(value, writeContext, _mockPayloadValueConverter);

        // Assert
        result.Should().Be(value);
    }

    [TestMethod]
    public void Constructor_ShouldThrowIfPayloadValueConverterIsNull()
    {
        // Act
        Action act = () => new RestierPrimitiveSerializer(null);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*payloadValueConverter*");
    }
}
