// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class BoundOperationAttribute : OperationAttribute
    {
        /// <summary>
        /// Gets or sets the path from the BindingParameter do the entity or entities being returned.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Bound Actions or Functions that return an entity or a collection of entities are typically returning data related to the Entity
        /// the operation is bound to. In these situations, it may be difficult for OData to return the corerct metadata, or for Restier to 
        /// execute the proper Interceptors to filter the results.
        /// </para>
        /// <para>
        /// EntitySetPath solves this problem by specifying the navigation segments to type casts required to traverse the entity structure.
        /// It consists of a series of segments joined together with forward slashes.
        ///  - The first segment of the entity set path MUST be the name of the binding parameter.
        ///  - The remaining segments of the entity set path MUST represent navigation segments or type casts.
        /// </para>
        /// </remarks>
        public string EntitySetPath { get; set; }
    }
}
