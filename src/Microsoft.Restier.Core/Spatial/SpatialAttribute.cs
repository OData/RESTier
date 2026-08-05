// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core.Spatial
{
    using System;

    /// <summary>
    /// Declares the Microsoft.Spatial EDM type to publish for a storage-typed spatial property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SpatialAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialAttribute"/> class.
        /// </summary>
        /// <param name="edmType">The Microsoft.Spatial CLR type to publish (e.g. <c>typeof(GeographyPoint)</c>).</param>
        public SpatialAttribute(Type edmType)
        {
            EdmType = edmType;
        }

        /// <summary>
        /// Gets the Microsoft.Spatial CLR type to publish (a subclass of <c>Microsoft.Spatial.Geography</c> or <c>Geometry</c>).
        /// </summary>
        public Type EdmType { get; }
    }
}
