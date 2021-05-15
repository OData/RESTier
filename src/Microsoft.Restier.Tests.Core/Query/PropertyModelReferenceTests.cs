// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Unit tests for the <see cref="PropertyModelReference"/> tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PropertyModelReferenceTests
    {
        /// <summary>
        /// Can construct an instance of <see cref="PropertyModelReference"/>.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new PropertyModelReference(new QueryModelReference(), "Name");
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can construct an instance of <see cref="PropertyModelReference"/> with three arguments.
        /// </summary>
        [TestMethod]
        public void CanConstructThreeArgs()
        {
            var instance = new PropertyModelReference(new QueryModelReference(), "Name", new Mock<IEdmProperty>().Object);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can get the source.
        /// </summary>
        [TestMethod]
        public void CanGetSource()
        {
            var queryModelReference = new QueryModelReference();
            var instance = new PropertyModelReference(queryModelReference, "Name", new Mock<IEdmProperty>().Object);
            instance.Source.Should().Be(queryModelReference);
        }

        /// <summary>
        /// Cannot get the source.
        /// </summary>
        [TestMethod]
        public void CannotGetSource()
        {
            var instance = new PropertyModelReference(default(QueryModelReference), "Name", new Mock<IEdmProperty>().Object);
            instance.Source.Should().BeNull();
        }

        /// <summary>
        /// Can get the EntitySet.
        /// </summary>
        [TestMethod]
        public void CanGetEntitySet()
        {
            var edmEntitySetMock = new Mock<IEdmEntitySet>();
            var edmTypeMock = new Mock<IEdmType>();
            var queryModelReference = new QueryModelReference(edmEntitySetMock.Object, edmTypeMock.Object);
            var instance = new PropertyModelReference(queryModelReference, "Name", new Mock<IEdmProperty>().Object);
            instance.EntitySet.Should().Be(edmEntitySetMock.Object);
        }

        /// <summary>
        /// Cannot get the entitySet.
        /// </summary>
        [TestMethod]
        public void CannotGetEntitySet()
        {
            var instance = new PropertyModelReference(default(QueryModelReference), "Name", new Mock<IEdmProperty>().Object);
            instance.Source.Should().BeNull();
        }

        /// <summary>
        /// Can get the type.
        /// </summary>
        [TestMethod]
        public void CanGetType()
        {
            var edmEntitySetMock = new Mock<IEdmEntitySet>();
            var edmTypeMock = new Mock<IEdmType>();
            var queryModelReference = new QueryModelReference(edmEntitySetMock.Object, edmTypeMock.Object);
            var propertyTypeReferenceMock = new Mock<IEdmTypeReference>();
            var propertyMock = new Mock<IEdmProperty>();
            propertyMock.Setup(x => x.Type).Returns(propertyTypeReferenceMock.Object);
            var propertyTypeMock = new Mock<IEdmType>();
            propertyTypeReferenceMock.Setup(x => x.Definition).Returns(propertyTypeMock.Object);
            var instance = new PropertyModelReference(queryModelReference, "Name", propertyMock.Object);
            instance.Type.Should().Be(propertyTypeMock.Object);
        }

        /// <summary>
        /// Cannot get the type.
        /// </summary>
        [TestMethod]
        public void CannotGetType()
        {
            var edmEntitySetMock = new Mock<IEdmEntitySet>();
            var edmTypeMock = new Mock<IEdmType>();
            var queryModelReference = new QueryModelReference(edmEntitySetMock.Object, edmTypeMock.Object);
            var instance = new PropertyModelReference(queryModelReference, "Name");
            instance.Type.Should().BeNull();
        }

        /// <summary>
        /// Can get a property.
        /// </summary>
        [TestMethod]
        public void CanGetProperty()
        {
            var edmEntitySetMock = new Mock<IEdmEntitySet>();
            var edmTypeMock = new Mock<IEdmType>();
            var queryModelReference = new QueryModelReference(edmEntitySetMock.Object, edmTypeMock.Object);
            var propertyMock = new Mock<IEdmProperty>();
            var instance = new PropertyModelReference(queryModelReference, "Name", propertyMock.Object);
            instance.Property.Should().Be(propertyMock.Object);
        }

        /// <summary>
        /// Can get a property.
        /// </summary>
        [TestMethod]
        public void CanGetPropertyThroughReference()
        {
            var edmEntitySetMock = new Mock<IEdmEntitySet>();
            var edmTypeMock = new Mock<IEdmType>();
            var edmStructuredTypeMock = edmTypeMock.As<IEdmStructuredType>();
            var queryModelReference = new QueryModelReference(edmEntitySetMock.Object, edmTypeMock.Object);
            var propertyMock = new Mock<IEdmProperty>();
            edmStructuredTypeMock.Setup(x => x.FindProperty(It.IsAny<string>())).Returns(propertyMock.Object);
            var instance = new PropertyModelReference(queryModelReference, "Name");
            instance.Property.Should().Be(propertyMock.Object);
        }

        /// <summary>
        /// Can get a property.
        /// </summary>
        [TestMethod]
        public void CannotGetProperty()
        {
            var edmEntitySetMock = new Mock<IEdmEntitySet>();
            var edmTypeMock = new Mock<IEdmType>();
            var queryModelReference = new QueryModelReference(edmEntitySetMock.Object, edmTypeMock.Object);
            var propertyMock = new Mock<IEdmProperty>();
            var instance = new PropertyModelReference(queryModelReference, "Name");
            instance.Property.Should().BeNull();
        }
    }
}