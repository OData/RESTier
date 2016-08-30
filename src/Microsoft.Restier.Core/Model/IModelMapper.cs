// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// Represents a service that maps between
    /// the model space and the object space.
    /// </summary>
    public interface IModelMapper
    {
        /// <summary>
        /// Tries to get the relevant type of an entity
        /// set, singleton, or composable function import.
        /// </summary>
        /// <param name="context">
        /// The context for model mapper.
        /// </param>
        /// <param name="name">
        /// The name of an entity set, singleton or composable function import.
        /// </param>
        /// <param name="relevantType">
        /// When this method returns, provides the
        /// relevant type of the queryable source.
        /// </param>
        /// <returns>
        /// <c>true</c> if the relevant type was
        /// provided; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// For entity sets, the relevant type is its element entity type.
        /// </para>
        /// <para>
        /// For singletons, the relevant type is the singleton entity type.
        /// </para>
        /// <para>
        /// For composable function imports, the relevant type is the return
        /// type if it is a primitive, complex or entity type, or the element
        /// type of the return type if it is a collection type.
        /// </para>
        /// <para>
        /// This method can return true and assign <c>null</c> as the relevant
        /// type when it is overriding a previously registered service and
        /// specifically opting to not support the specified queryable source.
        /// </para>
        /// </remarks>
        bool TryGetRelevantType(
            ModelContext context,
            string name,
            out Type relevantType);

        /// <summary>
        /// Tries to get the relevant type of a composable function.
        /// </summary>
        /// <param name="context">
        /// The context for model mapper.
        /// </param>
        /// <param name="namespaceName">
        /// The name of a namespace containing a composable function.
        /// </param>
        /// <param name="name">
        /// The name of composable function.
        /// </param>
        /// <param name="relevantType">
        /// When this method returns, provides the
        /// relevant type of the composable function.
        /// </param>
        /// <returns>
        /// <c>true</c> if the relevant type was
        /// provided; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// For composable functions, the relevant type is the return
        /// type if it is a primitive, complex or entity type, or the
        /// element type of the return type if it is a collection type.
        /// </para>
        /// <para>
        /// This method can return true and assign <c>null</c> as the relevant
        /// type when it is overriding a previously registered service and
        /// specifically opting to not support the specified composable function.
        /// </para>
        /// </remarks>
        bool TryGetRelevantType(
            ModelContext context,
            string namespaceName,
            string name,
            out Type relevantType);
    }
}
