// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Formatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;

namespace Microsoft.Restier.Tests.AspNetCore.Formatter
{
    /// <summary>
    /// Unit tests for the <see cref="DefaultRestierSerializerProvider"/> class.
    /// </summary>
    [TestClass]
    public class DefaultRestierSerializerProviderTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DefaultRestierSerializerProvider _serializerProvider;

        public DefaultRestierSerializerProviderTests()
        {
            _serviceProvider = Substitute.For<IServiceProvider>();
            _serializerProvider = new DefaultRestierSerializerProvider(_serviceProvider);
        }

        [TestMethod]
        public void Constructor_ShouldThrow_WhenServiceProviderIsNull()
        {
            // Act
            Action act = () => new DefaultRestierSerializerProvider(null);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
        }

        [TestMethod]
        public void GetODataPayloadSerializer_ShouldReturnCorrectSerializer_ForKnownTypes()
        {
            // Arrange
            var httpRequest = Substitute.For<HttpRequest>();

            // Act & Assert
            _serializerProvider.GetODataPayloadSerializer(typeof(ResourceSetResult), httpRequest)
                .Should().BeOfType<RestierResourceSetSerializer>();

            _serializerProvider.GetODataPayloadSerializer(typeof(PrimitiveResult), httpRequest)
                .Should().BeOfType<RestierPrimitiveSerializer>();

            _serializerProvider.GetODataPayloadSerializer(typeof(RawResult), httpRequest)
                .Should().BeOfType<RestierRawSerializer>();

            _serializerProvider.GetODataPayloadSerializer(typeof(ComplexResult), httpRequest)
                .Should().BeOfType<RestierResourceSerializer>();

            _serializerProvider.GetODataPayloadSerializer(typeof(NonResourceCollectionResult), httpRequest)
                .Should().BeOfType<RestierCollectionSerializer>();

            _serializerProvider.GetODataPayloadSerializer(typeof(EnumResult), httpRequest)
                .Should().BeOfType<RestierEnumSerializer>();
        }

        [TestMethod]
        public void GetODataPayloadSerializer_ShouldThrow_ForUnknownType()
        {
            // Arrange
            var httpRequest = Substitute.For<HttpRequest>();
            var unknownType = typeof(DefaultRestierDeserializerProviderTests);

            // Act
            Action act = () => _serializerProvider.GetODataPayloadSerializer(unknownType, httpRequest);

            // Assert
            act.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void GetEdmTypeSerializer_ShouldReturnCorrectSerializer_ForEdmTypes()
        {
            // Arrange
            var complexType = new EdmComplexTypeReference(new EdmComplexType("Namespace", "ComplexType"), isNullable: true);

            var primitiveTypeReference = Substitute.For<IEdmPrimitiveTypeReference>();
            var primitiveType = Substitute.For<IEdmPrimitiveType>();    
            primitiveType.TypeKind.Returns(EdmTypeKind.Primitive);
            primitiveTypeReference.Definition.Returns(primitiveType);

            var enumType = new EdmEnumTypeReference(new EdmEnumType("Namespace", "EnumType"), isNullable: true);
            var resourceSetType = new EdmCollectionTypeReference(new EdmCollectionType(new EdmEntityTypeReference(new EdmEntityType("Namespace", "MyEntity"), isNullable: true)));
            var collectionTypeReference = new EdmCollectionTypeReference(new EdmCollectionType(primitiveTypeReference));

            // Act & Assert
            _serializerProvider.GetEdmTypeSerializer(complexType).Should().BeOfType<RestierResourceSerializer>();
            _serializerProvider.GetEdmTypeSerializer(primitiveTypeReference).Should().BeOfType<RestierPrimitiveSerializer>();
            _serializerProvider.GetEdmTypeSerializer(enumType).Should().BeOfType<RestierEnumSerializer>();
            _serializerProvider.GetEdmTypeSerializer(resourceSetType).Should().BeOfType<RestierResourceSetSerializer>();
            _serializerProvider.GetEdmTypeSerializer(collectionTypeReference).Should().BeOfType<RestierCollectionSerializer>();
        }

        [TestMethod]
        public void GetEdmTypeSerializer_ShouldFallbackToBase_ForUnknownEdmType()
        {
            // Arrange
            var unknownEdmType = Substitute.For<IEdmTypeReference>();

            // Act
            var result = _serializerProvider.GetEdmTypeSerializer(unknownEdmType);

            // Assert
            result.Should().BeNull(); // Base implementation returns null for unknown types.
        }
    }
}
