// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Domain.Security
{
    /// <summary>
    /// Specifies a role-based security statement for a domain that
    /// grants permission on a securable element to a specific role.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GrantAttribute : DomainParticipantAttribute
    {
        /// <summary>
        /// Initializes a new grant attribute.
        /// </summary>
        /// <param name="permissionType">
        /// A built-in or custom permission type.
        /// </param>
        public GrantAttribute(string permissionType)
        {
            Ensure.NotNull(permissionType, "permissionType");
            this.PermissionType = permissionType;
        }

        /// <summary>
        /// Gets the type of the permission being granted.
        /// </summary>
        public string PermissionType { get; private set; }

        /// <summary>
        /// Gets or sets the name of the namespace
        /// containing the securable element.
        /// </summary>
        public string OnNamespace { get; set; }

        /// <summary>
        /// Gets or sets the name of the securable element.
        /// </summary>
        public string On { get; set; }

        /// <summary>
        /// Gets or sets the name of the child of the securable element.
        /// </summary>
        public string OnChild { get; set; }

        /// <summary>
        /// Gets or sets the role to which this domain permission applies.
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// Configures a domain configuration.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="type">
        /// The domain type on which this attribute was placed.
        /// </param>
        public override void Configure(
            DomainConfiguration configuration,
            Type type)
        {
            var permission = DomainPermission.CreateGrant(this.PermissionType,
                this.To, this.OnNamespace, this.On, this.OnChild);
            configuration.AddPermission(permission);
        }
    }
}
