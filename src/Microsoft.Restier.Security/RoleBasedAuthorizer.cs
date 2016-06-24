// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Security.Properties;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Represents a role-based authorization system.
    /// This class makes role based security checks on expression nodes.
    /// </summary>
    public class RoleBasedAuthorizer : IQueryExpressionAuthorizer
    {
        private const string AssertedRoles = "Microsoft.Restier.Security.AssertedRoles";

        /// <summary>
        /// Inspects an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// <c>true</c> if the inspection passed; otherwise, <c>false</c>.
        /// </returns>
        public Task<bool> AuthorizeAsync(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");

            // TODO GitHubIssue#35 : Support Inspect more elements in authorization
            if (context.ModelReference == null)
            {
                return Task.FromResult(true);
            }

            var dataSourceStubReference = context.ModelReference as DataSourceStubModelReference;
            if (dataSourceStubReference == null)
            {
                return Task.FromResult(true);
            }

            var entitySet = dataSourceStubReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return Task.FromResult(true);
            }

            var assertedRoles = context.QueryContext.ApiContext.GetProperty<List<string>>(AssertedRoles);
            var permissions = context.QueryContext.GetApiServices<ApiPermission>();
            if (permissions == null)
            {
                throw new SecurityException(
                    string.Format(CultureInfo.InvariantCulture, Resources.ReadDeniedOnEntitySet, entitySet.Name));
            }

            permissions = permissions.Where(p => (
                p.PermissionType == ApiPermissionType.All ||
                p.PermissionType == ApiPermissionType.Read) && (
                (p.NamespaceName == null && p.SecurableName == null) ||
                (p.NamespaceName == null && p.SecurableName == entitySet.Name)) &&
                p.ChildName == null && (p.Role == null || this.IsInRole(p.Role) ||
                (assertedRoles != null && assertedRoles.Contains(p.Role))));
            if (!permissions.Any() || permissions.Any(p => p.IsDeny))
            {
                throw new SecurityException(
                    string.Format(CultureInfo.InvariantCulture, Resources.ReadDeniedOnEntitySet, entitySet.Name));
            }

            return Task.FromResult(true);
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
