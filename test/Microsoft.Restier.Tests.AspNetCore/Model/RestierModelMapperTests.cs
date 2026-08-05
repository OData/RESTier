// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Linq;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

/// <summary>
/// Unit tests for <see cref="RestierModelMapper"/>.
/// </summary>
[TestClass]
public class RestierModelMapperTests
{
    [TestMethod]
    public void TryGetRelevantType_ShouldReturnTrue_WhenEntitySetIsFound()
    {
        // Arrange
        var mockInnerMapper = Substitute.For<IModelMapper>();
        var mockModel = Substitute.For<IEdmModel>();
        var mockEntityContainer = Substitute.For<IEdmEntityContainer>();
        var mockEntitySet = Substitute.For<IEdmEntitySet>();
        var mockEntityType = Substitute.For<IEdmEntityType>();
        var mockAnnotation = new ClrTypeAnnotation(typeof(string));

        mockModel.EntityContainer.Returns(mockEntityContainer);
        mockEntityContainer.Elements.Returns(new[] { mockEntitySet });
        mockEntitySet.Name.Returns("TestEntitySet");
        mockEntitySet.Type.Returns(new EdmCollectionType(new EdmEntityTypeReference(mockEntityType, false)));
        mockModel.GetAnnotationValue<ClrTypeAnnotation>(mockEntityType).Returns(mockAnnotation);
        var mockApi = Substitute.For<ApiBase>(mockModel, Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());

        var context = new InvocationContext(mockApi);
        var mapper = new RestierModelMapper { Inner = mockInnerMapper };

        // Act
        var result = mapper.TryGetRelevantType(context, "TestEntitySet", out var relevantType);

        // Assert
        result.Should().BeTrue();
        relevantType.Should().Be(typeof(string));
    }

    [TestMethod]
    public void TryGetRelevantType_ShouldReturnFalse_WhenEntitySetIsNotFound()
    {
        // Arrange
        var mockInnerMapper = Substitute.For<IModelMapper>();
        var mockApi = Substitute.For<ApiBase>(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
        var mockModel = Substitute.For<IEdmModel>();
        var mockEntityContainer = Substitute.For<IEdmEntityContainer>();

        mockModel.EntityContainer.Returns(mockEntityContainer);
        mockEntityContainer.Elements.Returns(Enumerable.Empty<IEdmEntityContainerElement>());

        var context = new InvocationContext(mockApi);
        var mapper = new RestierModelMapper { Inner = mockInnerMapper };

        // Act
        var result = mapper.TryGetRelevantType(context, "NonExistentEntitySet", out var relevantType);

        // Assert
        result.Should().BeFalse();
        relevantType.Should().BeNull();
    }

    [TestMethod]
    public void TryGetRelevantType_ShouldDelegateToInnerMapper_WhenElementIsNotFound()
    {
        // Arrange
        var mockInnerMapper = Substitute.For<IModelMapper>();
        var mockApi = Substitute.For<ApiBase>(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
        var mockModel = Substitute.For<IEdmModel>();
        var mockEntityContainer = Substitute.For<IEdmEntityContainer>();

        mockModel.EntityContainer.Returns(mockEntityContainer);
        mockEntityContainer.Elements.Returns(Enumerable.Empty<IEdmEntityContainerElement>());

        var context = new InvocationContext(mockApi);
        var mapper = new RestierModelMapper { Inner = mockInnerMapper };

        Type expectedType = typeof(int);
        mockInnerMapper.TryGetRelevantType(context, "NonExistentEntitySet", out Arg.Any<Type>())
            .Returns(x =>
            {
                x[2] = expectedType;
                return true;
            });

        // Act
        var result = mapper.TryGetRelevantType(context, "NonExistentEntitySet", out var relevantType);

        // Assert
        result.Should().BeTrue();
        relevantType.Should().Be(expectedType);
    }

    [TestMethod]
    public void TryGetRelevantType_ComposableFunction_ShouldDelegateToInnerMapper()
    {
        // Arrange
        var mockInnerMapper = Substitute.For<IModelMapper>();
        var context = Substitute.For<InvocationContext>(Substitute.For<ApiBase>(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>()));
        var mapper = new RestierModelMapper { Inner = mockInnerMapper };

        Type expectedType = typeof(int);
        mockInnerMapper.TryGetRelevantType(context, "Namespace", "FunctionName", out Arg.Any<Type>())
            .Returns(x =>
            {
                x[3] = expectedType;
                return true;
            });

        // Act
        var result = mapper.TryGetRelevantType(context, "Namespace", "FunctionName", out var relevantType);

        // Assert
        result.Should().BeTrue();
        relevantType.Should().Be(expectedType);
    }

    /// <summary>
    /// Verifies that an unbound function import returning <c>Collection(&lt;ComplexType&gt;)</c>
    /// — the keyless-view shape — resolves to the ClrTypeAnnotation on the return-type
    /// element. Without this the keyless-view dispatch path (api.GetQueryableSource&lt;T&gt;(name))
    /// would throw NotSupportedException, blocking the controller from routing view queries
    /// through the query pipeline.
    /// </summary>
    [TestMethod]
    public void TryGetRelevantType_KnownKeylessViewFunctionImport_ResolvesToClrType()
    {
        // Arrange — EDM scaffold: one ComplexType + an unbound function import returning Collection(ComplexType).
        var edmModel = new EdmModel();
        var complexType = new EdmComplexType("TestNs", "FakeView");
        complexType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32);
        edmModel.AddElement(complexType);
        edmModel.SetAnnotationValue(complexType, new ClrTypeAnnotation(typeof(FakeView)));

        var returnTypeRef = new EdmCollectionTypeReference(
            new EdmCollectionType(new EdmComplexTypeReference(complexType, isNullable: true)));
        var function = new EdmFunction(
            "TestNs.Views",
            "FakeView",
            returnTypeRef,
            isBound: false,
            entitySetPathExpression: null,
            isComposable: false);
        edmModel.AddElement(function);

        var container = new EdmEntityContainer("TestNs", "Container");
        container.AddFunctionImport("FakeView", function);
        edmModel.AddElement(container);

        var mockInnerMapper = Substitute.For<IModelMapper>();
        var mockApi = Substitute.For<ApiBase>(edmModel, Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());

        var context = new InvocationContext(mockApi);
        var mapper = new RestierModelMapper { Inner = mockInnerMapper };

        // Act
        var result = mapper.TryGetRelevantType(context, "FakeView", out var relevantType);

        // Assert
        result.Should().BeTrue();
        relevantType.Should().Be(typeof(FakeView));
        mockInnerMapper.DidNotReceiveWithAnyArgs().TryGetRelevantType(default, default, out _);
    }

    /// <summary>
    /// CLR-only stand-in for a keyless EF view; mirrors the shape used by
    /// <c>ConventionBasedQueryExpressionProcessorTests.FakeView</c>.
    /// </summary>
    private sealed class FakeView
    {
        public int Id { get; set; }
    }
}
