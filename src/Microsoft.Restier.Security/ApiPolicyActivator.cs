﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
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
        public Task<Expression> ExpandAsync(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");

            if (context.ModelReference == null)
            {
                return CallInner(context);
            }

            var dataSourceStubReference = context.ModelReference as DataSourceStubModelReference;
            if (dataSourceStubReference == null)
            {
                return CallInner(context);
            }

            var entitySet = dataSourceStubReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return CallInner(context);
            }

            var target = context.QueryContext.GetApiService<ApiBase>();
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

            // This class is used to activate and deactivate the policies
            // thus it is NOT intended to actually expand any query here.
            return CallInner(context);
        }

        private Task<Expression> CallInner(QueryExpressionContext context)
        {
            if (this.InnerHandler != null)
            {
                return this.InnerHandler.ExpandAsync(context);
            }

            return null;
        }
    }
}
