﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Threading;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Security.Properties;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Represents a role-based authorization system.
    /// This class makes role based security checks on expression nodes.
    /// </summary>
    public class RoleBasedAuthorizer : IQueryExpressionInspector
    {
        /// <summary>
        /// Gets or sets the inner <see cref="IQueryExpressionInspector"/> instance.
        /// </summary>
        public IQueryExpressionInspector Inner { get; set; }

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
            Ensure.NotNull(context, "context");

            // TODO GitHubIssue#35 : Support Inspect more elements in authorization
            if (context.ModelReference == null)
            {
                return CallInner(context);
            }

            var dataSourceStubReference = context.ModelReference as DataSourceStubReference;
            if (dataSourceStubReference == null)
            {
                return CallInner(context);
            }

            var entitySet = dataSourceStubReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return CallInner(context);
            }

            var assertedRoles = context.QueryContext.GetApiService<AssertedRoles>();
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

            return CallInner(context);
        }

        /// <summary>
        /// Calls the inner handler if it presents.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// The return value of the inner handler or <c>true</c> if inner handler is <c>null</c>.
        /// </returns>
        protected bool CallInner(QueryExpressionContext context)
        {
            if (Inner != null)
            {
                return Inner.Inspect(context);
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
