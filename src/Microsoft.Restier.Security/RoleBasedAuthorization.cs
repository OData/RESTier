// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
    public class RoleBasedAuthorization : IQueryExpressionInspector,
        IQueryExpressionExpander, IDelegateHookHandler<IQueryExpressionExpander>
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

        /// <inheritdoc/>
        public IQueryExpressionExpander InnerHandler { get; set; }

        /// <summary>
        /// Expands an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// An expanded expression of the same type as the visited node, or
        /// if expansion did not apply, the visited node or <c>null</c>.
        /// </returns>
        public Expression Expand(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");

            if (this.InnerHandler != null)
            {
                var result = this.InnerHandler.Expand(context);
                if (result != null)
                {
                    return result;
                }
            }

            if (context.ModelReference == null)
            {
                return null;
            }

            var apiDataReference = context.ModelReference as ApiDataReference;
            if (apiDataReference == null)
            {
                return null;
            }

            var entitySet = apiDataReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return null;
            }

            var target = context.QueryContext.ApiContext.GetProperty(
                typeof(Api).AssemblyQualifiedName);
            var entitySetProperty = target.GetType().GetProperties(
                BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.DeclaredOnly)
                .SingleOrDefault(p => p.Name == entitySet.Name);
            if (entitySetProperty != null)
            {
                var policies = entitySetProperty.GetCustomAttributes()
                        .OfType<IApiPolicy>();

                foreach (var policy in policies)
                {
                    policy.Activate(context.QueryContext);
                }

                context.AfterNestedVisitCallback = () =>
                {
                    foreach (var policy in policies.Reverse())
                    {
                        policy.Deactivate(context.QueryContext);
                    }
                };
            }

            return context.VisitedNode;
        }

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
                return true;
            }

            var apiDataReference = context.ModelReference as ApiDataReference;
            if (apiDataReference == null)
            {
                return true;
            }

            var entitySet = apiDataReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return true;
            }

            var assertedRoles = context.QueryContext
                .GetProperty<List<string>>(AssertedRoles);
            var permissions = context.QueryContext.ApiContext.Configuration
                .GetProperty<IEnumerable<ApiPermission>>(Permissions);
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
