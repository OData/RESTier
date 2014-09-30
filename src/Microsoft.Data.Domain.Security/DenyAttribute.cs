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

namespace Microsoft.Data.Domain.Security
{
    /// <summary>
    /// Specifies a role-based security statement for a domain that
    /// denies permission on a securable element to a specific role.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DenyAttribute : DomainParticipantAttribute
    {
        /// <summary>
        /// Initializes a new deny attribute.
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
            var permission = DomainPermission.CreateDeny(this.PermissionType,
                this.To, this.OnNamespace, this.On, this.OnChild);
            configuration.AddPermission(permission);
        }
    }
}
