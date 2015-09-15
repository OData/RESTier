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
    /// A conventional domain model builder that extends a model, maps between
    /// the model space and the object space, and expands a query expression.
    /// </summary>
    internal class ConventionalDomainModelBuilder :
        IModelBuilder, IDelegateHookHandler<IModelBuilder>,
        IModelMapper, IDelegateHookHandler<IModelMapper>,
        IQueryExpressionExpander
    {
        private Type targetType;
        private ICollection<PropertyInfo> entitySetProperties = new List<PropertyInfo>();
        private ICollection<PropertyInfo> singletonProperties = new List<PropertyInfo>();

        private ConventionalDomainModelBuilder(Type targetType)
        {
            this.targetType = targetType;
        }

        IModelBuilder IDelegateHookHandler<IModelBuilder>.InnerHandler { get; set; }

        IModelMapper IDelegateHookHandler<IModelMapper>.InnerHandler { get; set; }

        private IEnumerable<PropertyInfo> PublicPropertiesOnTargetType
        {
            get
            {
                var publicPropertiesOnTargetType = new HashSet<PropertyInfo>();
                var currentType = this.targetType;
                while (currentType != null && currentType != typeof(DomainBase))
                {
                    var publicPropertiesDeclaredOnCurrentType = currentType.GetProperties(
                        BindingFlags.Public |
                        BindingFlags.Static |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly);

                    foreach (var property in publicPropertiesDeclaredOnCurrentType)
                    {
                        if (property.CanRead &&
                            publicPropertiesOnTargetType.All(p => p.Name != property.Name))
                        {
                            publicPropertiesOnTargetType.Add(property);
                        }
                    }

                    currentType = currentType.BaseType;
                }

                return publicPropertiesOnTargetType;
            }
        }

        /// <inheritdoc/>
        public static void ApplyTo(
            DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");

            var provider = new ConventionalDomainModelBuilder(targetType);
            configuration.AddHookHandler<IModelBuilder>(provider);
            configuration.AddHookHandler<IModelMapper>(provider);
            configuration.AddHookPoint(typeof(IQueryExpressionExpander), provider);
        }

        /// <inheritdoc/>
        public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context);

            IEdmModel modelReturned = await GetModelReturnedByInnerHandlerAsync(context, cancellationToken);
            if (modelReturned == null)
            {
                // There is no model returned so return an empty model.
                var emptyModel = new EdmModel();
                EnsureEntityContainer(context, emptyModel);
                return emptyModel;
            }

            EdmModel edmModel = modelReturned as EdmModel;
            if (edmModel == null)
            {
                // The model returned is not an EDM model.
                return modelReturned;
            }

            this.BuildEntitySetsAndSingletons(context, edmModel);
            return edmModel;
        }

        /// <inheritdoc/>
        public bool TryGetRelevantType(
            DomainContext context,
            string name,
            out Type relevantType)
        {
            relevantType = null;
            var entitySetProperty = this.entitySetProperties.SingleOrDefault(p => p.Name == name);
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

            var entitySetProperty = this.entitySetProperties
                .SingleOrDefault(p => p.Name == entitySet.Name);
            if (entitySetProperty != null)
            {
                object target = null;
                if (!entitySetProperty.GetMethod.IsStatic)
                {
                    target = GetDomainInstance(context.QueryContext.DomainContext);
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

        private static object GetDomainInstance(DomainContext context)
        {
            return context.GetProperty(typeof(Domain).AssemblyQualifiedName);
        }

        private static bool IsEntitySetProperty(PropertyInfo property)
        {
            return property.PropertyType.IsGenericType &&
                   property.PropertyType.GetGenericTypeDefinition() == typeof(IQueryable<>) &&
                   property.PropertyType.GetGenericArguments()[0].IsClass;
        }

        private static bool IsSingletonProperty(PropertyInfo property)
        {
            return !property.PropertyType.IsGenericType && property.PropertyType.IsClass;
        }

        private static EdmEntityContainer EnsureEntityContainer(InvocationContext context, EdmModel model)
        {
            var container = (EdmEntityContainer)model.EntityContainer;
            if (container == null)
            {
                var domainInstance = GetDomainInstance(context.DomainContext);
                var domainNamespace = domainInstance.GetType().Namespace;
                container = new EdmEntityContainer(domainNamespace, "DefaultContainer");
                model.AddElement(container);
            }

            return container;
        }

        private async Task<IEdmModel> GetModelReturnedByInnerHandlerAsync(
            InvocationContext context, CancellationToken cancellationToken)
        {
            var innerHandler = ((IDelegateHookHandler<IModelBuilder>)this).InnerHandler;
            if (innerHandler != null)
            {
                return await innerHandler.GetModelAsync(context, cancellationToken);
            }

            return null;
        }

        private void BuildEntitySetsAndSingletons(InvocationContext context, EdmModel model)
        {
            var configuration = context.DomainContext.Configuration;
            foreach (var property in this.PublicPropertiesOnTargetType)
            {
                if (configuration.IsPropertyIgnored(property.Name))
                {
                    continue;
                }

                var isEntitySet = IsEntitySetProperty(property);
                if (!isEntitySet)
                {
                    if (!IsSingletonProperty(property))
                    {
                        continue;
                    }
                }

                var entityType = property.PropertyType;
                if (isEntitySet)
                {
                    entityType = entityType.GetGenericArguments()[0];
                }

                var edmEntityType = model.FindDeclaredType(entityType.FullName) as IEdmEntityType;
                if (edmEntityType == null)
                {
                    // Skip property whose entity type has not been declared yet.
                    continue;
                }

                var container = EnsureEntityContainer(context, model);
                if (isEntitySet)
                {
                    if (container.FindEntitySet(property.Name) == null)
                    {
                        this.entitySetProperties.Add(property);
                        container.AddEntitySet(property.Name, edmEntityType);
                    }
                }
                else
                {
                    if (container.FindSingleton(property.Name) == null)
                    {
                        this.singletonProperties.Add(property);
                        container.AddSingleton(property.Name, edmEntityType);
                    }
                }
            }
        }
    }
}
