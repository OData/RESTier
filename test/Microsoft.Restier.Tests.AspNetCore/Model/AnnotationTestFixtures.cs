// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

/// <summary>
/// Helpers and fixture types used by <c>ConventionBasedAnnotationModelBuilderTests</c>.
/// </summary>
internal static class AnnotationTestFixtures
{
    /// <summary>
    /// Builds an <see cref="EdmModel"/> from a single CLR entity type via
    /// <see cref="ODataConventionModelBuilder"/>, which sets <c>ClrTypeAnnotation</c>
    /// on the resulting EDM types.
    /// </summary>
    public static EdmModel BuildModelWith<T>() where T : class
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntityType<T>();
        return (EdmModel)builder.GetEdmModel();
    }

    /// <summary>
    /// Builds an <see cref="EdmModel"/> from a single CLR entity type via
    /// <see cref="ODataConventionModelBuilder"/> with <c>EnableLowerCamelCase</c>
    /// applied, so EDM property names will be lower-camel-case (e.g. "displayName")
    /// while the CLR side keeps PascalCase ("DisplayName").
    /// </summary>
    public static EdmModel BuildLowerCamelCaseModelWith<T>() where T : class
    {
        var builder = new ODataConventionModelBuilder();
        builder.EnableLowerCamelCase();
        builder.EntityType<T>();
        return (EdmModel)builder.GetEdmModel();
    }

    public static EdmModel BuildModelWithUnboundFunction(
        string namespaceName,
        string functionName,
        IEdmTypeReference returnTypeRef = null)
    {
        var model = new EdmModel();
        var container = new EdmEntityContainer(namespaceName, "Default");
        model.AddElement(container);

        returnTypeRef ??= EdmCoreModel.Instance.GetPrimitive(EdmPrimitiveTypeKind.Int32, false);
        var function = new EdmFunction(namespaceName, functionName, returnTypeRef);
        model.AddElement(function);
        container.AddFunctionImport(functionName, function);
        return model;
    }

    /// <summary>
    /// Builds an <see cref="EdmModel"/> with a single unbound function that has one
    /// named string parameter. Used to test parameter-level annotations.
    /// </summary>
    public static EdmModel BuildModelWithUnboundFunctionWithParameter(
        string namespaceName,
        string functionName,
        string parameterName)
    {
        var model = new EdmModel();
        var container = new EdmEntityContainer(namespaceName, "Default");
        model.AddElement(container);

        var returnTypeRef = EdmCoreModel.Instance.GetPrimitive(EdmPrimitiveTypeKind.Int32, false);
        var function = new EdmFunction(namespaceName, functionName, returnTypeRef);
        function.AddParameter(parameterName, EdmCoreModel.Instance.GetPrimitive(EdmPrimitiveTypeKind.String, true));
        model.AddElement(function);
        container.AddFunctionImport(functionName, function);
        return model;
    }

    /// <summary>
    /// Inner builder that returns a fixed model. Used to feed a known input model
    /// into the system-under-test without invoking the real RESTier chain.
    /// </summary>
    public sealed class StaticInnerBuilder : IModelBuilder
    {
        private readonly IEdmModel model;

        public StaticInnerBuilder(IEdmModel model) => this.model = model;

        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel() => model;
    }

    /// <summary>
    /// Stub API class used as the <c>apiType</c> argument to the system-under-test.
    /// Only the type metadata (via <c>typeof(StubApi)</c>) is consumed by the builder;
    /// the constructor is never invoked at runtime by the tests, so the <see langword="null"/>
    /// arguments to <see cref="Microsoft.Restier.Core.ApiBase"/> are safe in practice.
    /// </summary>
    public class StubApi : ApiBase
    {
        // Constructor is never executed; only typeof(StubApi) is used by the operation index.
        public StubApi() : base(null, null, null) { }
    }
}

[Description("A described entity.")]
internal class DescribedEntity
{
    public int Id { get; set; }
}

internal class EntityWithDescribedProperty
{
    public int Id { get; set; }

    [System.ComponentModel.Description("The display name of the entity.")]
    public string Name { get; set; }
}

[System.ComponentModel.Description("A postal address.")]
internal class DescribedComplex
{
    public string Street { get; set; }

    public string Zip { get; set; }
}

internal class EntityWithComplexProperty
{
    public int Id { get; set; }

    public DescribedComplex Address { get; set; }
}

internal class ApiWithDescribedOperation : ApiBase
{
    public ApiWithDescribedOperation() : base(null, null, null) { }

    [Microsoft.Restier.AspNetCore.Model.UnboundOperation]
    [System.ComponentModel.Description("Returns the active record count.")]
    public int CountActive() => 0;
}

internal class EntityWithIdentityKey
{
    [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(
        System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
}

internal class EntityWithComputedProperty
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(
        System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Computed)]
    public System.DateTime UpdatedAt { get; set; }
}

internal class EntityWithNoneOption
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(
        System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
    public string Name { get; set; }
}

internal class EntityWithReadOnlyTrue
{
    public int Id { get; set; }

    [System.ComponentModel.ReadOnly(true)]
    public System.DateTimeOffset CreatedOn { get; set; }
}

internal class EntityWithReadOnlyFalse
{
    public int Id { get; set; }

    [System.ComponentModel.ReadOnly(false)]
    public string Notes { get; set; }
}

internal class EntityWithIntRange
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0, 100)]
    public int Score { get; set; }
}

internal class EntityWithDoubleRange
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0.0, 1.0)]
    public double Ratio { get; set; }
}

internal class EntityWithDecimalRange
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0.00", "999.99")]
    public decimal Price { get; set; }
}

internal class EntityWithRangeOnString
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0, 10)]
    public string Label { get; set; }
}

internal class EntityWithRegexProperty
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.RegularExpression("^[A-Z]{2}$")]
    public string CountryCode { get; set; }
}

[System.ComponentModel.Description("From attribute.")]
internal class EntityWithExistingAnnotation
{
    public int Id { get; set; }
}

internal class EntityWithMaxLength
{
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(13)]
    public string Code { get; set; }
}

internal class BaseApiWithOperation : ApiBase
{
    public BaseApiWithOperation() : base(null, null, null) { }

    [Microsoft.Restier.AspNetCore.Model.UnboundOperation]
    [System.ComponentModel.Description("Inherited operation.")]
    public int InheritedOp() => 0;
}

internal class DerivedApi : BaseApiWithOperation
{
    public DerivedApi() : base() { }
}

internal class ApiWithProtectedOperation : ApiBase
{
    public ApiWithProtectedOperation() : base(null, null, null) { }

    [Microsoft.Restier.AspNetCore.Model.UnboundOperation]
    [System.ComponentModel.Description("Protected operation.")]
    protected internal int ProtectedOp() => 0;
}

internal class ApiWithIndexerProperty : ApiBase
{
    public ApiWithIndexerProperty() : base(null, null, null) { }

    // The compiler-emitted get_Item method has IsSpecialName=true.
    // We deliberately apply [UnboundOperation] and [Description] to the *get
    // accessor* (not the property) so they land on the synthesized get_Item
    // method. Without the IsSpecialName guard in the operation scan, this
    // would be picked up as an operation and annotated.
    public int this[int i]
    {
        [Microsoft.Restier.AspNetCore.Model.UnboundOperation]
        [System.ComponentModel.Description("Should not be treated as an operation.")]
        get => 0;
    }
}

internal class ApiWithObsoleteOperation : ApiBase
{
    public ApiWithObsoleteOperation() : base(null, null, null) { }

    [Microsoft.Restier.AspNetCore.Model.UnboundOperation]
    [Obsolete("Use NewMethod instead.")]
    public int DeprecatedMethod() => 0;
}

internal class ApiWithDescribedParameter : ApiBase
{
    public ApiWithDescribedParameter() : base(null, null, null) { }

    [Microsoft.Restier.AspNetCore.Model.UnboundOperation]
    public int ParamWithDescription([System.ComponentModel.Description("Search string.")] string query) => 0;
}
