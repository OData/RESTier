// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Conventions
{
    /// <summary>
    /// A conventional entity set provider that extends a model, maps between
    /// the model space and the object space, and expands a query expression.
    /// </summary>
    public class ConventionalEntitySetProvider :
        IModelExtender, IModelMapper, IQueryExpressionExpander
    {
        private Type _targetType;

        private ConventionalEntitySetProvider(Type targetType)
        {
            this._targetType = targetType;
        }

        /// <inheritdoc/>
        public static void ApplyTo(
            DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");
            var provider = new ConventionalEntitySetProvider(targetType);
            configuration.AddHookPoint(typeof(IModelExtender), provider);
            configuration.AddHookPoint(typeof(IModelMapper), provider);
            configuration.AddHookPoint(typeof(IQueryExpressionExpander), provider);
        }

        /// <inheritdoc/>
        public Task ExtendModelAsync(
            ModelContext context,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context);
            var model = context.Model;
            foreach (var entitySetProperty in this.AddedEntitySets)
            {
                var container = model.EntityContainer as EdmEntityContainer;
                var elementType = entitySetProperty
                    .PropertyType.GetGenericArguments()[0];
                var entityType = context.Model.SchemaElements
                    .OfType<IEdmEntityType>()
                    .SingleOrDefault(se => se.Name == elementType.Name);
                if (entityType == null)
                {
                    // TODO GitHubIssue#33 : Add new entity type representing entity shape
                    continue;
                }

                container.AddEntitySet(entitySetProperty.Name, entityType);
            }

            return Task.FromResult<object>(null);
        }

        /// <inheritdoc/>
        public bool TryGetRelevantType(
            DomainContext context,
            string name, out Type relevantType)
        {
            relevantType = null;
            var entitySetProperty = this.AddedEntitySets
                .SingleOrDefault(p => p.Name == name);
            if (entitySetProperty != null)
            {
                relevantType = entitySetProperty
                    .PropertyType.GetGenericArguments()[0];
            }

            return relevantType != null;
        }

        /// <inheritdoc/>
        public bool TryGetRelevantType(
            DomainContext context,
            string namespaceName, string name,
            out Type relevantType)
        {
            relevantType = null;
            return false;
        }

        /// <inheritdoc/>
        public Expression Expand(QueryExpressionContext context)
        {
            Ensure.NotNull(context);
            if (context.ModelReference == null)
            {
                return null;
            }

            var domainDataReference = context.ModelReference as DomainDataReference;
            if (domainDataReference == null)
            {
                return null;
            }

            var entitySet = domainDataReference.Element as IEdmEntitySet;
            if (entitySet == null)
            {
                return null;
            }

            var entitySetProperty = this.AddedEntitySets
                .SingleOrDefault(p => p.Name == entitySet.Name);
            if (entitySetProperty != null)
            {
                object target = null;
                if (!entitySetProperty.GetMethod.IsStatic)
                {
                    target = context.QueryContext.DomainContext.GetProperty(
                        this._targetType.AssemblyQualifiedName);
                    if (target == null ||
                        !this._targetType.IsAssignableFrom(target.GetType()))
                    {
                        return null;
                    }
                }

                var result = entitySetProperty.GetValue(target) as IQueryable;
                if (result != null)
                {
                    var policies = entitySetProperty.GetCustomAttributes()
                        .OfType<IDomainPolicy>();
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

                    return result.Expression;
                }
            }

            return null;
        }

        private IEnumerable<PropertyInfo> AddedEntitySets
        {
            get
            {
                var properties = this._targetType.GetProperties(
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);
                return properties.Where(p =>
                    !p.CanWrite && !p.GetMethod.IsPrivate &&
                    p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(IQueryable<>)
                ).ToArray();
            }
        }
    }
}
