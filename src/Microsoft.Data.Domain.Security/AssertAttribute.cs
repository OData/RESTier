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
    /// Specifies a domain security policy that asserts a role should be
    /// present for the current principal on the target type or member.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class AssertAttribute : Attribute, IDomainPolicy
    {
        /// <summary>
        /// Initializes a new assert attribute.
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
        /// Activates this domain policy.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        public void Activate(InvocationContext context)
        {
            context.AssertRole(this.Role);
        }

        /// <summary>
        /// Deactivates this domain policy.
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
