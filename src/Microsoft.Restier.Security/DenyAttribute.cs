// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Specifies a role-based security statement for an API that
    /// denies permission on a securable element to a specific role.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DenyAttribute : ApiConfiguratorAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DenyAttribute" /> class.
        /// </summary>
        /// <param name="permissionType">
        /// A built-in or custom permission type.
        /// </param>
        public DenyAttribute(string permissionType)
        {
            Ensure.NotNull(permissionType, "permissionType");
            this.PermissionType = permissionType;
        }

        /// <summary>
        /// Gets the type of the permission being denied.
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
        /// Gets or sets the role to which this API permission applies.
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// Configure an API.
        /// </summary>
        /// <param name="services">
        /// The API services registration.
        /// </param>
        /// <param name="type">
        /// The API type on which this attribute was placed.
        /// </param>
        [CLSCompliant(false)]
        public override void AddApiServices(IServiceCollection services, Type type)
        {
            var permission = ApiPermission.CreateDeny(
                this.PermissionType,
                this.To,
                this.OnNamespace,
                this.On,
                this.OnChild);
            services.AddInstance(permission);
        }
    }
}
