// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Restier.Core.Spatial
{
    /// <summary>
    /// Provides EF-flavor-specific metadata that the shared <c>SpatialModelConvention</c> needs
    /// to identify and classify storage-typed spatial properties at model-build time.
    /// </summary>
    public interface ISpatialModelMetadataProvider
    {
        /// <summary>
        /// Returns true if values of <paramref name="clrType"/> are spatial storage values for this flavor.
        /// </summary>
        /// <param name="clrType">A CLR type from an entity property declaration.</param>
        bool IsSpatialStorageType(Type clrType);

        /// <summary>
        /// Infers the spatial genus (Geography vs Geometry) for a given property.
        /// </summary>
        /// <param name="entityClrType">The entity CLR type owning the property.</param>
        /// <param name="property">The property declaration.</param>
        /// <param name="providerContext">
        /// Flavor-specific lookup state. EF6 passes <c>null</c>; EF Core passes the active <c>DbContext</c>
        /// instance (cast inside the provider to read <c>.Model</c> for column-type inference).
        /// </param>
        /// <returns>The inferred genus, or <c>null</c> if the genus cannot be determined.</returns>
        SpatialGenus? InferGenus(Type entityClrType, PropertyInfo property, object providerContext);

        /// <summary>
        /// The full set of storage CLR types that the convention should pass to
        /// <c>ODataConventionModelBuilder.Ignore(Type[])</c> so the convention builder
        /// skips them during structural-property discovery.
        /// </summary>
        IReadOnlyList<Type> IgnoredStorageTypes { get; }
    }
}
