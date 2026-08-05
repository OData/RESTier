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

namespace Microsoft.Restier.Tests.AspNetCore.Formatter
{
    /// <summary>
    /// Unit tests for <see cref="RestierCollectionSerializer"/>.
    /// </summary>
    [TestClass]
    public class RestierCollectionSerializerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task WriteObjectAsync_CallsBaseWriteObjectAsync_WithUnpackedResult()
        {
            // Arrange
            var provider = new DefaultRestierSerializerProvider(Substitute.For<IServiceProvider>());
            var serializer = new RestierCollectionSerializer(provider);
            var stream = new MemoryStream();
            var message = Substitute.For<IODataRequestMessageAsync>();
            message.GetStreamAsync().Returns(Task.FromResult((Stream)stream));
            var messageWriter = new ODataMessageWriter(message);
            var writeContext = new ODataSerializerContext();
            writeContext.Model = EdmCoreModel.Instance;
            writeContext.RootElementName = "System_String";
            var expectedQueryable = (new List<string>() { "Item1", "Item2" }).AsQueryable();
            var edmType = new EdmStringTypeReference(EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.String), false);
            string expected = @"{""value"":[""Item1"",""Item2""]}";

            var inputResult = new NonResourceCollectionResult(expectedQueryable, edmType);

            // Act
            await serializer.WriteObjectAsync(inputResult, typeof(NonResourceCollectionResult), messageWriter, writeContext);

            // Assert
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var result = await reader.ReadToEndAsync(TestContext.CancellationTokenSource.Token);
            result.Should().Be(expected);
        }

        [TestMethod]
        public void UnpackResult_ReturnsCorrectGraphAndType_ForNonResourceCollectionResult()
        {
            // Arrange
            var expectedQueryable = (new List<string>() { "Item1", "Item2" }).AsQueryable();
            var edmType = new EdmStringTypeReference(EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.String), false);
            var expectedType = typeof(IQueryable<string>);

            var inputResult = new NonResourceCollectionResult(expectedQueryable, edmType);

            // Act
            var (graph, type) = RestierCollectionSerializer.UnpackResult(inputResult, typeof(NonResourceCollectionResult));

            // Assert
            graph.Should().Be(expectedQueryable);
            type.Should().Implement(expectedType);
        }

        [TestMethod]
        public void UnpackResult_ReturnsOriginalGraphAndType_ForNonNonResourceCollectionResult()
        {
            // Arrange
            var inputGraph = new[] { "Item1", "Item2" };
            var inputType = typeof(string[]);

            // Act
            var (graph, type) = RestierCollectionSerializer.UnpackResult(inputGraph, inputType);

            // Assert
            graph.Should().Be(inputGraph);
            type.Should().Be(inputType);
        }
    }
}
