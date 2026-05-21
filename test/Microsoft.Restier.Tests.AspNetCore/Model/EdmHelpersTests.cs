// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Model
{
    public class EdmHelpersTests
    {
        private readonly EdmModel _model = new EdmModel();

        [Fact]
        public void GetTypeReference_ValueType_DefaultsToNullableFalse()
        {
            // The two-arg overload preserves original behavior:
            // plain value types are non-nullable; only Nullable<T> or reference types are nullable.
            var reference = typeof(int).GetTypeReference(_model);
            reference.Should().NotBeNull();
            reference.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void GetTypeReference_ValueType_NullableFalseOverload_EmitsNonNullable()
        {
            var reference = typeof(int).GetTypeReference(_model, nullable: false);
            reference.Should().NotBeNull();
            reference.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void GetTypeReference_ValueType_NullableTrueOverload_EmitsNullable()
        {
            var reference = typeof(int).GetTypeReference(_model, nullable: true);
            reference.Should().NotBeNull();
            reference.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void GetTypeReference_NullableValueType_AlwaysNullable()
        {
            // Nullable<T> wraps the underlying primitive; the type ref is always nullable.
            var reference = typeof(int?).GetTypeReference(_model, nullable: false);
            reference.Should().NotBeNull();
            reference.IsNullable.Should().BeTrue();
        }

        // Issue #766: facet-bearing primitives must be emitted as their specific
        // IEdm*TypeReference subtype so downstream consumers (e.g. Microsoft.OpenApi.OData
        // schema generation) can hard-cast to those interfaces without InvalidCastException.

        [Fact]
        public void GetPrimitiveTypeReference_String_ReturnsStringTypeReference()
        {
            var reference = typeof(string).GetPrimitiveTypeReference();
            reference.Should().BeAssignableTo<IEdmStringTypeReference>();
        }

        [Fact]
        public void GetPrimitiveTypeReference_ByteArray_ReturnsBinaryTypeReference()
        {
            var reference = typeof(byte[]).GetPrimitiveTypeReference();
            reference.Should().BeAssignableTo<IEdmBinaryTypeReference>();
        }

        [Fact]
        public void GetPrimitiveTypeReference_Decimal_ReturnsDecimalTypeReference()
        {
            var reference = typeof(decimal).GetPrimitiveTypeReference();
            reference.Should().BeAssignableTo<IEdmDecimalTypeReference>();
        }

        [Theory]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(TimeSpan))]
        [InlineData(typeof(TimeOnly))]
        public void GetPrimitiveTypeReference_Temporal_ReturnsTemporalTypeReference(Type clrType)
        {
            var reference = clrType.GetPrimitiveTypeReference();
            reference.Should().BeAssignableTo<IEdmTemporalTypeReference>();
        }

        [Fact]
        public void GetPrimitiveTypeReference_Int32_ReturnsPlainPrimitiveTypeReference()
        {
            // Int32 has no facets — a bare EdmPrimitiveTypeReference is fine and expected.
            var reference = typeof(int).GetPrimitiveTypeReference();
            reference.Should().NotBeNull();
            reference.PrimitiveKind().Should().Be(EdmPrimitiveTypeKind.Int32);
        }
    }
}
