// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using RestierOptional = Microsoft.Restier.AspNetCore.Model.OptionalAttribute;

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// Shared classification helpers for RESTier operation parameters.
    /// Used at build time by <see cref="RestierWebApiOperationModelBuilder"/>
    /// and at request time by <see cref="Microsoft.Restier.AspNetCore.Operation.RestierOperationExecutor"/>.
    /// </summary>
    /// <remarks>
    /// Nullability (the EDM type reference accepts <c>null</c> as a value) and
    /// optionality (the parameter may be omitted from the URL, in which case a
    /// declared default applies) are independent signals. See the magical-operations
    /// design spec for the full semantics table.
    /// </remarks>
    public static class OperationParameterClassifier
    {
        /// <summary>
        /// Returns <see langword="true"/> when the EDM type reference for this parameter
        /// should be emitted with <c>Nullable = true</c>. Driven purely by whether the
        /// CLR type can hold <see langword="null"/>; the <see cref="OptionalAttribute"/>
        /// itself does not change type-ref nullability, only optionality.
        /// </summary>
        public static bool ComputeNullable(ParameterInfo parameter)
        {
            Ensure.NotNull(parameter, nameof(parameter));
            return CanHoldNull(parameter);
        }

        /// <summary>
        /// Classifies whether the parameter is omittable (an <c>EdmOptionalParameter</c>)
        /// and returns the literal string used as the EDM default-value attribute.
        /// </summary>
        /// <remarks>
        /// <see cref="OptionalAttribute"/> on a non-nullable value type with no default value
        /// is rejected (treated as required) with a <see cref="Trace.TraceWarning(string)"/>:
        /// such a parameter cannot represent the omitted-state at runtime because
        /// <c>MethodInfo.Invoke</c> cannot pass <see langword="null"/> for a non-nullable
        /// value-type slot.
        /// </remarks>
        public static (bool IsOptional, string DefaultLiteral) ClassifyOptionality(ParameterInfo parameter)
        {
            Ensure.NotNull(parameter, nameof(parameter));

            var attr = parameter.GetCustomAttribute<DefaultValueAttribute>(true);
            if (attr is not null)
            {
                return (true, FormatLiteral(attr.Value));
            }

            if (parameter.HasDefaultValue)
            {
                return (true, FormatLiteral(parameter.DefaultValue));
            }

            if (parameter.GetCustomAttribute<RestierOptional>(true) is not null)
            {
                if (CanHoldNull(parameter))
                {
                    return (true, "null");
                }

                Trace.TraceWarning(
                    $"Restier: Parameter '{parameter.Name}' on '{parameter.Member?.DeclaringType?.FullName}.{parameter.Member?.Name}' " +
                    $"is marked [Optional] but its type '{parameter.ParameterType.Name}' is a non-nullable value type with no default value, " +
                    $"so the parameter cannot be omitted from the request. Treating as required. " +
                    $"To make it omittable, give it a default value (e.g. '{parameter.ParameterType.Name} {parameter.Name} = default') " +
                    $"or change the type to '{parameter.ParameterType.Name}?'.");
                return (false, null);
            }

            return (false, null);
        }

        private static bool CanHoldNull(ParameterInfo parameter)
            => Nullable.GetUnderlyingType(parameter.ParameterType) is not null
               || parameter.ParameterType.IsClass;

        /// <summary>
        /// Returns <see langword="true"/> when this parameter, if absent from a request,
        /// should be substituted with its declared default rather than passed as null.
        /// </summary>
        public static bool IsOmittedOptional(ParameterInfo parameter)
            => ClassifyOptionality(parameter).IsOptional;

        /// <summary>
        /// Resolves the runtime CLR default value for an omitted optional parameter.
        /// </summary>
        /// <returns>
        /// The <c>[DefaultValue]</c> attribute value when present, then
        /// <see cref="ParameterInfo.DefaultValue"/> when supplied by the compiler,
        /// then <see langword="null"/>.
        /// </returns>
        public static object ResolveDefault(ParameterInfo parameter)
        {
            Ensure.NotNull(parameter, nameof(parameter));
            var attr = parameter.GetCustomAttribute<DefaultValueAttribute>(true);
            if (attr is not null)
            {
                return attr.Value;
            }

            if (parameter.HasDefaultValue)
            {
                return parameter.DefaultValue;
            }

            return null;
        }

        private static string FormatLiteral(object value)
        {
            if (value is null)
            {
                return "null";
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }
    }
}
