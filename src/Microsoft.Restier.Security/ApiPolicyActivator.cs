// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// This class applies security policy on expanded expression nodes.
    /// </summary>
    public class ApiPolicyActivator : IQueryExpressionExpander
    {
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

            var target = context.QueryContext.GetApiService<IApi>();
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
    }
}
