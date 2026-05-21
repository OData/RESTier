// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.AspNetCore.OData.Query.Wrapper;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Formatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Formatter;

/// <summary>
/// Unit tests for <see cref="RestierResourceSetSerializer"/>.
/// </summary>
[TestClass]
public class RestierResourceSetSerializerTests
{
    public TestContext TestContext { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ODataSerializerProvider _serializerProvider;
    private readonly RestierResourceSetSerializer _serializer;

    public RestierResourceSetSerializerTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serializerProvider = new DefaultRestierSerializerProvider(_serviceProvider);
        _serializer = new RestierResourceSetSerializer(_serializerProvider);
    }

    [TestMethod]
    public async Task WriteObjectAsync_Should_Call_Base_When_Not_ResourceSetResult()
    {
        // Arrange

        _serviceProvider.GetService(typeof(ODataResourceSerializer))
            .Returns(new RestierResourceSerializer(_serializerProvider));

        var graph = new[] { new MyEntity() { Property1 = "Test", Property2 = "Test" } };
        var type = graph.GetType();
        
        var stream = new MemoryStream();
        var message = Substitute.For<IODataRequestMessageAsync>();
        message.GetStreamAsync().Returns(Task.FromResult((Stream)stream));
        var messageWriter = new ODataMessageWriter(message);
        var writeContext = new ODataSerializerContext();
        
        var modelBuilder = new ODataConventionModelBuilder();
        modelBuilder.AddEntitySet("MyEntities", modelBuilder.AddEntityType(typeof(MyEntity)));
        var model = modelBuilder.GetEdmModel();
        writeContext.Model = model;
        string expected = "{\"value\":[{\"@odata.type\":\"#Microsoft.Restier.Tests.AspNetCore.Formatter.MyEntity\",\"Property1\":\"Test\",\"Property2\":\"Test\"}]}";

        // Act
        await _serializer.WriteObjectAsync(graph, type, messageWriter, writeContext);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync(TestContext.CancellationTokenSource.Token);
        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task WriteObjectAsync_Should_Handle_ResourceSetResult()
    {

        _serviceProvider.GetService(typeof(ODataResourceSerializer))
            .Returns(new RestierResourceSerializer(_serializerProvider));

        var graph = new[] { new MyEntity() { Property1 = "Test", Property2 = "Test" } };

        var stream = new MemoryStream();
        var message = Substitute.For<IODataRequestMessageAsync>();
        message.GetStreamAsync().Returns(Task.FromResult((Stream)stream));
        var messageWriter = new ODataMessageWriter(message);
        var writeContext = new ODataSerializerContext();

        var modelBuilder = new ODataConventionModelBuilder();
        var entityType = modelBuilder.AddEntityType(typeof(MyEntity));
        modelBuilder.AddEntitySet("MyEntities", entityType);
        var model = modelBuilder.GetEdmModel();
        writeContext.Model = model;
        string expected = "{\"value\":[{\"@odata.type\":\"#Microsoft.Restier.Tests.AspNetCore.Formatter.MyEntity\",\"Property1\":\"Test\",\"Property2\":\"Test\"}]}";
        var collectionResult = new ResourceSetResult(graph.AsQueryable(), new EdmEntityTypeReference(new EdmEntityType("Microsoft.Restier.Tests.AspNetCore.Formatter", "MyEntity"), false));

        // Act
        await _serializer.WriteObjectAsync(collectionResult, typeof(ResourceSetResult), messageWriter, writeContext);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync(TestContext.CancellationTokenSource.Token);
        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task TryWriteAggregationResult_Should_Return_True_For_DynamicTypeWrapper()
    {
        // Arrange

        _serviceProvider.GetService(typeof(ODataResourceSerializer))
            .Returns(new RestierResourceSerializer(_serializerProvider));

        var graph = new List<DynamicTypeWrapper>();
        var type = typeof(List<DynamicTypeWrapper>);
        var stream = new MemoryStream();
        var message = Substitute.For<IODataRequestMessageAsync>();
        message.GetStreamAsync().Returns(Task.FromResult((Stream)stream));
        var messageWriter = new ODataMessageWriter(message);
        var writeContext = new ODataSerializerContext
        {
            NavigationSource = Substitute.For<IEdmEntitySetBase>()
        };
        var modelBuilder = new ODataConventionModelBuilder();
        var entityType = modelBuilder.AddEntityType(typeof(MyEntity));
        modelBuilder.AddEntitySet("MyEntities", entityType);
        var model = modelBuilder.GetEdmModel();
        writeContext.Model = model;

        // Act
        var result = await _serializer.TryWriteAggregationResult(graph, type, messageWriter, writeContext, new EdmCollectionTypeReference(model.FindDeclaredEntitySet("MyEntities").Type as IEdmCollectionType));

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task TryWriteAggregationResult_Should_Return_False_For_NonDynamicTypeWrapper()
    {
        // Arrange

        _serviceProvider.GetService(typeof(ODataResourceSerializer))
          .Returns(new RestierResourceSerializer(_serializerProvider));

        var graph = new List<object>();
        var type = typeof(List<object>);
        var stream = new MemoryStream();
        var message = Substitute.For<IODataRequestMessageAsync>();
        message.GetStreamAsync().Returns(Task.FromResult((Stream)stream));
        var messageWriter = new ODataMessageWriter(message);
        var writeContext = new ODataSerializerContext
        {
            NavigationSource = Substitute.For<IEdmEntitySetBase>()
        };
        var modelBuilder = new ODataConventionModelBuilder();
        var entityType = modelBuilder.AddEntityType(typeof(MyEntity));
        modelBuilder.AddEntitySet("MyEntities", entityType);
        var model = modelBuilder.GetEdmModel();
        writeContext.Model = model;

        // Act
        var result = await _serializer.TryWriteAggregationResult(graph, type, messageWriter, writeContext, new EdmCollectionTypeReference(model.FindDeclaredEntitySet("MyEntities").Type as IEdmCollectionType));

        // Assert
        result.Should().BeFalse();
    }

    [DataContract]
    public class MyEntity
    {
        [DataMember]
        [Key]
        public string Property1 { get; set; }

        [DataMember]
        public string Property2 { get; set; }
    }
}
