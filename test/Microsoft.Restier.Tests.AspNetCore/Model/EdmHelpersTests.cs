// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

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
    }
}
