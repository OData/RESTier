// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.Edm.Vocabularies.V1;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

[TestClass]
public class ConventionBasedAnnotationModelBuilderTests
{
    private const string CoreDescriptionTerm = "Org.OData.Core.V1.Description";
    private const string CoreRevisionsTerm = "Org.OData.Core.V1.Revisions";
    private const string CoreComputedTerm = "Org.OData.Core.V1.Computed";
    private const string CoreImmutableTerm = "Org.OData.Core.V1.Immutable";
    private const string ValidationMinimumTerm = "Org.OData.Validation.V1.Minimum";
    private const string ValidationMaximumTerm = "Org.OData.Validation.V1.Maximum";
    private const string ValidationPatternTerm = "Org.OData.Validation.V1.Pattern";

    [TestMethod]
    public void GetEdmModel_EmitsCoreDescription_WhenEntityTypeHasDescriptionAttribute()
    {
        // Arrange
        var inputModel = AnnotationTestFixtures.BuildModelWith<DescribedEntity>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert
        var entityType = result.FindDeclaredType(typeof(DescribedEntity).FullName);
        entityType.Should().NotBeNull("the input model should still contain DescribedEntity");

        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(entityType, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;

        var stringValue = annotation.Value.Should().BeAssignableTo<IEdmStringConstantExpression>().Subject;
        stringValue.Value.Should().Be("A described entity.");
    }

    [TestMethod]
    public void GetEdmModel_EmitsCoreDescription_WhenPropertyHasDescriptionAttribute()
    {
        // Arrange
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithDescribedProperty>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert
        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithDescribedProperty).FullName);
        var property = entityType.FindProperty(nameof(EntityWithDescribedProperty.Name));

        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;

        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("The display name of the entity.");
    }

    [TestMethod]
    public void GetEdmModel_EmitsCoreDescription_WhenEdmPropertyNameIsLowerCamelCase()
    {
        // Arrange — EnableLowerCamelCase() makes the EDM property name "name",
        // while the CLR property is "Name" with [Description].
        var inputModel = AnnotationTestFixtures.BuildLowerCamelCaseModelWith<EntityWithDescribedProperty>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert — annotation lands on the camelCased EDM property "name".
        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithDescribedProperty).FullName);
        var property = entityType.FindProperty("name");
        property.Should().NotBeNull("ODataConventionModelBuilder.EnableLowerCamelCase() should rename Name to name");

        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;
        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("The display name of the entity.");
    }

    [TestMethod]
    public void GetEdmModel_EmitsCoreDescription_WhenComplexTypeHasDescriptionAttribute()
    {
        // Arrange
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithComplexProperty>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert
        var complexType = result.FindDeclaredType(typeof(DescribedComplex).FullName);
        complexType.Should().BeAssignableTo<IEdmComplexType>("ODataConventionModelBuilder should infer DescribedComplex as a complex type");

        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(complexType, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;

        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("A postal address.");
    }

    [TestMethod]
    public void GetEdmModel_EmitsCoreDescription_WhenOperationMethodHasDescriptionAttribute()
    {
        // Arrange
        var inputModel = AnnotationTestFixtures.BuildModelWithUnboundFunction(
            namespaceName: "Microsoft.Restier.Tests.AspNetCore.Model",
            functionName: nameof(ApiWithDescribedOperation.CountActive));
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(ApiWithDescribedOperation))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert
        var operation = result.SchemaElements.OfType<IEdmOperation>().Single();
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(operation, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;

        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("Returns the active record count.");
    }

    [TestMethod]
    public void GetEdmModel_EmitsCoreComputed_WhenPropertyIsDatabaseGeneratedIdentity()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithIdentityKey>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithIdentityKey).FullName);
        var property = entityType.FindProperty(nameof(EntityWithIdentityKey.Id));
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreComputedTerm)
            .Should().ContainSingle().Subject;
        ((IEdmBooleanConstantExpression)annotation.Value).Value.Should().BeTrue();
    }

    [TestMethod]
    public void GetEdmModel_EmitsCoreComputed_WhenPropertyIsDatabaseGeneratedComputed()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithComputedProperty>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithComputedProperty).FullName);
        var property = entityType.FindProperty(nameof(EntityWithComputedProperty.UpdatedAt));
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreComputedTerm)
            .Should().ContainSingle().Subject;
        ((IEdmBooleanConstantExpression)annotation.Value).Value.Should().BeTrue();
    }

    [TestMethod]
    public void GetEdmModel_DoesNotEmitCoreComputed_WhenPropertyIsDatabaseGeneratedNone()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithNoneOption>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithNoneOption).FullName);
        var property = entityType.FindProperty(nameof(EntityWithNoneOption.Name));
        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreComputedTerm)
            .Should().BeEmpty();
    }

    [TestMethod]
    public void GetEdmModel_EmitsCoreImmutable_WhenPropertyIsReadOnlyTrue()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithReadOnlyTrue>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithReadOnlyTrue).FullName);
        var property = entityType.FindProperty(nameof(EntityWithReadOnlyTrue.CreatedOn));
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreImmutableTerm)
            .Should().ContainSingle().Subject;
        ((IEdmBooleanConstantExpression)annotation.Value).Value.Should().BeTrue();
    }

    [TestMethod]
    public void GetEdmModel_DoesNotEmitCoreImmutable_WhenPropertyIsReadOnlyFalse()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithReadOnlyFalse>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithReadOnlyFalse).FullName);
        var property = entityType.FindProperty(nameof(EntityWithReadOnlyFalse.Notes));
        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, CoreImmutableTerm)
            .Should().BeEmpty();
    }

    [TestMethod]
    public void GetEdmModel_EmitsIntegerMinMax_WhenIntPropertyHasRangeAttribute()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithIntRange>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithIntRange).FullName);
        var property = entityType.FindProperty(nameof(EntityWithIntRange.Score));

        var min = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMinimumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmIntegerConstantExpression)min.Value).Value.Should().Be(0L);

        var max = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMaximumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmIntegerConstantExpression)max.Value).Value.Should().Be(100L);
    }

    [TestMethod]
    public void GetEdmModel_EmitsFloatingMinMax_WhenDoublePropertyHasRangeAttribute()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithDoubleRange>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithDoubleRange).FullName);
        var property = entityType.FindProperty(nameof(EntityWithDoubleRange.Ratio));

        var min = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMinimumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmFloatingConstantExpression)min.Value).Value.Should().Be(0.0);

        var max = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMaximumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmFloatingConstantExpression)max.Value).Value.Should().Be(1.0);
    }

    [TestMethod]
    public void GetEdmModel_EmitsDecimalMinMax_WhenDecimalPropertyHasRangeAttribute()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithDecimalRange>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithDecimalRange).FullName);
        var property = entityType.FindProperty(nameof(EntityWithDecimalRange.Price));

        var min = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMinimumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmDecimalConstantExpression)min.Value).Value.Should().Be(0.00m);

        var max = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMaximumTerm)
            .Should().ContainSingle().Subject;
        ((IEdmDecimalConstantExpression)max.Value).Value.Should().Be(999.99m);
    }

    [TestMethod]
    public void GetEdmModel_DoesNotEmitMinMax_WhenRangeAppliedToStringProperty()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithRangeOnString>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithRangeOnString).FullName);
        var property = entityType.FindProperty(nameof(EntityWithRangeOnString.Label));

        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMinimumTerm)
            .Should().BeEmpty();
        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationMaximumTerm)
            .Should().BeEmpty();
    }

    [TestMethod]
    public void GetEdmModel_EmitsValidationPattern_WhenPropertyHasRegularExpression()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithRegexProperty>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithRegexProperty).FullName);
        var property = entityType.FindProperty(nameof(EntityWithRegexProperty.CountryCode));

        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, ValidationPatternTerm)
            .Should().ContainSingle().Subject;
        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("^[A-Z]{2}$");
    }

    [TestMethod]
    public void GetEdmModel_DoesNotOverrideExistingDescriptionAnnotation()
    {
        // Arrange — build the model and pre-add a Description annotation manually.
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithExistingAnnotation>();
        var entityType = inputModel.FindDeclaredType(typeof(EntityWithExistingAnnotation).FullName);
        var preExisting = new EdmVocabularyAnnotation(
            entityType,
            Microsoft.OData.Edm.Vocabularies.V1.CoreVocabularyModel.DescriptionTerm,
            new EdmStringConstant("Pre-existing."));
        preExisting.SetSerializationLocation(inputModel, EdmVocabularyAnnotationSerializationLocation.Inline);
        inputModel.AddVocabularyAnnotation(preExisting);

        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert — the pre-existing annotation survives; no second annotation was added.
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(entityType, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;
        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("Pre-existing.");
    }

    [TestMethod]
    public void GetEdmModel_ReturnsNull_WhenInnerIsNull()
    {
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = null,
        };

        sut.GetEdmModel().Should().BeNull();
    }

    [TestMethod]
    public void GetEdmModel_ReturnsNull_WhenInnerReturnsNull()
    {
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(null),
        };

        sut.GetEdmModel().Should().BeNull();
    }

    [TestMethod]
    public void Constructor_Throws_WhenApiTypeIsNull()
    {
        var act = () => new ConventionBasedAnnotationModelBuilder(null);
        act.Should().Throw<ArgumentNullException>().WithParameterName("apiType");
    }

    [TestMethod]
    public void GetEdmModel_DoesNotEmitVocabularyAnnotation_ForMaxLengthAttribute()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWith<EntityWithMaxLength>();
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(AnnotationTestFixtures.StubApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var entityType = (IEdmEntityType)result.FindDeclaredType(typeof(EntityWithMaxLength).FullName);
        var property = entityType.FindProperty(nameof(EntityWithMaxLength.Code));

        // Assert — no Validation.MaxLength vocabulary annotation; structural facet remains.
        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(property, "Org.OData.Validation.V1.MaxLength")
            .Should().BeEmpty();
        property.Type.AsString().MaxLength.Should().Be(13, "the structural facet should still carry the constraint");
    }

    [TestMethod]
    public void GetEdmModel_AnnotatesOperation_WhenMethodIsDeclaredOnBaseClass()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWithUnboundFunction(
            namespaceName: "Microsoft.Restier.Tests.AspNetCore.Model",
            functionName: nameof(BaseApiWithOperation.InheritedOp));
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(DerivedApi))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var operation = result.SchemaElements.OfType<IEdmOperation>().Single();
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(operation, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;
        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("Inherited operation.");
    }

    [TestMethod]
    public void GetEdmModel_AnnotatesOperation_WhenMethodIsProtectedInternal()
    {
        var inputModel = AnnotationTestFixtures.BuildModelWithUnboundFunction(
            namespaceName: "Microsoft.Restier.Tests.AspNetCore.Model",
            functionName: "ProtectedOp");
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(ApiWithProtectedOperation))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        var result = sut.GetEdmModel();

        var operation = result.SchemaElements.OfType<IEdmOperation>().Single();
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(operation, CoreDescriptionTerm)
            .Should().ContainSingle().Subject;
        ((IEdmStringConstantExpression)annotation.Value).Value.Should().Be("Protected operation.");
    }

    [TestMethod]
    public void Constructor_DoesNotIndexSpecialNameMethods_AsOperations()
    {
        // Arrange — feed in a model with a function named "get_Item" (the indexer's getter name).
        var inputModel = AnnotationTestFixtures.BuildModelWithUnboundFunction(
            namespaceName: "Microsoft.Restier.Tests.AspNetCore.Model",
            functionName: "get_Item");
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(ApiWithIndexerProperty))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert — the [Description] on the indexer property should NOT be picked up
        // as an operation description, because get_Item is IsSpecialName.
        var operation = result.SchemaElements.OfType<IEdmOperation>().Single();
        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(operation, CoreDescriptionTerm)
            .Should().BeEmpty();
    }

    [TestMethod]
    public void GetEdmModel_EmitsCoreRevisions_WhenOperationMethodHasObsoleteAttribute()
    {
        // Arrange
        var inputModel = AnnotationTestFixtures.BuildModelWithUnboundFunction(
            namespaceName: "Microsoft.Restier.Tests.AspNetCore.Model",
            functionName: nameof(ApiWithObsoleteOperation.DeprecatedMethod));
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(ApiWithObsoleteOperation))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert — a Core.V1.Revisions annotation must exist on the operation.
        var operation = result.SchemaElements.OfType<IEdmOperation>().Single();
        var annotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(operation, CoreRevisionsTerm)
            .Should().ContainSingle("an [Obsolete] method should emit a Revisions annotation").Subject;

        // The annotation value is a collection with one record entry.
        var collectionExpr = annotation.Value.Should().BeAssignableTo<IEdmCollectionExpression>().Subject;
        var record = collectionExpr.Elements.Should().ContainSingle().Subject
            .Should().BeAssignableTo<IEdmRecordExpression>().Subject;

        // Kind = Deprecated
        var kindConstructor = record.Properties.FirstOrDefault(p => p.Name == "Kind");
        kindConstructor.Should().NotBeNull("the record must have a Kind property");
        var enumExpr = kindConstructor.Value.Should().BeAssignableTo<IEdmEnumMemberExpression>().Subject;
        enumExpr.EnumMembers.Should().ContainSingle(m => m.Name == "Deprecated");

        // Description matches the obsolete message
        var descConstructor = record.Properties.FirstOrDefault(p => p.Name == "Description");
        descConstructor.Should().NotBeNull("the record must have a Description property");
        ((IEdmStringConstantExpression)descConstructor.Value).Value.Should().Be("Use NewMethod instead.");
    }

    [TestMethod]
    public void GetEdmModel_EmitsCoreDescription_WhenOperationParameterHasDescriptionAttribute()
    {
        // Arrange
        var inputModel = AnnotationTestFixtures.BuildModelWithUnboundFunctionWithParameter(
            namespaceName: "Microsoft.Restier.Tests.AspNetCore.Model",
            functionName: nameof(ApiWithDescribedParameter.ParamWithDescription),
            parameterName: "query");
        var sut = new ConventionBasedAnnotationModelBuilder(typeof(ApiWithDescribedParameter))
        {
            Inner = new AnnotationTestFixtures.StaticInnerBuilder(inputModel),
        };

        // Act
        var result = sut.GetEdmModel();

        // Assert — the Description annotation must land on the *parameter*, not the operation.
        var operation = result.SchemaElements.OfType<IEdmOperation>().Single();
        var parameter = operation.FindParameter("query");
        parameter.Should().NotBeNull("the EDM function should have a 'query' parameter");

        // Parameter carries the Description.
        var paramAnnotation = result
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(
                (IEdmVocabularyAnnotatable)parameter, CoreDescriptionTerm)
            .Should().ContainSingle("a parameter with [Description] should get a Core.V1.Description annotation").Subject;
        ((IEdmStringConstantExpression)paramAnnotation.Value).Value.Should().Be("Search string.");

        // The operation itself must NOT receive a Description from the parameter's attribute.
        result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(operation, CoreDescriptionTerm)
            .Should().BeEmpty("the [Description] is on the parameter, not the method");
    }
}
