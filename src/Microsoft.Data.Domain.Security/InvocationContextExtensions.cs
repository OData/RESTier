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
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Data.Domain.Security
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
            "Microsoft.Data.Domain.Security.AssertedRoles";

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
