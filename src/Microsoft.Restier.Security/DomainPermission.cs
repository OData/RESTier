// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Represents a role-based security statement that grants or
    /// denies permission on a securable element to a specific role.
    /// </summary>
    public class DomainPermission
    {
        private DomainPermission()
        {
        }

        /// <summary>
        /// Creates a grant permission.
        /// </summary>
        /// <param name="permissionType">
        /// A built-in or custom permission type.
        /// </param>
        /// <param name="role">
        /// The name of a role, or <c>null</c> to grant permission to everyone.
        /// </param>
        /// <param name="namespaceName">
        /// The name of a namespace that contains a securable element.
        /// </param>
        /// <param name="securableName">
        /// The name of a securable element.
        /// </param>
        /// <param name="childName">
        /// The name of a child of a securable element.
        /// </param>
        /// <returns>
        /// A new domain permission.
        /// </returns>
        /// <remarks>
        /// If no securable element is identified, the permission is granted
        /// across all securable elements. If a namespace name is not provided,
        /// the securable name identifies an element in the modeled entity
        /// container, otherwise it identifies an element in a modeled schema.
        /// </remarks>
        public static DomainPermission CreateGrant(
            string permissionType,
            string role = null,
            string namespaceName = null,
            string securableName = null,
            string childName = null)
        {
            Ensure.NotNull(permissionType, "permissionType");
            return new DomainPermission()
            {
                IsGrant = true, IsDeny = false,
                PermissionType = permissionType,
                NamespaceName = namespaceName,
                SecurableName = securableName,
                ChildName = childName,
                Role = role
            };
        }

        /// <summary>
        /// Creates a deny permission.
        /// </summary>
        /// <param name="permissionType">
        /// A built-in or custom permission type.
        /// </param>
        /// <param name="role">
        /// The name of a role, or <c>null</c> to deny permission to everyone.
        /// </param>
        /// <param name="namespaceName">
        /// The name of a namespace that contains a securable element.
        /// </param>
        /// <param name="securableName">
        /// The name of a securable element.
        /// </param>
        /// <param name="childName">
        /// The name of a child of a securable element.
        /// </param>
        /// <returns>
        /// A new domain permission.
        /// </returns>
        /// <remarks>
        /// If no securable element is identified, the permission is denied
        /// across all securable elements. If a namespace name is not provided,
        /// the securable name identifies an element in the modeled entity
        /// container, otherwise it identifies an element in a modeled schema.
        /// </remarks>
        public static DomainPermission CreateDeny(
            string permissionType,
            string role = null,
            string namespaceName = null,
            string securableName = null,
            string childName = null)
        {
            Ensure.NotNull(permissionType, "permissionType");
            return new DomainPermission()
            {
                IsGrant = false, IsDeny = true,
                PermissionType = permissionType,
                NamespaceName = namespaceName,
                SecurableName = securableName,
                ChildName = childName,
                Role = role
            };
        }

        /// <summary>
        /// Gets a value indicating whether this domain permission grants access.
        /// </summary>
        public bool IsGrant { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this domain permission denies access.
        /// </summary>
        public bool IsDeny { get; private set; }

        /// <summary>
        /// Gets the type of the permission being granted or denied.
        /// </summary>
        public string PermissionType { get; private set; }

        /// <summary>
        /// Gets the name of the namespace containing the securable element.
        /// </summary>
        public string NamespaceName { get; private set; }

        /// <summary>
        /// Gets the name of the securable element.
        /// </summary>
        public string SecurableName { get; private set; }

        /// <summary>
        /// Gets the name of the child of the securable element.
        /// </summary>
        public string ChildName { get; private set; }

        /// <summary>
        /// Gets the role to which this domain permission applies.
        /// </summary>
        public string Role { get; private set; }
    }

    /// <summary>
    /// Represents a set of built-in domain permission types.
    /// </summary>
    public static class DomainPermissionType
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
