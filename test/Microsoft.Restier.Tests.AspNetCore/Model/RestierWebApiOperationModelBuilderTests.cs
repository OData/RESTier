// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

/// <summary>
/// Unit tests for the <see cref="RestierWebApiOperationModelBuilder"/> class.
/// </summary>
[TestClass]
public class RestierWebApiOperationModelBuilderTests
{
    private readonly Type _targetApiType = typeof(SampleApi);
    private readonly IModelBuilder _innerModelBuilder = Substitute.For<IModelBuilder>();

    [TestMethod]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var extender = new RestierWebApiModelExtender(_targetApiType);

        // Act
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender);

        // Assert
        builder.Should().NotBeNull();
    }

    [TestMethod]
    public void GetEdmModel_ShouldReturnNull_WhenInnerModelBuilderReturnsNull()
    {
        // Arrange
        _innerModelBuilder.GetEdmModel().Returns((IEdmModel)null);
        var extender = new RestierWebApiModelExtender(_targetApiType);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender)
        {
            Inner = _innerModelBuilder
        };

        // Act
        var result = builder.GetEdmModel();

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void GetEdmModel_ShouldReturnModel_WhenInnerModelBuilderReturnsValidModel()
    {
        // Arrange
        var edmModel = new EdmModel();
        var container = new EdmEntityContainer("TestNamespace", "DefaultContainer");
        edmModel.AddElement(container);
        _innerModelBuilder.GetEdmModel().Returns(edmModel);

        var extender = new RestierWebApiModelExtender(_targetApiType);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender)
        {
            Inner = _innerModelBuilder
        };

        // Act
        var result = builder.GetEdmModel();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<EdmModel>();
    }

    [TestMethod]
    public void GetEdmModel_ShouldExtendModelWithOperations()
    {
        // Arrange
        var edmModel = new EdmModel();
        var container = new EdmEntityContainer("TestNamespace", "DefaultContainer");
        edmModel.AddElement(container);
        _innerModelBuilder.GetEdmModel().Returns(edmModel);

        var extender = new RestierWebApiModelExtender(_targetApiType);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender)
        {
            Inner = _innerModelBuilder
        };

        // Act
        var result = builder.GetEdmModel();

        // Assert
        result.Should().NotBeNull();
        var test = edmModel.FindDeclaredOperationImports("SampleMethod");
        test.Count().Should().Be(1);
    }

    [TestMethod]
    public void GetEdmModel_ShouldWarnWhenBoundOperationHasNoParameters()
    {
        var testTraceListener = new TestTraceListener();
        Trace.Listeners.Add(testTraceListener);

        try
        {
            // Arrange
            var edmModel = new EdmModel();
            var container = new EdmEntityContainer("TestNamespace", "DefaultContainer");
            edmModel.AddElement(container);
            _innerModelBuilder.GetEdmModel().Returns(edmModel);

            var extender = new RestierWebApiModelExtender(_targetApiType);
            var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender)
            {
                Inner = _innerModelBuilder
            };

            // Act
            var result = builder.GetEdmModel();

            // Assert
            result.Should().NotBeNull();
            testTraceListener.Messages.Should().Contain("The operation 'WrongBoundMethod' was marked with [BoundOperation], but no parameters were specified to bind against.");
        }
        finally
        {
            Trace.Listeners.Remove(testTraceListener);
        }
    }

    [TestMethod]
    public void GetEdmModel_DuplicateOperationByName_SkipsAttributeAdditionWithWarning()
    {
        var testTraceListener = new TestTraceListener();
        Trace.Listeners.Add(testTraceListener);
        try
        {
            // Arrange — inner model already declares an EdmFunction named "SampleMethod"
            // matching the [UnboundOperation] on SampleApi.SampleMethod.
            var edmModel = new EdmModel();
            var container = new EdmEntityContainer("TestNamespace", "DefaultContainer");
            edmModel.AddElement(container);
            var int32Ref = new EdmPrimitiveTypeReference(
                EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.Int32),
                isNullable: false);
            // Use the same namespace the builder will compute via GetNamespaceName:
            // modelNamespace = model.DeclaredNamespaces.FirstOrDefault() = "TestNamespace".
            var preexistingFunction = new EdmFunction(
                "TestNamespace", "SampleMethod", int32Ref);
            edmModel.AddElement(preexistingFunction);
            container.AddFunctionImport("SampleMethod", preexistingFunction);
            _innerModelBuilder.GetEdmModel().Returns(edmModel);

            var extender = new RestierWebApiModelExtender(_targetApiType);
            var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender)
            {
                Inner = _innerModelBuilder
            };

            // Act
            var result = builder.GetEdmModel();

            // Assert — exactly one EdmFunction named "SampleMethod" exists (the preexisting one),
            // exactly one OperationImport, and the warning has been emitted.
            result.Should().NotBeNull();
            result.SchemaElements.OfType<IEdmOperation>()
                .Count(o => o.Name == "SampleMethod").Should().Be(1);
            result.EntityContainer.Elements.OfType<IEdmOperationImport>()
                .Count(i => i.Name == "SampleMethod").Should().Be(1);
            testTraceListener.Messages.Should().Contain("already declared");
            testTraceListener.Messages.Should().Contain("SampleMethod");
        }
        finally
        {
            Trace.Listeners.Remove(testTraceListener);
        }
    }

    [TestMethod]
    [DataRow(nameof(SampleApi.IntWithDefault), "5", false)]
    [DataRow(nameof(SampleApi.NullableIntWithDefault), "null", true)]
    [DataRow(nameof(SampleApi.OptionalRef), "null", true)]
    [DataRow(nameof(SampleApi.DefaultValueAttr), "hello", true)]
    public void GetEdmModel_EmitsOptionalParameter_WithExpectedDefaultAndNullability(
        string operationName, string expectedDefault, bool expectedNullable)
    {
        var edmModel = new EdmModel();
        edmModel.AddElement(new EdmEntityContainer("TestNamespace", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(edmModel);
        var extender = new RestierWebApiModelExtender(_targetApiType);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel() as EdmModel;

        var op = result.SchemaElements.OfType<IEdmOperation>().Single(o => o.Name == operationName);
        var param = op.Parameters.Single();
        param.Should().BeAssignableTo<IEdmOptionalParameter>();
        ((IEdmOptionalParameter)param).DefaultValueString.Should().Be(expectedDefault);
        param.Type.IsNullable.Should().Be(expectedNullable);
    }

    [TestMethod]
    public void GetEdmModel_BareNullableParam_IsNullableButRequired()
    {
        var edmModel = new EdmModel();
        edmModel.AddElement(new EdmEntityContainer("TestNamespace", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(edmModel);
        var extender = new RestierWebApiModelExtender(_targetApiType);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel() as EdmModel;

        var op = result.SchemaElements.OfType<IEdmOperation>().Single(o => o.Name == nameof(SampleApi.NullableInt));
        var param = op.Parameters.Single();
        param.Should().NotBeAssignableTo<IEdmOptionalParameter>();   // not optional
        param.Type.IsNullable.Should().BeTrue();                     // but nullable
    }

    [TestMethod]
    public void GetEdmModel_PlainValueTypeParam_IsNonNullableAndRequired()
    {
        var edmModel = new EdmModel();
        edmModel.AddElement(new EdmEntityContainer("TestNamespace", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(edmModel);
        var extender = new RestierWebApiModelExtender(_targetApiType);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel() as EdmModel;

        var op = result.SchemaElements.OfType<IEdmOperation>().Single(o => o.Name == nameof(SampleApi.PlainValueType));
        var param = op.Parameters.Single();
        param.Should().NotBeAssignableTo<IEdmOptionalParameter>();
        param.Type.IsNullable.Should().BeFalse();
    }
}

// Sample API class for testing purposes
public class SampleApi
{
    [UnboundOperation]
    public int SampleMethod()
    {
        return 42;
    }

    [BoundOperation]
    public int WrongBoundMethod()
    {
        return 42;
    }

    [UnboundOperation]
    public int IntWithDefault(int p = 5) => p;

    [UnboundOperation]
    public int? NullableInt(int? p) => p;

    [UnboundOperation]
    public int? NullableIntWithDefault(int? p = null) => p;

    [UnboundOperation]
    public string OptionalRef([Microsoft.Restier.AspNetCore.Model.OptionalAttribute] string p) => p;

    [UnboundOperation]
    public string DefaultValueAttr([System.ComponentModel.DefaultValue("hello")] string p) => p;

    [UnboundOperation]
    public int PlainValueType(int p) => p;
}
