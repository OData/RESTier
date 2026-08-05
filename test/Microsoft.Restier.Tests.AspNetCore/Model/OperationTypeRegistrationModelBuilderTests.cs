// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

[TestClass]
public class OperationTypeRegistrationModelBuilderTests
{
    private readonly IModelBuilder _innerModelBuilder = Substitute.For<IModelBuilder>();

    /// <summary>
    /// Returns the EDM full name for a CLR type using the OData convention:
    /// Namespace.SimpleName. This avoids the '+' separator in nested CLR FullName.
    /// </summary>
    private static string EdmName(System.Type type)
    {
        var ns = type.Namespace;
        return string.IsNullOrEmpty(ns) ? type.Name : ns + "." + type.Name;
    }

    [TestMethod]
    public void GetEdmModel_NullInnerModel_ReturnsNull()
    {
        _innerModelBuilder.GetEdmModel().Returns((IEdmModel)null);
        var builder = new OperationTypeRegistrationModelBuilder(typeof(EmptyApi))
        {
            Inner = _innerModelBuilder,
        };

        builder.GetEdmModel().Should().BeNull();
    }

    [TestMethod]
    public void GetEdmModel_NoOperations_PassesModelThrough()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(EmptyApi)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        result.Should().BeSameAs(inner);
        result.SchemaElements.OfType<IEdmSchemaType>().Should().BeEmpty();
    }

    [TestMethod]
    public void GetEdmModel_OperationWithMissingComplexType_RegistersIt()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithUnknownComplexInput)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        result.FindDeclaredType(EdmName(typeof(SearchCriteria))).Should().NotBeNull();
        result.FindDeclaredType(EdmName(typeof(SearchCriteria))).Should().BeAssignableTo<IEdmComplexType>();
    }

    [TestMethod]
    public void GetEdmModel_OperationWithMissingEnumReturn_RegistersIt()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithUnknownEnumReturn)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        result.FindDeclaredType(EdmName(typeof(Color))).Should().BeAssignableTo<IEdmEnumType>();
    }

    [TestMethod]
    public void GetEdmModel_OperationWithMissingEntityReturn_RegistersAsEntityType()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithUnknownEntityReturn)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        var declared = result.FindDeclaredType(EdmName(typeof(Author)));
        declared.Should().BeAssignableTo<IEdmEntityType>();
        // No entity set is created for operation-only types.
        result.EntityContainer.EntitySets().Should().NotContain(s => s.EntityType.FullName() == EdmName(typeof(Author)));
    }

    [TestMethod]
    public void GetEdmModel_NestedComplexType_RegistersBoth()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithNestedComplex)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        result.FindDeclaredType(EdmName(typeof(Outer))).Should().NotBeNull();
        result.FindDeclaredType(EdmName(typeof(InnerComplex))).Should().NotBeNull();
    }

    [TestMethod]
    public void GetEdmModel_OperationWithIReadOnlyListReturn_RegistersElementType()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithIReadOnlyListReturn))
        {
            Inner = _innerModelBuilder,
        };

        var result = builder.GetEdmModel();

        // SearchCriteria is the element type of the IReadOnlyList<...> return; it must be registered.
        result.SchemaElements.OfType<IEdmComplexType>()
            .Should().Contain(t => t.Name == nameof(SearchCriteria));
    }

    [TestMethod]
    public void GetEdmModel_RegisteredType_HasClrTypeAnnotation()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithUnknownComplexInput)) { Inner = _innerModelBuilder };

        var result = (EdmModel)builder.GetEdmModel();

        var registered = result.SchemaElements.OfType<IEdmSchemaType>()
            .First(t => t.Name == nameof(SearchCriteria));
        var clrAnnotation = result.GetAnnotationValue<ClrTypeAnnotation>(registered);
        clrAnnotation.Should().NotBeNull();
        clrAnnotation.ClrType.Should().Be(typeof(SearchCriteria));
    }

    [TestMethod]
    public void GetEdmModel_TypeAlreadyDeclared_DoesNotDuplicate()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        var existing = new EdmComplexType(typeof(SearchCriteria).Namespace, nameof(SearchCriteria));
        inner.AddElement(existing);
        inner.SetAnnotationValue(existing, new ClrTypeAnnotation(typeof(SearchCriteria)));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithUnknownComplexInput)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        result.SchemaElements.OfType<IEdmComplexType>()
            .Count(t => t.FullName() == EdmName(typeof(SearchCriteria))).Should().Be(1);
    }

    // ---- Test scenario surface ----

    public class EmptyApi { }

    public class ApiWithUnknownComplexInput
    {
        [UnboundOperation]
        public int Search(SearchCriteria criteria) => 0;
    }

    public class ApiWithUnknownEnumReturn
    {
        [UnboundOperation]
        public Color GetColor() => Color.Red;
    }

    public class ApiWithUnknownEntityReturn
    {
        [UnboundOperation]
        public Author GetAuthor() => null;
    }

    public class ApiWithNestedComplex
    {
        [UnboundOperation]
        public Outer Wrap() => null;
    }

    public class ApiWithIReadOnlyListReturn
    {
        [UnboundOperation]
        public System.Collections.Generic.IReadOnlyList<SearchCriteria> Recent() => null;
    }

    public class SearchCriteria
    {
        public string Query { get; set; }
        public int Limit { get; set; }
    }

    public enum Color { Red, Green, Blue }

    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Outer
    {
        public InnerComplex Detail { get; set; }
    }

    public class InnerComplex
    {
        public string Note { get; set; }
    }
}
