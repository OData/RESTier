// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.Edm.Vocabularies.V1;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.AspNetCore.Model;

/// <summary>
/// A chained <see cref="IModelBuilder"/> that scans CLR types referenced by the
/// EDM model for .NET attributes and emits the corresponding OData vocabulary annotations.
/// </summary>
/// <remarks>
/// Runs last in the model-building chain so it can annotate every entity, complex
/// type, property, and operation contributed by inner builders. Annotations are
/// written inline so they appear on their target element in <c>$metadata</c>,
/// allowing OpenAPI generators to surface them as descriptions, computed flags,
/// and validation hints.
/// </remarks>
public class ConventionBasedAnnotationModelBuilder : IModelBuilder
{
    private static readonly IEdmTerm ValidationMinimumTerm =
        ValidationVocabularyModel.Instance.FindDeclaredTerm("Org.OData.Validation.V1.Minimum");
    private static readonly IEdmTerm ValidationMaximumTerm =
        ValidationVocabularyModel.Instance.FindDeclaredTerm("Org.OData.Validation.V1.Maximum");
    private static readonly IEdmTerm ValidationPatternTerm =
        ValidationVocabularyModel.Instance.FindDeclaredTerm("Org.OData.Validation.V1.Pattern");
    private static readonly IEdmTerm RevisionsTerm =
        CoreVocabularyModel.Instance.FindDeclaredTerm("Org.OData.Core.V1.Revisions");

    private readonly Type apiType;
    private readonly Dictionary<string, MethodInfo> operationMethods;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConventionBasedAnnotationModelBuilder"/> class.
    /// </summary>
    /// <param name="apiType">The <see cref="Microsoft.Restier.Core.ApiBase"/>-derived type whose declared operations are scanned for annotation attributes. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="apiType"/> is <see langword="null"/>.</exception>
    public ConventionBasedAnnotationModelBuilder(Type apiType)
    {
        Ensure.NotNull(apiType, nameof(apiType));
        this.apiType = apiType;
        this.operationMethods = BuildOperationIndex(apiType);
    }

    private static Dictionary<string, MethodInfo> BuildOperationIndex(Type apiType)
    {
        var index = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        var methods = apiType
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Public
                      | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object));

        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<OperationAttribute>(inherit: true) is null)
            {
                continue;
            }

            // EDM operation name is the C# method name. The operation attributes
            // do not currently expose a Name override.
            index.TryAdd(method.Name, method);
        }

        return index;
    }

    /// <summary>
    /// Gets or sets the inner model builder in the chain of responsibility.
    /// </summary>
    public IModelBuilder Inner { get; set; }

    /// <inheritdoc />
    public IEdmModel GetEdmModel()
    {
        var inner = Inner?.GetEdmModel();
        if (inner is not EdmModel model)
        {
            // Annotation enrichment requires EdmModel APIs (AddVocabularyAnnotation).
            // If the inner model is null or a different IEdmModel implementation,
            // pass it through unchanged so the chain is preserved.
            return inner;
        }

        ApplyAnnotations(model);
        return model;
    }

    private void ApplyAnnotations(EdmModel model)
    {
        foreach (var schemaType in model.SchemaElements.OfType<IEdmSchemaType>())
        {
            if (schemaType is not IEdmStructuredType structuredType)
            {
                continue;
            }

            var clrType = model.GetAnnotationValue<ClrTypeAnnotation>(schemaType)?.ClrType;
            if (clrType is null)
            {
                continue;
            }

            ApplyDescription(model, schemaType, clrType);
            ApplyPropertyAnnotations(model, structuredType, clrType);
        }

        foreach (var operation in model.SchemaElements.OfType<IEdmOperation>())
        {
            if (!operationMethods.TryGetValue(operation.Name, out var method))
            {
                continue;
            }

            ApplyDescription(model, operation, method);
            ApplyRevisions(model, operation, method);
            ApplyParameterAnnotations(model, operation, method);
        }
    }

    private static void ApplyPropertyAnnotations(
        EdmModel model,
        IEdmStructuredType structuredType,
        Type clrType)
    {
        foreach (var edmProperty in structuredType.DeclaredProperties)
        {
            // Resolve the CLR property name through the EDM->CLR mapper.
            // This honors EnableLowerCamelCase() and any EDM-name overrides
            // by reading ClrPropertyInfoAnnotation set by ODataConventionModelBuilder,
            // and matches the resolution path the submit pipeline already uses.
            var clrName = EdmClrPropertyMapper.GetClrPropertyName(edmProperty, model);
            var clrProperty = clrType.GetProperty(
                clrName,
                BindingFlags.Public | BindingFlags.Instance);
            if (clrProperty is null)
            {
                continue;
            }

            ApplyDescription(model, edmProperty, clrProperty);
            ApplyComputed(model, edmProperty, clrProperty);
            ApplyImmutable(model, edmProperty, clrProperty);
            ApplyRange(model, edmProperty, clrProperty);
            ApplyPattern(model, edmProperty, clrProperty);
        }
    }

    private static void ApplyPattern(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        PropertyInfo clrProperty)
    {
        var attr = clrProperty.GetCustomAttribute<RegularExpressionAttribute>(inherit: true);
        if (attr is null || string.IsNullOrEmpty(attr.Pattern))
        {
            return;
        }

        if (HasAnnotation(model, target, ValidationPatternTerm))
        {
            return;
        }

        var annotation = new EdmVocabularyAnnotation(
            target,
            ValidationPatternTerm,
            new EdmStringConstant(attr.Pattern));
        annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
        model.AddVocabularyAnnotation(annotation);
    }

    private static void ApplyRange(
        EdmModel model,
        IEdmProperty edmProperty,
        PropertyInfo clrProperty)
    {
        var attr = clrProperty.GetCustomAttribute<RangeAttribute>(inherit: true);
        if (attr is null)
        {
            return;
        }

        if (edmProperty.Type.Definition is not IEdmPrimitiveType primitive)
        {
            Trace.TraceWarning(
                "ConventionBasedAnnotationModelBuilder: [Range] on '{0}.{1}' is not a primitive property; skipping.",
                clrProperty.DeclaringType?.FullName, clrProperty.Name);
            return;
        }

        EmitRangeBound(model, edmProperty, primitive, attr.Minimum, ValidationMinimumTerm, clrProperty);
        EmitRangeBound(model, edmProperty, primitive, attr.Maximum, ValidationMaximumTerm, clrProperty);
    }

    private static void EmitRangeBound(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        IEdmPrimitiveType primitive,
        object boundValue,
        IEdmTerm term,
        PropertyInfo clrProperty)
    {
        if (boundValue is null)
        {
            return;
        }

        if (HasAnnotation(model, target, term))
        {
            return;
        }

        IEdmExpression expression;
        try
        {
            switch (primitive.PrimitiveKind)
            {
                case EdmPrimitiveTypeKind.Byte:
                case EdmPrimitiveTypeKind.SByte:
                case EdmPrimitiveTypeKind.Int16:
                case EdmPrimitiveTypeKind.Int32:
                case EdmPrimitiveTypeKind.Int64:
                    expression = new EdmIntegerConstant(
                        Convert.ToInt64(boundValue, CultureInfo.InvariantCulture));
                    break;
                case EdmPrimitiveTypeKind.Single:
                case EdmPrimitiveTypeKind.Double:
                    expression = new EdmFloatingConstant(
                        Convert.ToDouble(boundValue, CultureInfo.InvariantCulture));
                    break;
                case EdmPrimitiveTypeKind.Decimal:
                    expression = new EdmDecimalConstant(
                        Convert.ToDecimal(boundValue, CultureInfo.InvariantCulture));
                    break;
                default:
                    Trace.TraceWarning(
                        "ConventionBasedAnnotationModelBuilder: [Range] on '{0}.{1}' targets primitive kind {2}, which is not supported; skipping.",
                        clrProperty.DeclaringType?.FullName, clrProperty.Name, primitive.PrimitiveKind);
                    return;
            }
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            Trace.TraceWarning(
                "ConventionBasedAnnotationModelBuilder: [Range] value '{0}' on '{1}.{2}' could not be converted: {3}; skipping.",
                boundValue, clrProperty.DeclaringType?.FullName, clrProperty.Name, ex.Message);
            return;
        }

        var annotation = new EdmVocabularyAnnotation(target, term, expression);
        annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
        model.AddVocabularyAnnotation(annotation);
    }

    private static void ApplyImmutable(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        PropertyInfo clrProperty)
    {
        var attr = clrProperty.GetCustomAttribute<ReadOnlyAttribute>(inherit: true);
        if (attr is null || !attr.IsReadOnly)
        {
            return;
        }

        if (HasAnnotation(model, target, CoreVocabularyModel.ImmutableTerm))
        {
            return;
        }

        var annotation = new EdmVocabularyAnnotation(
            target,
            CoreVocabularyModel.ImmutableTerm,
            new EdmBooleanConstant(true));
        annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
        model.AddVocabularyAnnotation(annotation);
    }

    private static void ApplyComputed(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        PropertyInfo clrProperty)
    {
        var attr = clrProperty.GetCustomAttribute<DatabaseGeneratedAttribute>(inherit: true);
        if (attr is null || attr.DatabaseGeneratedOption == DatabaseGeneratedOption.None)
        {
            return;
        }

        if (HasAnnotation(model, target, CoreVocabularyModel.ComputedTerm))
        {
            return;
        }

        var annotation = new EdmVocabularyAnnotation(
            target,
            CoreVocabularyModel.ComputedTerm,
            new EdmBooleanConstant(true));
        annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
        model.AddVocabularyAnnotation(annotation);
    }

    private static void ApplyDescription(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        MemberInfo clrMember)
    {
        var description = clrMember.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;
        if (string.IsNullOrEmpty(description))
        {
            return;
        }

        if (HasAnnotation(model, target, CoreVocabularyModel.DescriptionTerm))
        {
            return;
        }

        var annotation = new EdmVocabularyAnnotation(
            target,
            CoreVocabularyModel.DescriptionTerm,
            new EdmStringConstant(description));
        annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
        model.AddVocabularyAnnotation(annotation);
    }

    private static void ApplyDescription(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        ParameterInfo clrParameter)
    {
        var description = clrParameter.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;
        if (string.IsNullOrEmpty(description))
        {
            return;
        }

        if (HasAnnotation(model, target, CoreVocabularyModel.DescriptionTerm))
        {
            return;
        }

        var annotation = new EdmVocabularyAnnotation(
            target,
            CoreVocabularyModel.DescriptionTerm,
            new EdmStringConstant(description));
        annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
        model.AddVocabularyAnnotation(annotation);
    }

    private static void ApplyRevisions(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        MemberInfo clrMember)
    {
        if (RevisionsTerm is null)
        {
            return;
        }

        var obsolete = clrMember.GetCustomAttribute<ObsoleteAttribute>(inherit: true);
        if (obsolete is null)
        {
            return;
        }

        if (HasAnnotation(model, target, RevisionsTerm))
        {
            return;
        }

        try
        {
            var deprecatedMember = ResolveDeprecatedMember();
            if (deprecatedMember is null)
            {
                Trace.TraceWarning(
                    "ConventionBasedAnnotationModelBuilder: Could not resolve Core.V1.RevisionKind/Deprecated member; skipping [Obsolete] annotation for '{0}'.",
                    (target as IEdmNamedElement)?.Name);
                return;
            }

            var record = new EdmRecordExpression(
                new EdmPropertyConstructor("Version", new EdmStringConstant("obsolete")),
                new EdmPropertyConstructor("Kind", new EdmEnumMemberExpression(deprecatedMember)),
                new EdmPropertyConstructor("Description",
                    new EdmStringConstant(obsolete.Message ?? "Deprecated.")));

            var annotation = new EdmVocabularyAnnotation(
                target,
                RevisionsTerm,
                new EdmCollectionExpression(record));
            annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
            model.AddVocabularyAnnotation(annotation);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(
                "ConventionBasedAnnotationModelBuilder: Failed to emit [Obsolete] annotation: {0}", ex.Message);
        }
    }

    private static void ApplyRevisions(
        EdmModel model,
        IEdmVocabularyAnnotatable target,
        ParameterInfo clrParameter)
    {
        if (RevisionsTerm is null)
        {
            return;
        }

        var obsolete = clrParameter.GetCustomAttribute<ObsoleteAttribute>(inherit: true);
        if (obsolete is null)
        {
            return;
        }

        if (HasAnnotation(model, target, RevisionsTerm))
        {
            return;
        }

        try
        {
            var deprecatedMember = ResolveDeprecatedMember();
            if (deprecatedMember is null)
            {
                Trace.TraceWarning(
                    "ConventionBasedAnnotationModelBuilder: Could not resolve Core.V1.RevisionKind/Deprecated member; skipping [Obsolete] annotation for parameter '{0}'.",
                    clrParameter.Name);
                return;
            }

            var record = new EdmRecordExpression(
                new EdmPropertyConstructor("Version", new EdmStringConstant("obsolete")),
                new EdmPropertyConstructor("Kind", new EdmEnumMemberExpression(deprecatedMember)),
                new EdmPropertyConstructor("Description",
                    new EdmStringConstant(obsolete.Message ?? "Deprecated.")));

            var annotation = new EdmVocabularyAnnotation(
                target,
                RevisionsTerm,
                new EdmCollectionExpression(record));
            annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
            model.AddVocabularyAnnotation(annotation);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning(
                "ConventionBasedAnnotationModelBuilder: Failed to emit [Obsolete] annotation: {0}", ex.Message);
        }
    }

    private static IEdmEnumMember ResolveDeprecatedMember()
    {
        // Core.V1.Revisions is a Collection(Core.V1.RevisionType). RevisionType has a Kind
        // property whose type is the Core.V1.RevisionKind enum with members Added/Modified/Deprecated.
        if (RevisionsTerm?.Type.AsCollection().ElementType().Definition is not IEdmComplexType revisionType)
        {
            return null;
        }

        var kindProperty = revisionType.FindProperty("Kind");
        if (kindProperty?.Type.Definition is not IEdmEnumType kindEnum)
        {
            return null;
        }

        return kindEnum.Members.FirstOrDefault(
            m => string.Equals(m.Name, "Deprecated", StringComparison.Ordinal));
    }

    private static void ApplyParameterAnnotations(
        EdmModel model,
        IEdmOperation operation,
        MethodInfo method)
    {
        var clrParameters = method.GetParameters();
        foreach (var edmParameter in operation.Parameters)
        {
            var clrParameter = clrParameters.FirstOrDefault(p => p.Name == edmParameter.Name);
            if (clrParameter is null)
            {
                continue;
            }

            ApplyDescription(model, (IEdmVocabularyAnnotatable)edmParameter, clrParameter);
            ApplyRevisions(model, (IEdmVocabularyAnnotatable)edmParameter, clrParameter);
        }
    }

    private static bool HasAnnotation(IEdmModel model, IEdmVocabularyAnnotatable target, IEdmTerm term)
    {
        return model
            .FindVocabularyAnnotations<IEdmVocabularyAnnotation>(target, term.FullName())
            .Any();
    }
}
