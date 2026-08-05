// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Formatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Formatter;

/// <summary>
/// Unit tests for <see cref="RestierResourceSerializer"/>.
/// </summary>
[TestClass]
public class RestierResourceSerializerTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void UnpackResult_ShouldReturnOriginalObject_WhenNotComplexResult()
    {
        // Arrange
        var inputObject = new object();
        var inputType = typeof(object);

        // Act
        var result = RestierResourceSerializer.UnpackResult(inputObject, inputType);

        // Assert
        result.Graph.Should().Be(inputObject);
        result.Type.Should().Be(inputType);
    }

    [TestMethod]
    public void UnpackResult_ShouldReturnComplexResultProperties_WhenComplexResult()
    {
        // Arrange
        var value = new Tuple<string, string>("Test", "Test");
        IQueryable<Tuple<string,string>> expectedGraph = new[] { value }.AsQueryable();
        var expectedType = new EdmComplexTypeReference(new EdmComplexType("Test", "Test"), false);
        var complexResult = new ComplexResult(expectedGraph, expectedType);

        // Act
        var result = RestierResourceSerializer.UnpackResult(complexResult, typeof(ComplexResult));

        // Assert
        result.Graph.Should().Be(value);
        result.Type.Should().Be(typeof(Tuple<string, string>));
    }

    [TestMethod]
    public async Task WriteObjectAsync_ShouldCallBaseWriteObjectAsync_WithUnpackedResult()
    {
        // Arrange
        var provider = new DefaultRestierSerializerProvider(Substitute.For<IServiceProvider>());
        var serializer = new RestierResourceSerializer(provider);
        var stream = new MemoryStream();
        var message = Substitute.For<IODataRequestMessageAsync>();
        message.GetStreamAsync().Returns(Task.FromResult((Stream)stream));
        var messageWriter = new ODataMessageWriter(message);
        var segment = Substitute.For<ODataPathSegment>();
        var writeContext = new ODataSerializerContext
        {
            Path = Substitute.For<ODataPath>([segment])
        };
        var modelBuilder = new ODataConventionModelBuilder();
        modelBuilder.AddComplexType(typeof(ComplexClass));
        var model = modelBuilder.GetEdmModel();
        writeContext.Model = model;
        writeContext.RootElementName = "System_String";

        var value = new ComplexClass() { Property1 = "Test", Property2 = "Test" };
        IQueryable<ComplexClass> expectedGraph = new[] { value }.AsQueryable();
        var edmComplexType = new EdmComplexType("Microsoft.Restier.Tests.AspNetCore.Formatter", "MyEntity");
        writeContext.Path.LastSegment.EdmType.Returns(edmComplexType);
        var expectedTypeReference = new EdmComplexTypeReference(edmComplexType, false);
        var complexResult = new ComplexResult(expectedGraph, expectedTypeReference);
        string expected = "{\"Property1\":\"Test\",\"Property2\":\"Test\"}";

        // Act
        await serializer.WriteObjectAsync(complexResult, typeof(ComplexResult), messageWriter, writeContext);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync(TestContext.CancellationTokenSource.Token);
        result.Should().Be(expected);
    }

    [DataContract]
    public class ComplexClass
    {
        [DataMember]
        public string Property1 { get; set; }

        [DataMember]
        public string Property2 { get; set; }
    }
}

