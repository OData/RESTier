// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Reflection;
using FluentAssertions;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Tests.Shared;
using Xunit;
using RestierOptional = Microsoft.Restier.AspNetCore.Model.OptionalAttribute;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

public class OperationParameterClassifierTests
{
    private static ParameterInfo Param(string methodName, int index = 0)
        => typeof(SampleParameters).GetMethod(methodName)!.GetParameters()[index];

    [Fact]
    public void ComputeNullable_ReturnsTrue_ForNullableValueType()
    {
        OperationParameterClassifier.ComputeNullable(Param(nameof(SampleParameters.NullableInt)))
            .Should().BeTrue();
    }

    [Fact]
    public void ComputeNullable_ReturnsTrue_ForReferenceType()
    {
        OperationParameterClassifier.ComputeNullable(Param(nameof(SampleParameters.ReferenceString)))
            .Should().BeTrue();
    }

    [Fact]
    public void ComputeNullable_ReturnsTrue_ForOptionalAttributeOnNullableValueType()
    {
        // [Optional] int? p — nullable via the underlying Nullable<T>, not via the attribute.
        OperationParameterClassifier.ComputeNullable(Param(nameof(SampleParameters.OptionalNullableInt)))
            .Should().BeTrue();
    }

    [Fact]
    public void ComputeNullable_ReturnsFalse_ForOptionalAttributeOnNonNullableValueType()
    {
        // [Optional] int p — non-nullable value type. The attribute alone does not
        // make the type ref nullable; warn+require fires in ClassifyOptionality instead.
        OperationParameterClassifier.ComputeNullable(Param(nameof(SampleParameters.OptionalPlainInt)))
            .Should().BeFalse();
    }

    [Fact]
    public void ComputeNullable_ReturnsFalse_ForBareValueType()
    {
        OperationParameterClassifier.ComputeNullable(Param(nameof(SampleParameters.PlainInt)))
            .Should().BeFalse();
    }

    [Fact]
    public void ClassifyOptionality_ReturnsRequired_ForBareValueType()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.PlainInt)));
        isOptional.Should().BeFalse();
        literal.Should().BeNull();
    }

    [Fact]
    public void ClassifyOptionality_ReturnsRequired_ForBareNullable_NoDefault()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.NullableInt)));
        isOptional.Should().BeFalse();
        literal.Should().BeNull();
    }

    [Fact]
    public void ClassifyOptionality_ReturnsOptional_ForCompilerDefault()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.IntWithDefault)));
        isOptional.Should().BeTrue();
        literal.Should().Be("5");
    }

    [Fact]
    public void ClassifyOptionality_ReturnsOptional_ForDefaultValueAttribute()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.StringWithDefaultValueAttribute)));
        isOptional.Should().BeTrue();
        literal.Should().Be("hello");
    }

    [Fact]
    public void ClassifyOptionality_DefaultValueAttribute_OverridesCompilerDefault()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.AttributeOverridesCompilerDefault)));
        isOptional.Should().BeTrue();
        literal.Should().Be("attribute");
    }

    [Fact]
    public void ClassifyOptionality_ReturnsOptional_ForOptionalAttribute()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.OptionalNullableInt)));
        isOptional.Should().BeTrue();
        literal.Should().Be("null");
    }

    [Fact]
    public void ClassifyOptionality_OptionalOnNonNullableValueType_WarnsAndReturnsRequired()
    {
        // [Optional] int p (no default) — the attribute cannot be honored because the
        // CLR slot cannot hold null and there is no declared default. Warn and demote.
        var testTraceListener = new TestTraceListener();
        Trace.Listeners.Add(testTraceListener);
        try
        {
            var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(
                Param(nameof(SampleParameters.OptionalPlainInt)));

            isOptional.Should().BeFalse();
            literal.Should().BeNull();
            testTraceListener.Messages.Should()
                .Contain("non-nullable value type")
                .And.Contain("Treating as required");
        }
        finally
        {
            Trace.Listeners.Remove(testTraceListener);
        }
    }

    [Fact]
    public void ClassifyOptionality_OptionalOnNonNullableValueTypeWithDefault_StaysOptional()
    {
        // [Optional] int p = 5 — the compiler default rescues optionality;
        // the [Optional] attribute is redundant but does not trigger the warning.
        var testTraceListener = new TestTraceListener();
        Trace.Listeners.Add(testTraceListener);
        try
        {
            var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(
                Param(nameof(SampleParameters.OptionalPlainIntWithDefault)));

            isOptional.Should().BeTrue();
            literal.Should().Be("5");
            testTraceListener.Messages.Should().NotContain("Treating as required");
        }
        finally
        {
            Trace.Listeners.Remove(testTraceListener);
        }
    }

    [Fact]
    public void ClassifyOptionality_NullCompilerDefault_EmitsNullLiteral()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.NullableIntWithNullDefault)));
        isOptional.Should().BeTrue();
        literal.Should().Be("null");
    }

    [Fact]
    public void IsOmittedOptional_MirrorsClassifyOptionality()
    {
        OperationParameterClassifier.IsOmittedOptional(Param(nameof(SampleParameters.IntWithDefault))).Should().BeTrue();
        OperationParameterClassifier.IsOmittedOptional(Param(nameof(SampleParameters.PlainInt))).Should().BeFalse();
    }

    [Fact]
    public void ResolveDefault_ReturnsAttributeValue_WhenPresent()
    {
        OperationParameterClassifier.ResolveDefault(Param(nameof(SampleParameters.StringWithDefaultValueAttribute)))
            .Should().Be("hello");
    }

    [Fact]
    public void ResolveDefault_ReturnsCompilerDefault_WhenNoAttribute()
    {
        OperationParameterClassifier.ResolveDefault(Param(nameof(SampleParameters.IntWithDefault)))
            .Should().Be(5);
    }

    [Fact]
    public void ResolveDefault_ReturnsNull_ForOptionalAttribute_Only()
    {
        OperationParameterClassifier.ResolveDefault(Param(nameof(SampleParameters.OptionalNullableInt)))
            .Should().BeNull();
    }

    public class SampleParameters
    {
        public void PlainInt(int p) { }
        public void NullableInt(int? p) { }
        public void NullableIntWithNullDefault(int? p = null) { }
        public void IntWithDefault(int p = 5) { }
        public void ReferenceString(string p) { }
        public void StringWithDefaultValueAttribute([System.ComponentModel.DefaultValue("hello")] string p) { }
        public void AttributeOverridesCompilerDefault([System.ComponentModel.DefaultValue("attribute")] string p = "compiler") { }
        public void OptionalNullableInt([RestierOptional] int? p) { }
        public void OptionalPlainInt([RestierOptional] int p) { }
        public void OptionalPlainIntWithDefault([RestierOptional] int p = 5) { }
    }
}
