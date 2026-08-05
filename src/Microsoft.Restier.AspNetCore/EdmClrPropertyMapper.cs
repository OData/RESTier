// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Microsoft.Restier.AspNetCore
{
    /// <summary>
    /// Maps EDM property names back to CLR property names using model annotations.
    /// When <c>EnableLowerCamelCase</c> has been called on the <see cref="ODataConventionModelBuilder"/>,
    /// EDM properties carry a <see cref="ClrPropertyInfoAnnotation"/> that maps to the original CLR PropertyInfo.
    /// Without camelCase, no annotation exists and the EDM name is returned as-is.
    /// </summary>
    internal static class EdmClrPropertyMapper
    {
        /// <summary>
        /// Gets the CLR property name for a given EDM property.
        /// </summary>
        /// <param name="edmProperty">The EDM property to look up.</param>
        /// <param name="model">The EDM model that may contain CLR annotations.</param>
        /// <returns>The CLR property name, or the EDM property name if no annotation exists.</returns>
        public static string GetClrPropertyName(IEdmProperty edmProperty, IEdmModel model)
        {
            var annotation = model.GetAnnotationValue<ClrPropertyInfoAnnotation>(edmProperty);
            return annotation?.ClrPropertyInfo?.Name ?? edmProperty.Name;
        }
    }
}
