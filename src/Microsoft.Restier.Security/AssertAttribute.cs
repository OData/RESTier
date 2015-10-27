// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Specifies an API security policy that asserts a role should be
    /// present for the current principal on the target type or member.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class AssertAttribute : Attribute, IApiPolicy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssertAttribute" /> class.
        /// </summary>
        /// <param name="role">
        /// The name of a role.
        /// </param>
        public AssertAttribute(string role)
        {
            Ensure.NotNull(role, "role");
            this.Role = role;
        }

        /// <summary>
        /// Gets the role being asserted.
        /// </summary>
        public string Role { get; private set; }

        /// <summary>
        /// Activates this API policy.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        public void Activate(InvocationContext context)
        {
            context.AssertRole(this.Role);
        }

        /// <summary>
        /// Deactivates this API policy.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        public void Deactivate(InvocationContext context)
        {
            context.RevokeRole(this.Role);
        }
    }
}
