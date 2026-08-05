// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core.Spatial
{
    /// <summary>
    /// Converts between EF storage-typed spatial values (e.g. <c>DbGeography</c>, NTS <c>Geometry</c>)
    /// and Microsoft.Spatial primitive values (e.g. <c>GeographyPoint</c>).
    /// One implementation per EF flavor; resolved via DI.
    /// </summary>
    public interface ISpatialTypeConverter
    {
        /// <summary>
        /// Returns true if this converter handles values of the given storage CLR type.
        /// </summary>
        /// <param name="storageType">The CLR type of the storage value (e.g. <c>typeof(DbGeography)</c>).</param>
        bool CanConvert(Type storageType);

        /// <summary>
        /// Converts a storage value into the requested Microsoft.Spatial type.
        /// </summary>
        /// <param name="storageValue">The storage value (e.g. a <c>DbGeography</c> instance). May be null.</param>
        /// <param name="targetEdmType">The Microsoft.Spatial CLR type to produce (e.g. <c>typeof(GeographyPoint)</c>).</param>
        /// <returns>A Microsoft.Spatial value, or null if <paramref name="storageValue"/> was null.</returns>
        object ToEdm(object storageValue, Type targetEdmType);

        /// <summary>
        /// Converts a Microsoft.Spatial value into the requested storage CLR type.
        /// </summary>
        /// <param name="targetStorageType">The storage CLR type to produce (e.g. <c>typeof(DbGeography)</c>).</param>
        /// <param name="edmValue">The Microsoft.Spatial value. May be null.</param>
        /// <returns>A storage value, or null if <paramref name="edmValue"/> was null.</returns>
        object ToStorage(Type targetStorageType, object edmValue);
    }
}
