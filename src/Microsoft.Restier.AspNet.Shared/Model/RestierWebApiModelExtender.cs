// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore.Model
#else
namespace Microsoft.Restier.AspNet.Model
#endif
{
    /// <summary>
    /// A convention-based API model builder that extends a model, maps between
    /// the model space and the object space, and expands a query expression.
    /// </summary>
    internal class RestierWebApiModelExtender
    {
        private readonly Type targetApiType;
        private readonly ICollection<PropertyInfo> publicProperties = new List<PropertyInfo>();
        private readonly ICollection<PropertyInfo> entitySetProperties = new List<PropertyInfo>();
        private readonly ICollection<PropertyInfo> singletonProperties = new List<PropertyInfo>();
        private readonly ICollection<EdmNavigationSource> addedNavigationSources = new List<EdmNavigationSource>();

        private readonly IDictionary<IEdmEntityType, IEdmEntitySet[]> entitySetCache =
            new Dictionary<IEdmEntityType, IEdmEntitySet[]>();

        private readonly IDictionary<IEdmEntityType, IEdmSingleton[]> singletonCache =
            new Dictionary<IEdmEntityType, IEdmSingleton[]>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierWebApiModelExtender"/> class.
        /// </summary>
        /// <param name="targetApiType">The target api type.</param>
        internal RestierWebApiModelExtender(Type targetApiType) => this.targetApiType = targetApiType;

        private static bool IsEntitySetProperty(PropertyInfo property)
        {
            return property.PropertyType.IsGenericType &&
                   property.PropertyType.GetGenericTypeDefinition() == typeof(IQueryable<>) &&
                   property.PropertyType.GetGenericArguments()[0].IsClass;
        }

        private static bool IsSingletonProperty(PropertyInfo property) => !property.PropertyType.IsGenericType && property.PropertyType.IsClass;

        private IQueryable GetEntitySetQuery(QueryExpressionContext context)
        {
            Ensure.NotNull(context, nameof(context));
            if (context.ModelReference is null)
            {
                return null;
            }

            if (!(context.ModelReference is DataSourceStubModelReference dataSourceStubReference))
            {
                return null;
            }

            if (!(dataSourceStubReference.Element is IEdmEntitySet entitySet))
            {
                return null;
            }

            var entitySetProperty = entitySetProperties
                .SingleOrDefault(p => p.Name == entitySet.Name);
            if (entitySetProperty is not null)
            {
                object target = null;
                if (!entitySetProperty.GetMethod.IsStatic)
                {
                    target = context.QueryContext.Api;
                    if (target is null ||
                        !targetApiType.IsInstanceOfType(target))
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
            Ensure.NotNull(context, nameof(context));
            if (context.ModelReference is null)
            {
                return null;
            }

            if (!(context.ModelReference is DataSourceStubModelReference dataSourceStubReference))
            {
                return null;
            }

            if (!(dataSourceStubReference.Element is IEdmSingleton singleton))
            {
                return null;
            }

            var singletonProperty = singletonProperties
                .SingleOrDefault(p => p.Name == singleton.Name);
            if (singletonProperty is not null)
            {
                object target = null;
                if (!singletonProperty.GetMethod.IsStatic)
                {
                    target = context.QueryContext.Api;
                    if (target is null ||
                        !targetApiType.IsInstanceOfType(target))
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
            var currentType = targetApiType;
            while (currentType is not null && currentType != typeof(ApiBase))
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

        private void BuildEntitySetsAndSingletons(EdmModel model)
        {
            foreach (var property in publicProperties)
            {
                var resourceAttribute = property.GetCustomAttributes<ResourceAttribute>(true).FirstOrDefault();
                if (resourceAttribute is null)
                {
                    continue;
                }

                var isEntitySet = IsEntitySetProperty(property);
                var isSingleton = IsSingletonProperty(property);
                if (!isSingleton && !isEntitySet)
                {
                    // This means property type is not IQueryable<T> when indicating an entityset
                    // or not non-generic type when indicating a singleton
                    continue;
                }

                var propertyType = property.PropertyType;
                if (isEntitySet)
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }

                var entityType = model.FindDeclaredType(propertyType.FullName) as IEdmEntityType;
                if (entityType is null)
                {
                    // Skip property whose entity type has not been declared yet.
                    continue;
                }

                var container = model.EnsureEntityContainer(targetApiType);
                if (isEntitySet)
                {
                    if (container.FindEntitySet(property.Name) is null)
                    {
                        container.AddEntitySet(property.Name, entityType);
                    }

                    // If ODataConventionModelBuilder is used to build the model, and a entity set is added,
                    // i.e. the entity set is already in the container,
                    // we should add it into entitySetProperties and addedNavigationSources
                    if (!entitySetProperties.Contains(property))
                    {
                        entitySetProperties.Add(property);
                        addedNavigationSources.Add(container.FindEntitySet(property.Name) as EdmEntitySet);
                    }
                }
                else
                {
                    if (container.FindSingleton(property.Name) is null)
                    {
                        container.AddSingleton(property.Name, entityType);
                    }

                    if (!singletonProperties.Contains(property))
                    {
                        singletonProperties.Add(property);
                        addedNavigationSources.Add(container.FindSingleton(property.Name) as EdmSingleton);
                    }
                }
            }
        }

        private IEdmEntitySet[] GetMatchingEntitySets(IEdmEntityType entityType, IEdmModel model)
        {
            if (!entitySetCache.TryGetValue(entityType, out var matchingEntitySets))
            {
                matchingEntitySets = model.EntityContainer.EntitySets().Where(s => s.EntityType() == entityType).ToArray();
                entitySetCache.Add(entityType, matchingEntitySets);
            }

            return matchingEntitySets;
        }

        private IEdmSingleton[] GetMatchingSingletons(IEdmEntityType entityType, IEdmModel model)
        {
            if (!singletonCache.TryGetValue(entityType, out var matchingSingletons))
            {
                matchingSingletons =  model.EntityContainer.Singletons().Where(s => s.EntityType() == entityType).ToArray();
                singletonCache.Add(entityType, matchingSingletons);
            }

            return matchingSingletons;
        }

        private void AddNavigationPropertyBindings(IEdmModel model)
        {
            // Only add navigation property bindings for the navigation sources added by this builder.
            foreach (var navigationSource in addedNavigationSources)
            {
                var sourceEntityType = navigationSource.EntityType();
                foreach (var navigationProperty in sourceEntityType.NavigationProperties())
                {
                    var targetEntityType = navigationProperty.ToEntityType();
                    var matchingEntitySets = GetMatchingEntitySets(targetEntityType, model);
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
                        var matchingSingletons = GetMatchingSingletons(targetEntityType, model);
                        if (matchingEntitySets.Length == 1 && matchingSingletons.Length == 0)
                        {
                            targetNavigationSource = matchingEntitySets[0];
                        }
                        else if (matchingEntitySets.Length == 0 && matchingSingletons.Length == 1)
                        {
                            targetNavigationSource = matchingSingletons[0];
                        }
                    }

                    if (targetNavigationSource is not null)
                    {
                        navigationSource.AddNavigationTarget(navigationProperty, targetNavigationSource);
                    }
                }
            }
        }

        /// <summary>
        /// Internal model Builder.
        /// </summary>
        internal class ModelBuilder : IModelBuilder
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ModelBuilder"/> class.
            /// </summary>
            /// <param name="modelCache">The model cache.</param>
            public ModelBuilder(RestierWebApiModelExtender modelCache) => ModelCache = modelCache;

            /// <summary>
            /// Gets a reference to the inner model builder.
            /// </summary>
            public IModelBuilder InnerModelBuilder { get; private set; }

            private RestierWebApiModelExtender ModelCache { get; set; }

            /// <inheritdoc/>
            public IEdmModel GetModel(ModelContext context)
            {
                Ensure.NotNull(context, nameof(context));

                var modelReturned = GetModelReturnedByInnerHandler(context);
                if (modelReturned is null)
                {
                    // There is no model returned so return an empty model.
                    var emptyModel = new EdmModel();
                    emptyModel.EnsureEntityContainer(ModelCache.targetApiType);
                    return emptyModel;
                }

                var edmModel = modelReturned as EdmModel;
                if (edmModel is null)
                {
                    // The model returned is not an EDM model.
                    return modelReturned;
                }

                ModelCache.ScanForDeclaredPublicProperties();
                ModelCache.BuildEntitySetsAndSingletons(edmModel);
                ModelCache.AddNavigationPropertyBindings(edmModel);
                return edmModel;
            }

            private IEdmModel GetModelReturnedByInnerHandler(ModelContext context)
            {
                var innerHandler = InnerModelBuilder;
                if (innerHandler is not null)
                {
                    return innerHandler.GetModel(context);
                }

                return null;
            }
        }
        /// <summary>
        /// Internal Model Mapper.
        /// </summary>
        internal class ModelMapper : IModelMapper
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ModelMapper"/> class.
            /// </summary>
            /// <param name="modelCache">The model cache.</param>
            public ModelMapper(RestierWebApiModelExtender modelCache) => ModelCache = modelCache;

            /// <summary>
            /// Gets the model Cache.
            public RestierWebApiModelExtender ModelCache { get; set; }

            private IModelMapper InnerModelMapper { get; set; }

            /// <inheritdoc/>
            public bool TryGetRelevantType(ModelContext context, string name, out Type relevantType)
            {
                if (InnerModelMapper is not null &&
                    InnerModelMapper.TryGetRelevantType(context, name, out relevantType))
                {
                    return true;
                }

                relevantType = null;
                var entitySetProperty = ModelCache.entitySetProperties.SingleOrDefault(p => p.Name == name);
                if (entitySetProperty is not null)
                {
                    relevantType = entitySetProperty.PropertyType.GetGenericArguments()[0];
                }

                if (relevantType is null)
                {
                    var singletonProperty = ModelCache.singletonProperties.SingleOrDefault(p => p.Name == name);
                    if (singletonProperty is not null)
                    {
                        relevantType = singletonProperty.PropertyType;
                    }
                }

                return relevantType is not null;
            }

            /// <inheritdoc/>
            public bool TryGetRelevantType(
                ModelContext context,
                string namespaceName,
                string name,
                out Type relevantType)
            {
                if (InnerModelMapper is not null &&
                    InnerModelMapper.TryGetRelevantType(context, namespaceName, name, out relevantType))
                {
                    return true;
                }

                relevantType = null;
                return false;
            }
        }

        /// <summary>
        /// Restier <see cref="IQueryExpressionExpander"/> implementation. Handles Expand in a Query expression.
        /// </summary>
        internal class QueryExpressionExpander : IQueryExpressionExpander
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="QueryExpressionExpander"/> class.
            /// </summary>
            /// <param name="modelCache">The model cache.</param>
            public QueryExpressionExpander(RestierWebApiModelExtender modelCache) => ModelCache = modelCache;

            /// <summary>
            /// Gets or sets the inner handler.
            /// </summary>
            public IQueryExpressionExpander InnerHandler { get; set; }

            private RestierWebApiModelExtender ModelCache { get; set; }

            /// <inheritdoc/>
            public Expression Expand(QueryExpressionContext context)
            {
                Ensure.NotNull(context, nameof(context));

                var result = CallInner(context);
                if (result is not null)
                {
                    return result;
                }

                // Ensure this query constructs from DataSourceStub.
                if (context.ModelReference is DataSourceStubModelReference)
                {
                    // Only expand entity set query which returns IQueryable<T>.
                    var query = ModelCache.GetEntitySetQuery(context);
                    if (query is not null)
                    {
                        return query.Expression;
                    }
                }

                // No expansion happened just return the node itself.
                return context.VisitedNode;
            }

            private Expression CallInner(QueryExpressionContext context)
            {
                return InnerHandler?.Expand(context);
            }
        }

        /// <summary>
        /// Gets the source of the query.
        /// </summary>
        internal class QueryExpressionSourcer : IQueryExpressionSourcer
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="QueryExpressionSourcer"/> class.
            /// </summary>
            /// <param name="modelCache">The model cache.</param>
            public QueryExpressionSourcer(RestierWebApiModelExtender modelCache) => ModelCache = modelCache;

            /// <summary>
            /// Gets or sets the inner handler.
            /// </summary>
            public IQueryExpressionSourcer InnerHandler { get; set; }

            private RestierWebApiModelExtender ModelCache { get; set; }

            /// <inheritdoc/>
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                var result = CallInner(context, embedded);
                if (result is not null)
                {
                    // Call the provider's sourcer to find the source of the query.
                    return result;
                }

                // This sourcer ONLY deals with queries that cannot be addressed by the provider
                // such as a singleton query that cannot be sourced by the EF provider, etc.
                var query = ModelCache.GetEntitySetQuery(context) ?? ModelCache.GetSingletonQuery(context);
                if (query is not null)
                {
                    return Expression.Constant(query);
                }

                return null;
            }

            private Expression CallInner(QueryExpressionContext context, bool embedded)
            {
                if (InnerHandler is not null)
                {
                    return InnerHandler.ReplaceQueryableSource(context, embedded);
                }

                return null;
            }
        }
    }
}
