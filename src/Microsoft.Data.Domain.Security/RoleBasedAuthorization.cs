// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Represents a role-based authorization system.
    /// </summary>
    public class RoleBasedAuthorization :
        IModelVisibilityFilter, IQueryExpressionInspector
    {
        private const string Permissions =
            "Microsoft.Restier.Security.Permissions";
        private const string AssertedRoles =
            "Microsoft.Restier.Security.AssertedRoles";

        /// <summary>
        /// Gets the default role-based authorization system instance, which
        /// uses the current security principal to determine role membership.
        /// </summary>
        public static readonly RoleBasedAuthorization Default =
            new RoleBasedAuthorization();

        /// <summary>
        /// Indicates if a schema element is currently visible.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="context">
        /// An optional invocation context.
        /// </param>
        /// <param name="model">
        /// A model.
        /// </param>
        /// <param name="element">
        /// A schema element.
        /// </param>
        /// <returns>
        /// <c>true</c> if the element is currently
        /// visible; otherwise, <c>false</c>.
        /// </returns>
        public bool IsVisible(
            DomainConfiguration configuration,
            InvocationContext context,
            IEdmModel model, IEdmSchemaElement element)
        {
            // TODO: properly filter types
            if (element is IEdmType || element is IEdmOperation)
            {
                return true;
            }
            return this.IsVisible(configuration,
                context, element.Namespace, element.Name);
        }

        /// <summary>
        /// Indicates if an entity container element is currently visible.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="context">
        /// An optional invocation context.
        /// </param>
        /// <param name="model">
        /// A model.
        /// </param>
        /// <param name="element">
        /// An entity container element.
        /// </param>
        /// <returns>
        /// <c>true</c> if the element is currently
        /// visible; otherwise, <c>false</c>.
        /// </returns>
        public bool IsVisible(
            DomainConfiguration configuration,
            InvocationContext context,
            IEdmModel model, IEdmEntityContainerElement element)
        {
            return this.IsVisible(configuration,
                context, null, element.Name);
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
            // TODO: something other than entity sets
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
                // TODO: error message
                throw new SecurityException(
                    "Not authorized for read: " + entitySet.Name);
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
                // TODO: error message
                throw new SecurityException(
                    "Not authorized for read: " + entitySet.Name);
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

        private bool IsVisible(
            DomainConfiguration configuration,
            InvocationContext context,
            string namespaceName,
            string securableName)
        {
            List<string> assertedRoles = null;
            if (context != null)
            {
                assertedRoles = context.GetProperty<
                    List<string>>(AssertedRoles);
            }
            var permissions = configuration.GetProperty<
                IEnumerable<DomainPermission>>(Permissions);
            if (permissions == null)
            {
                return false;
            }
            permissions = permissions.Where(p => (
                p.PermissionType == DomainPermissionType.All ||
                p.PermissionType == DomainPermissionType.Inspect) && (
                (p.NamespaceName == null && p.SecurableName == null) ||
                (p.NamespaceName == namespaceName && p.SecurableName == securableName)) &&
                p.ChildName == null && (p.Role == null || this.IsInRole(p.Role) ||
                (assertedRoles != null && assertedRoles.Contains(p.Role))));
            if (!permissions.Any() || permissions.Any(p => p.IsDeny))
            {
                return false;
            }
            return true;
        }
    }
}
