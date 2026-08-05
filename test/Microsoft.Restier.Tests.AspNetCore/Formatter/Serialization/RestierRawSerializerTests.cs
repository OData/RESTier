// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Formatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Formatter.Serialization;

/// <summary>
/// Unit tests for <see cref="RestierRawSerializer"/>.
/// </summary>
[TestClass]
public class RestierRawSerializerTests
{
    public TestContext TestContext { get; set; }

    private readonly ODataPayloadValueConverter _mockPayloadValueConverter;
    private readonly RestierRawSerializer _serializer;

    public RestierRawSerializerTests()
    {
        _mockPayloadValueConverter = Substitute.For<ODataPayloadValueConverter>();
        _serializer = new RestierRawSerializer(_mockPayloadValueConverter);
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenPayloadValueConverterIsNull()
    {
        // Act
        Action act = () => new RestierRawSerializer(null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*payloadValueConverter*");
    }

    [TestMethod]
    public async Task WriteObjectAsync_ShouldUseRawResult_WhenGraphIsRawResult()
    {
        // Arrange
        var value = "TestResult";
        var queryable = (new List<string>() { value }).AsQueryable();
        var edmType = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.String);
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
        writeContext.RootElementName = "System_String";
        _mockPayloadValueConverter
    .ConvertToPayloadValue(value, Arg.Any<EdmTypeReference>())
    .Returns(value);

        var rawResult = new RawResult(queryable, edmTypeReference);
        var expected = "TestResult";

        // Act
        await _serializer.WriteObjectAsync(rawResult, typeof(RawResult), messageWriter, writeContext);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync(TestContext.CancellationTokenSource.Token);
        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task WriteObjectAsync_ShouldConvertToPayloadValue_WhenWriteContextIsNotNull()
    {
        // Arrange
        var stream = new MemoryStream();
        var message = Substitute.For<IODataRequestMessageAsync>();
        message.GetStreamAsync().Returns(Task.FromResult((Stream)stream));
        var messageWriter = new ODataMessageWriter(message);
        var segment = Substitute.For<ODataPathSegment>();
        var writeContext = new ODataSerializerContext
        {
            Path = Substitute.For<ODataPath>([segment])
        };

        var graph = "TestGraph";
        var expected = "ConvertedValue";
        _mockPayloadValueConverter.ConvertToPayloadValue(graph, Arg.Any<IEdmTypeReference>()).Returns(expected);

        // Act
        await _serializer.WriteObjectAsync(graph, typeof(string), messageWriter, writeContext);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync(TestContext.CancellationTokenSource.Token);
        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task WriteObjectAsync_ShouldSerializeEmptyString_WhenGraphIsNull()
    {
        // Arrange       
        var stream = new MemoryStream();
        var message = Substitute.For<IODataRequestMessageAsync>();
        message.GetStreamAsync().Returns(Task.FromResult((Stream)stream));
        var messageWriter = new ODataMessageWriter(message);
        var segment = Substitute.For<ODataPathSegment>();
        var writeContext = new ODataSerializerContext
        {
            Path = Substitute.For<ODataPath>([segment])
        };

        // Act
        await _serializer.WriteObjectAsync(null, typeof(string), messageWriter, writeContext);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync(TestContext.CancellationTokenSource.Token);
        result.Should().Be(string.Empty);
    }
}
