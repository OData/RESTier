// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic)
    /// methods for interacting with objects that implement
    /// <see cref="InvocationContext"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class InvocationContextExtensions
    {
        private const string AssertedRoles =
            "Microsoft.Restier.Security.AssertedRoles";

        /// <summary>
        /// Asserts that a role should be present for the current principal.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        /// <param name="role">
        /// The name of a role.
        /// </param>
        public static void AssertRole(
            this InvocationContext context, string role)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(role, "role");
            var assertedRoles = context.GetProperty<
                List<string>>(AssertedRoles);
            if (assertedRoles == null)
            {
                assertedRoles = new List<string>();
                context.SetProperty(AssertedRoles, assertedRoles);
            }
            assertedRoles.Add(role);
        }

        /// <summary>
        /// Revokes a previous assertion for a role.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        /// <param name="role">
        /// The name of a role.
        /// </param>
        public static void RevokeRole(
            this InvocationContext context, string role)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(role, "role");
            var assertedRoles = context.GetProperty<
                List<string>>(AssertedRoles);
            if (assertedRoles != null)
            {
                int index = assertedRoles.LastIndexOf(role);
                if (index >= 0)
                {
                    assertedRoles.RemoveAt(index);
                }
            }
        }
    }
}
