// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;

namespace Microsoft.Data.Domain.Model
{
    /// <summary>
    /// Represents a hook point that maps between
    /// the model space and the object space.
    /// </summary>
    /// <remarks>
    /// This is both a singleton hook point that should be implemented by an
    /// underlying data provider as well as a multi-cast hook point whose
    /// instances are used in reverse order of registration. When in use,
    /// the multi-cast hook points are used before the singleton hook point.
    /// </remarks>
    public interface IModelMapper
    {
        /// <summary>
        /// Tries to get the relevant type of an entity
        /// set, singleton, or composable function import.
        /// </summary>
        /// <param name="context">
        /// A domain context.
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
        /// type when it is overriding a previously registered hook point and
        /// specifically opting to not support the specified queryable source.
        /// </para>
        /// </remarks>
        bool TryGetRelevantType(
            DomainContext context,
            string name,
            out Type relevantType);

        /// <summary>
        /// Tries to get the relevant type of a composable function.
        /// </summary>
        /// <param name="context">
        /// A domain context.
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
        /// type when it is overriding a previously registered hook point and
        /// specifically opting to not support the specified composable function.
        /// </para>
        /// </remarks>
        bool TryGetRelevantType(
            DomainContext context,
            string namespaceName, string name,
            out Type relevantType);
    }
}
