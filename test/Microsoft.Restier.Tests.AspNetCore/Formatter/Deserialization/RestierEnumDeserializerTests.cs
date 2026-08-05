// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder.Annotations;
using Microsoft.Restier.AspNetCore.Formatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Restier.Tests.AspNetCore.Formatter
{
    /// <summary>
    /// Unit tests for the <see cref="RestierEnumDeserializer"/> class."/>
    /// </summary>
    [TestClass]
    public class RestierEnumDeserializerTests
    {
        private readonly RestierEnumDeserializer deserializer;

        public RestierEnumDeserializerTests()
        {
            deserializer = new RestierEnumDeserializer();
        }

        [TestMethod]
        public void Constructor_ShouldInitialize()
        {
            // Act
            var instance = new RestierEnumDeserializer();

            // Assert
            instance.Should().NotBeNull();
        }

        [TestMethod]
        public void ReadInline_ShouldReturnEnumValue_WhenResultIsEdmEnumObject()
        {
            // Arrange
            var edmType = Substitute.For<IEdmTypeReference>();
            var enumType = new EdmEnumType("System", "AttributeTargets");
            edmType.Definition.Returns(enumType);
            var readContext = new ODataDeserializerContext();
            readContext.Model = Substitute.For<IEdmModel>();
            
            var edmEnumObject = new ODataEnumValue("Parameter");

            // Act
            var result = deserializer.ReadInline(edmEnumObject, edmType, readContext);

            // Assert
            result.Should().Be(AttributeTargets.Parameter);
        }

        [TestMethod]
        public void ReadInline_ShouldReturnBaseResult_WhenResultIsNotEdmEnumObject()
        {
            // Arrange
            var edmType = Substitute.For<IEdmTypeReference>();
            edmType.Definition.Returns(new EdmEntityType("System", "Object"));
            var readContext = new ODataDeserializerContext();
            readContext.Model = Substitute.For<IEdmModel>();
            var nonEnumObject = new object();

            // Mock the base method behavior
            var baseDeserializer = Substitute.For<ODataEnumDeserializer>();
            baseDeserializer.ReadInline(nonEnumObject, edmType, readContext).Returns(nonEnumObject);

            // Act
            var result = deserializer.ReadInline(nonEnumObject, edmType, readContext);

            // Assert
            result.Should().Be(nonEnumObject);
        }

        [TestMethod]
        public void ReadInline_ShouldThrowArgumentNullException_WhenEdmTypeIsNull()
        {
            // Arrange
            var readContext = new ODataDeserializerContext();
            var item = new object();

            // Act
            Action act = () => deserializer.ReadInline(item, null, readContext);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithMessage("*type*");
        }
    }
}
