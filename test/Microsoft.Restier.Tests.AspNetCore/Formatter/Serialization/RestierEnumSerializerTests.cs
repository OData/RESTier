// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Formatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Formatter;

/// <summary>
/// Unit tests for <see cref="RestierEnumSerializer"/>.
/// </summary>
[TestClass]
public class RestierEnumSerializerTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task WriteObjectAsync_ShouldCallBaseWriteObjectAsync_WithUnpackedResult()
    {
        // Arrange
        var provider = new DefaultRestierSerializerProvider(Substitute.For<IServiceProvider>());
        var serializer = new RestierEnumSerializer(provider);
        var stream = new MemoryStream();
        var message = Substitute.For<IODataRequestMessageAsync>();
        message.GetStreamAsync().Returns(Task.FromResult((Stream)stream));
        var messageWriter = new ODataMessageWriter(message);
        var writeContext = new ODataSerializerContext();
        var model = new EdmModel();
        var enumType = new EdmEnumType("System", "AttributeTargets");
        model.AddElement(enumType);
        writeContext.Model = model;
        writeContext.RootElementName = "System_AttributeTargets";

        var queryable = (new List<AttributeTargets>() { AttributeTargets.Struct }).AsQueryable();
        var edmType = new EdmEnumTypeReference(enumType, false);
        var enumResult = new EnumResult(queryable, edmType);
        var expected = @"{""@odata.type"":""#System.AttributeTargets"",""value"":""Struct""}";

        // Act
        await serializer.WriteObjectAsync(enumResult, typeof(EnumResult), messageWriter, writeContext);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync(TestContext.CancellationTokenSource.Token);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void UnpackResult_ShouldReturnGraphAndType_WhenInputIsEnumResult()
    {
        // Arrange
        var expectedQueryable = (new List<AttributeTargets>() { AttributeTargets.Struct }).AsQueryable();
        var edmType = new EdmEnumTypeReference(new EdmEnumType("System", "AttributeTargets"), false);
        var expectedType = typeof(IQueryable<string>);

        var enumResult = new EnumResult(expectedQueryable, edmType);

        // Act
        var result = RestierEnumSerializer.UnpackResult(enumResult, typeof(EnumResult));

        // Assert
        result.Graph.Should().Be(AttributeTargets.Struct);
        result.Type.Should().Be(typeof(AttributeTargets));
    }

    [TestMethod]
    public void UnpackResult_ShouldReturnOriginalGraphAndType_WhenInputIsNotEnumResult()
    {
        // Arrange
        var graph = "TestValue";
        var type = typeof(string);

        // Act
        var result = RestierEnumSerializer.UnpackResult(graph, type);

        // Assert
        result.Graph.Should().Be(graph);
        result.Type.Should().Be(type);
    }
}
