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
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Core.Conventions
{
    /// <summary>
    /// A conventional entity set provider that extends a model, maps between
    /// the model space and the object space, and expands a query expression.
    /// </summary>
    internal class ConventionalEntitySetProvider :
        IModelBuilder, IDelegateHookHandler<IModelBuilder>,
        IModelMapper, IDelegateHookHandler<IModelMapper>,
        IQueryExpressionExpander, IDelegateHookHandler<IQueryExpressionExpander>
    {
        private Type targetType;

        private ConventionalEntitySetProvider(Type targetType)
        {
            this.targetType = targetType;
        }

        IModelBuilder IDelegateHookHandler<IModelBuilder>.InnerHandler { get; set; }

        IModelMapper IDelegateHookHandler<IModelMapper>.InnerHandler { get; set; }

        IQueryExpressionExpander IDelegateHookHandler<IQueryExpressionExpander>.InnerHandler { get; set; }

        private IEnumerable<PropertyInfo> AddedEntitySets
        {
            get
            {
                var properties = this.targetType.GetProperties(
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);
                return properties.Where(p =>
                    !p.CanWrite && !p.GetMethod.IsPrivate &&
                    p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(IQueryable<>)).ToArray();
            }
        }

        /// <inheritdoc/>
        public static void ApplyTo(
            DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");
            var provider = new ConventionalEntitySetProvider(targetType);
            configuration.AddHookHandler<IModelBuilder>(provider);
            configuration.AddHookHandler<IModelMapper>(provider);
            configuration.AddHookHandler<IQueryExpressionExpander>(provider);
        }

        /// <inheritdoc/>
        public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context);

            IEdmModel model = null;
            var innerHandler = ((IDelegateHookHandler<IModelBuilder>)this).InnerHandler;
            if (innerHandler != null)
            {
                model = await innerHandler.GetModelAsync(context, cancellationToken);
            }

            if (model != null)
            {
                foreach (var entitySetProperty in this.AddedEntitySets)
                {
                    var container = model.EntityContainer as EdmEntityContainer;
                    var elementType = entitySetProperty
                        .PropertyType.GetGenericArguments()[0];
                    var entityType = model.SchemaElements
                        .OfType<IEdmEntityType>()
                        .SingleOrDefault(se => se.Name == elementType.Name);
                    if (entityType == null)
                    {
                        // TODO GitHubIssue#33 : Add new entity type representing entity shape
                        continue;
                    }

                    container.AddEntitySet(entitySetProperty.Name, entityType);
                }
            }

            return model;
        }

        /// <inheritdoc/>
        public bool TryGetRelevantType(
            DomainContext context,
            string name,
            out Type relevantType)
        {
            relevantType = null;
            var entitySetProperty = this.AddedEntitySets.SingleOrDefault(p => p.Name == name);
            if (entitySetProperty != null)
            {
                relevantType = entitySetProperty.PropertyType.GetGenericArguments()[0];
            }

            if (relevantType != null)
            {
                return true;
            }

            var innerHandler = ((IDelegateHookHandler<IModelMapper>)this).InnerHandler;
            return innerHandler != null && innerHandler.TryGetRelevantType(context, name, out relevantType);
        }

        /// <inheritdoc/>
        public bool TryGetRelevantType(
            DomainContext context,
            string namespaceName,
            string name,
            out Type relevantType)
        {
            relevantType = null;
            return false;
        }

        /// <inheritdoc/>
        public Expression Expand(QueryExpressionContext context)
        {
            Ensure.NotNull(context);

            var innerHandler = ((IDelegateHookHandler<IQueryExpressionExpander>)this).InnerHandler;
            if (innerHandler != null)
            {
                var result = innerHandler.Expand(context);
                if (result != null)
                {
                    return result;
                }
            }

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
                        typeof(Domain).AssemblyQualifiedName);
                    if (target == null ||
                        !this.targetType.IsAssignableFrom(target.GetType()))
                    {
                        return null;
                    }
                }

                var result = entitySetProperty.GetValue(target) as IQueryable;
                if (result != null)
                {
                    return result.Expression;
                }
            }

            return null;
        }
    }
}
