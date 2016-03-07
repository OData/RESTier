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
    /// A convention-based API model builder that extends a model, maps between
    /// the model space and the object space, and expands a query expression.
    /// </summary>
    internal class ConventionBasedApiModelBuilder
    {
        private readonly Type targetType;
        private readonly ICollection<PropertyInfo> publicProperties = new List<PropertyInfo>();
        private readonly ICollection<PropertyInfo> entitySetProperties = new List<PropertyInfo>();
        private readonly ICollection<PropertyInfo> singletonProperties = new List<PropertyInfo>();
        private readonly ICollection<EdmNavigationSource> addedNavigationSources = new List<EdmNavigationSource>();

        private readonly IDictionary<IEdmEntityType, IEdmEntitySet[]> entitySetCache =
            new Dictionary<IEdmEntityType, IEdmEntitySet[]>();

        private readonly IDictionary<IEdmEntityType, IEdmSingleton[]> singletonCache =
            new Dictionary<IEdmEntityType, IEdmSingleton[]>();

        private ConventionBasedApiModelBuilder(Type targetType)
        {
            this.targetType = targetType;
        }

        public static void ApplyTo(
            ApiBuilder builder,
            Type targetType)
        {
            Ensure.NotNull(builder, "builder");
            Ensure.NotNull(targetType, "targetType");

            // The model builder must maintain a singleton life time, for holding states and being injected into
            // some other services.
            builder.AddInstance(new ConventionBasedApiModelBuilder(targetType));

            builder.ChainPrevious<IModelBuilder, ModelBuilder>();
            builder.ChainPrevious<IModelMapper, ModelMapper>();
            builder.ChainPrevious<IQueryExpressionExpander, QueryExpressionExpander>();
            builder.ChainPrevious<IQueryExpressionSourcer, QueryExpressionSourcer>();
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

        private IQueryable GetEntitySetQuery(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");
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

            var entitySetProperty = this.entitySetProperties
                .SingleOrDefault(p => p.Name == entitySet.Name);
            if (entitySetProperty != null)
            {
                object target = null;
                if (!entitySetProperty.GetMethod.IsStatic)
                {
                    target = context.QueryContext.ApiContext
                        .GetProperty(typeof(Api).AssemblyQualifiedName);
                    if (target == null ||
                        !this.targetType.IsAssignableFrom(target.GetType()))
                    {
                        return null;
                    }
                }

                return entitySetProperty.GetValue(target) as IQueryable;
            }

            return null;
        }

        private IQueryable GetSingletonQuery(QueryExpressionContext context)
        {
            Ensure.NotNull(context, "context");
            if (context.ModelReference == null)
            {
                return null;
            }

            var apiDataReference = context.ModelReference as ApiDataReference;
            if (apiDataReference == null)
            {
                return null;
            }

            var singleton = apiDataReference.Element as IEdmSingleton;
            if (singleton == null)
            {
                return null;
            }

            var singletonProperty = this.singletonProperties
                .SingleOrDefault(p => p.Name == singleton.Name);
            if (singletonProperty != null)
            {
                object target = null;
                if (!singletonProperty.GetMethod.IsStatic)
                {
                    target = context.QueryContext.ApiContext
                        .GetProperty(typeof(Api).AssemblyQualifiedName);
                    if (target == null ||
                        !this.targetType.IsAssignableFrom(target.GetType()))
                    {
                        return null;
                    }
                }

                var value = Array.CreateInstance(singletonProperty.PropertyType, 1);
                value.SetValue(singletonProperty.GetValue(target), 0);
                return value.AsQueryable();
            }

            return null;
        }

        private void ScanForDeclaredPublicProperties()
        {
            var currentType = this.targetType;
            while (currentType != null && currentType != typeof(ApiBase))
            {
                var publicPropertiesDeclaredOnCurrentType = currentType.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Static |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);

                foreach (var property in publicPropertiesDeclaredOnCurrentType)
                {
                    if (property.CanRead &&
                        publicProperties.All(p => p.Name != property.Name))
                    {
                        publicProperties.Add(property);
                    }
                }

                currentType = currentType.BaseType;
            }
        }

        private void BuildEntitySetsAndSingletons(InvocationContext context, EdmModel model)
        {
            var configuration = context.ApiContext.Configuration;
            foreach (var property in this.publicProperties)
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

                var propertyType = property.PropertyType;
                if (isEntitySet)
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }

                var entityType = model.FindDeclaredType(propertyType.FullName) as IEdmEntityType;
                if (entityType == null)
                {
                    // Skip property whose entity type has not been declared yet.
                    continue;
                }

                var container = model.EnsureEntityContainer(this.targetType);
                if (isEntitySet)
                {
                    if (container.FindEntitySet(property.Name) == null)
                    {
                        this.entitySetProperties.Add(property);
                        var addedEntitySet = container.AddEntitySet(property.Name, entityType);
                        this.addedNavigationSources.Add(addedEntitySet);
                    }
                }
                else
                {
                    if (container.FindSingleton(property.Name) == null)
                    {
                        this.singletonProperties.Add(property);
                        var addedSingleton = container.AddSingleton(property.Name, entityType);
                        this.addedNavigationSources.Add(addedSingleton);
                    }
                }
            }
        }

        private IEdmEntitySet[] GetMatchingEntitySets(IEdmEntityType entityType, IEdmModel model)
        {
            IEdmEntitySet[] matchingEntitySets;
            if (!entitySetCache.TryGetValue(entityType, out matchingEntitySets))
            {
                matchingEntitySets =
                    model.EntityContainer.EntitySets().Where(s => s.EntityType() == entityType).ToArray();
                entitySetCache.Add(entityType, matchingEntitySets);
            }

            return matchingEntitySets;
        }

        private IEdmSingleton[] GetMatchingSingletons(IEdmEntityType entityType, IEdmModel model)
        {
            IEdmSingleton[] matchingSingletons;
            if (!singletonCache.TryGetValue(entityType, out matchingSingletons))
            {
                matchingSingletons =
                    model.EntityContainer.Singletons().Where(s => s.EntityType() == entityType).ToArray();
                singletonCache.Add(entityType, matchingSingletons);
            }

            return matchingSingletons;
        }

        private void AddNavigationPropertyBindings(IEdmModel model)
        {
            // Only add navigation property bindings for the navigation sources added by this builder.
            foreach (var navigationSource in this.addedNavigationSources)
            {
                var sourceEntityType = navigationSource.EntityType();
                foreach (var navigationProperty in sourceEntityType.NavigationProperties())
                {
                    var targetEntityType = navigationProperty.ToEntityType();
                    var matchingEntitySets = this.GetMatchingEntitySets(targetEntityType, model);
                    IEdmNavigationSource targetNavigationSource = null;
                    if (navigationProperty.Type.IsCollection())
                    {
                        // Collection navigation property can only bind to entity set.
                        if (matchingEntitySets.Length == 1)
                        {
                            targetNavigationSource = matchingEntitySets[0];
                        }
                    }
                    else
                    {
                        // Singleton navigation property can bind to either entity set or singleton.
                        var matchingSingletons = this.GetMatchingSingletons(targetEntityType, model);
                        if (matchingEntitySets.Length == 1 && matchingSingletons.Length == 0)
                        {
                            targetNavigationSource = matchingEntitySets[0];
                        }
                        else if (matchingEntitySets.Length == 0 && matchingSingletons.Length == 1)
                        {
                            targetNavigationSource = matchingSingletons[0];
                        }
                    }

                    if (targetNavigationSource != null)
                    {
                        navigationSource.AddNavigationTarget(navigationProperty, targetNavigationSource);
                    }
                }
            }
        }

        internal class ModelBuilder : IModelBuilder
        {
            public ModelBuilder(ConventionBasedApiModelBuilder modelCache)
            {
                ModelCache = modelCache;
            }

            public IModelBuilder InnerModelBuilder { get; private set; }

            private ConventionBasedApiModelBuilder ModelCache { get; set; }

            /// <inheritdoc/>
            public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                Ensure.NotNull(context, "context");

                IEdmModel modelReturned = await GetModelReturnedByInnerHandlerAsync(context, cancellationToken);
                if (modelReturned == null)
                {
                    // There is no model returned so return an empty model.
                    var emptyModel = new EdmModel();
                    emptyModel.EnsureEntityContainer(ModelCache.targetType);
                    return emptyModel;
                }

                EdmModel edmModel = modelReturned as EdmModel;
                if (edmModel == null)
                {
                    // The model returned is not an EDM model.
                    return modelReturned;
                }

                ModelCache.ScanForDeclaredPublicProperties();
                ModelCache.BuildEntitySetsAndSingletons(context, edmModel);
                ModelCache.AddNavigationPropertyBindings(edmModel);
                return edmModel;
            }

            private async Task<IEdmModel> GetModelReturnedByInnerHandlerAsync(
                InvocationContext context, CancellationToken cancellationToken)
            {
                var innerHandler = InnerModelBuilder;
                if (innerHandler != null)
                {
                    return await innerHandler.GetModelAsync(context, cancellationToken);
                }

                return null;
            }
        }

        internal class ModelMapper : IModelMapper
        {
            public ModelMapper(ConventionBasedApiModelBuilder modelCache)
            {
                ModelCache = modelCache;
            }

            public ConventionBasedApiModelBuilder ModelCache { get; set; }

            private IModelMapper InnerModelMapper { get; set; }

            /// <inheritdoc/>
            public bool TryGetRelevantType(
                ApiContext context,
                string name,
                out Type relevantType)
            {
                if (this.InnerModelMapper != null &&
                    this.InnerModelMapper.TryGetRelevantType(context, name, out relevantType))
                {
                    return true;
                }

                relevantType = null;
                var entitySetProperty = this.ModelCache.entitySetProperties.SingleOrDefault(p => p.Name == name);
                if (entitySetProperty != null)
                {
                    relevantType = entitySetProperty.PropertyType.GetGenericArguments()[0];
                }

                if (relevantType == null)
                {
                    var singletonProperty = this.ModelCache.singletonProperties.SingleOrDefault(p => p.Name == name);
                    if (singletonProperty != null)
                    {
                        relevantType = singletonProperty.PropertyType;
                    }
                }

                return relevantType != null;
            }

            /// <inheritdoc/>
            public bool TryGetRelevantType(
                ApiContext context,
                string namespaceName,
                string name,
                out Type relevantType)
            {
                if (this.InnerModelMapper != null &&
                    this.InnerModelMapper.TryGetRelevantType(context, namespaceName, name, out relevantType))
                {
                    return true;
                }

                relevantType = null;
                return false;
            }
        }

        internal class QueryExpressionExpander : IQueryExpressionExpander
        {
            public QueryExpressionExpander(ConventionBasedApiModelBuilder modelCache)
            {
                ModelCache = modelCache;
            }

            /// <inheritdoc/>
            public IQueryExpressionExpander InnerHandler { get; set; }

            private ConventionBasedApiModelBuilder ModelCache { get; set; }

            /// <inheritdoc/>
            public Expression Expand(QueryExpressionContext context)
            {
                Ensure.NotNull(context, "context");

                var result = CallInner(context);
                if (result != null)
                {
                    return result;
                }

                // Ensure this query constructs from ApiData.
                if (context.ModelReference is ApiDataReference)
                {
                    // Only expand entity set query which returns IQueryable<T>.
                    var query = ModelCache.GetEntitySetQuery(context);
                    if (query != null)
                    {
                        return query.Expression;
                    }
                }

                // No expansion happened just return the node itself.
                return context.VisitedNode;
            }

            private Expression CallInner(QueryExpressionContext context)
            {
                if (this.InnerHandler != null)
                {
                    return this.InnerHandler.Expand(context);
                }

                return null;
            }
        }

        internal class QueryExpressionSourcer : IQueryExpressionSourcer
        {
            public QueryExpressionSourcer(ConventionBasedApiModelBuilder modelCache)
            {
                ModelCache = modelCache;
            }

            public IQueryExpressionSourcer InnerHandler { get; set; }

            private ConventionBasedApiModelBuilder ModelCache { get; set; }

            /// <inheritdoc/>
            public Expression Source(QueryExpressionContext context, bool embedded)
            {
                var result = CallInner(context, embedded);
                if (result != null)
                {
                    // Call the provider's sourcer to find the source of the query.
                    return result;
                }

                // This sourcer ONLY deals with queries that cannot be addressed by the provider
                // such as a singleton query that cannot be sourced by the EF provider, etc.
                var query = ModelCache.GetEntitySetQuery(context) ?? ModelCache.GetSingletonQuery(context);
                if (query != null)
                {
                    return Expression.Constant(query);
                }

                return null;
            }

            private Expression CallInner(QueryExpressionContext context, bool embedded)
            {
                if (this.InnerHandler != null)
                {
                    return this.InnerHandler.Source(context, embedded);
                }

                return null;
            }
        }
    }
}
