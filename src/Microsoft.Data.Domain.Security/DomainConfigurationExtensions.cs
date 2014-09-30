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
    using Model;
    using Query;

    /// <summary>
    /// Provides a set of static (Shared in Visual Basic)
    /// methods for interacting with objects that implement
    /// <see cref="DomainConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class DomainConfigurationExtensions
    {
        private const string Permissions =
            "Microsoft.Data.Domain.Security.Permissions";

        /// <summary>
        /// Enables principal-supplied role-based security for a domain.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <remarks>
        /// This method adds hook points to the domain configuration that
        /// authorize according to roles assigned to the current principal
        /// along with any that have been asserted during a domain flow.
        /// </remarks>
        public static void EnableRoleBasedSecurity(
            this DomainConfiguration configuration)
        {
            Ensure.NotNull(configuration, "configuration");
            configuration.AddHookPoint(
                typeof(IModelVisibilityFilter),
                RoleBasedAuthorization.Default);
            configuration.AddHookPoint(
                typeof(IQueryExpressionInspector),
                RoleBasedAuthorization.Default);
        }

        /// <summary>
        /// Adds a domain permission to a domain configuration.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="permission">
        /// A domain permission.
        /// </param>
        public static void AddPermission(
            this DomainConfiguration configuration,
            DomainPermission permission)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(permission, "permission");
            var permissions = configuration.GetProperty<
                List<DomainPermission>>(Permissions);
            if (permissions == null)
            {
                permissions = new List<DomainPermission>();
                configuration.SetProperty(Permissions, permissions);
            }
            permissions.Add(permission);
        }
    }
}
