// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Represents a set of built-in API permission types.
    /// </summary>
    public static class ApiPermissionType
    {
        /// <summary>
        /// Allows inspecting the model definition of a securable element.
        /// </summary>
        public const string Inspect = "Inspect";

        /// <summary>
        /// Allows creation of a new entity in an entity set.
        /// </summary>
        public const string Create = "Create";

        /// <summary>
        /// Allows reading entities from an entity set.
        /// </summary>
        public const string Read = "Read";

        /// <summary>
        /// Allows updating entities in an entity set.
        /// </summary>
        public const string Update = "Update";

        /// <summary>
        /// Allows deleting entities in an entity set.
        /// </summary>
        public const string Delete = "Delete";

        /// <summary>
        /// Allows invoking a function or action.
        /// </summary>
        public const string Invoke = "Invoke";

        /// <summary>
        /// Allows all actions on a securable element.
        /// </summary>
        public const string All = "All";
    }
}
