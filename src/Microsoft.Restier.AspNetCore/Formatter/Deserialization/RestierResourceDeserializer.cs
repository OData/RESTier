// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData;
using Microsoft.OData.Edm;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Formatter
{
    /// <summary>
    /// A custom OData resource deserializer that fixes property name mapping when
    /// <c>EnableLowerCamelCase</c> is active. The base <see cref="ODataResourceDeserializer"/>
    /// uses <c>ClrPropertyInfoAnnotation</c> to resolve CLR property names, then passes those
    /// PascalCase names to <see cref="EdmStructuredObject.TrySetPropertyValue"/>.
    /// But <c>EdmStructuredObject</c> validates property names against the EDM type, which has
    /// camelCase names — causing a silent mismatch.
    /// This override lets the base class handle all value materialization (complex objects,
    /// collections, enums, etc.), then detects if the property was silently dropped and
    /// re-applies it using the EDM property name.
    /// </summary>
    internal class RestierResourceDeserializer : ODataResourceDeserializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierResourceDeserializer"/> class.
        /// </summary>
        /// <param name="deserializerProvider">The deserializer provider.</param>
        public RestierResourceDeserializer(IODataDeserializerProvider deserializerProvider)
            : base(deserializerProvider)
        {
        }

        /// <inheritdoc />
        public override void ApplyStructuralProperty(object resource, ODataProperty structuralProperty,
            IEdmStructuredTypeReference structuredType, ODataDeserializerContext readContext)
        {
            if (resource is EdmStructuredObject edmObject)
            {
                // Snapshot which properties are set before the base call
                var propsBefore = edmObject.GetChangedPropertyNames().ToHashSet();

                // Let the base class do all value materialization (complex objects, collections,
                // enums, nested resources, etc.). It resolves the CLR property name via
                // ClrPropertyInfoAnnotation and calls TrySetPropertyValue with that CLR name.
                base.ApplyStructuralProperty(resource, structuralProperty, structuredType, readContext);

                // Check if the base class successfully set the property.
                // With camelCase EDM, the base uses the CLR name (e.g. "Title") but
                // EdmStructuredObject only accepts the EDM name (e.g. "title"), so
                // TrySetPropertyValue silently fails. Detect this and re-apply.
                var propsAfter = edmObject.GetChangedPropertyNames().ToHashSet();
                if (propsAfter.Count > propsBefore.Count)
                {
                    // Base class successfully set the property — nothing to fix
                    return;
                }

                // Property was dropped. Re-apply using the EDM name.
                // First, find what value the base class materialized by trying the CLR name.
                var edmPropertyName = structuralProperty.Name;
                var clrPropertyName = EdmClrPropertyMapper.GetClrPropertyName(
                    structuredType.FindProperty(edmPropertyName), readContext.Model);

                if (clrPropertyName != edmPropertyName && edmObject.TryGetPropertyValue(clrPropertyName, out var value))
                {
                    // Base set it under CLR name but EdmStructuredObject rejected it.
                    // This shouldn't happen (TrySetPropertyValue returns false for unknown names),
                    // but handle it defensively.
                    edmObject.TrySetPropertyValue(edmPropertyName, value);
                }
                else
                {
                    // Base class couldn't set it at all. Fall back to raw OData value
                    // with EDM property name. Handle enum values as strings for
                    // EFChangeSetInitializer.ConvertToEfValue compatibility.
                    var rawValue = structuralProperty.Value;
                    if (rawValue is ODataEnumValue enumVal)
                    {
                        rawValue = enumVal.Value;
                    }

                    edmObject.TrySetPropertyValue(edmPropertyName, rawValue);
                }

                return;
            }

            // For CLR objects (not EdmStructuredObject), the base implementation correctly
            // maps EDM names to CLR property names via reflection. No fix needed.
            base.ApplyStructuralProperty(resource, structuralProperty, structuredType, readContext);
        }
    }
}
