// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// Marks a RESTier operation parameter as optional in the EDM model.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Apply this attribute to a parameter of a method decorated with
    /// <see cref="BoundOperationAttribute"/> or <see cref="UnboundOperationAttribute"/>
    /// to declare that the parameter may be omitted from the URL. The resulting
    /// EDM parameter is emitted as an <c>EdmOptionalParameter</c> with a <c>null</c>
    /// default literal, and the parameter type reference is emitted with <c>Nullable = true</c>.
    /// </para>
    /// <para>
    /// Use this attribute when neither <c>Nullable&lt;T&gt;</c> nor a compile-time
    /// default value can express the intent — typically for reference-type parameters
    /// under nullable-reference-types-disabled compilation, or when the absence of
    /// the parameter should produce a <c>null</c> CLR argument at invocation time.
    /// </para>
    /// <para>
    /// This attribute is intentionally distinct from
    /// <see cref="System.Runtime.InteropServices.OptionalAttribute"/>. Use the
    /// fully qualified name <c>Microsoft.Restier.AspNetCore.Model.OptionalAttribute</c>
    /// when both namespaces are in scope.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class OptionalAttribute : Attribute
    {
    }
}
