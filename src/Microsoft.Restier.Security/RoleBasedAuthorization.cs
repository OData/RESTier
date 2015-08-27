// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Threading;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Security.Properties;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Represents a role-based authorization system.
    /// </summary>
    public class RoleBasedAuthorization : IQueryExpressionInspector
    {
        private const string Permissions = "Microsoft.Restier.Security.Permissions";

        private const string AssertedRoles = "Microsoft.Restier.Security.AssertedRoles";

        static RoleBasedAuthorization()
        {
            Default = new RoleBasedAuthorization();
        }

        private RoleBasedAuthorization()
        {
        }

        /// <summary>
        /// Gets the default role-based authorization system instance, which
        /// uses the current security principal to determine role membership.
        /// </summary>
        public static RoleBasedAuthorization Default { get; private set; }

        /// <summary>
        /// Inspects an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// <c>true</c> if the inspection passed; otherwise, <c>false</c>.
        /// </returns>
        public bool Inspect(QueryExpressionContext context)
        {
            Ensure.NotNull(context);

            // TODO GitHubIssue#35 : Support Inspect more elements in authorization
            if (context.ModelReference == null)
            {
                return true;
            }

            var domainDataReference = context.ModelReference as DomainDataReference;
            if (domainDataReference == null)
            {
                return true;
            }

            var entitySet = domainDataReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return true;
            }

            var assertedRoles = context.QueryContext
                .GetProperty<List<string>>(AssertedRoles);
            var permissions = context.QueryContext.DomainContext.Configuration
                .GetProperty<IEnumerable<DomainPermission>>(Permissions);
            if (permissions == null)
            {
                throw new SecurityException(
                    string.Format(CultureInfo.InvariantCulture, Resources.ReadDeniedOnEntitySet, entitySet.Name));
            }

            permissions = permissions.Where(p => (
                p.PermissionType == DomainPermissionType.All ||
                p.PermissionType == DomainPermissionType.Read) && (
                (p.NamespaceName == null && p.SecurableName == null) ||
                (p.NamespaceName == null && p.SecurableName == entitySet.Name)) &&
                p.ChildName == null && (p.Role == null || this.IsInRole(p.Role) ||
                (assertedRoles != null && assertedRoles.Contains(p.Role))));
            if (!permissions.Any() || permissions.Any(p => p.IsDeny))
            {
                throw new SecurityException(
                    string.Format(CultureInfo.InvariantCulture, Resources.ReadDeniedOnEntitySet, entitySet.Name));
            }

            return true;
        }

        /// <summary>
        /// Determines if the current user is in a role.
        /// </summary>
        /// <param name="role">
        /// The name of a role.
        /// </param>
        /// <returns>
        /// <c>true</c> if the current user is
        /// in the role; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool IsInRole(string role)
        {
            return Thread.CurrentPrincipal.IsInRole(role);
        }
    }
}
