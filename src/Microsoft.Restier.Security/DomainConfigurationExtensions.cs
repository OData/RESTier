// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic)
    /// methods for interacting with objects that implement
    /// <see cref="DomainConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class DomainConfigurationExtensions
    {
        private const string Permissions =
            "Microsoft.Restier.Security.Permissions";

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
            var permissions = configuration.GetProperty<List<DomainPermission>>(Permissions);
            if (permissions == null)
            {
                permissions = new List<DomainPermission>();
                configuration.SetProperty(Permissions, permissions);
            }

            permissions.Add(permission);
        }
    }
}
