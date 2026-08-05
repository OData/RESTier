// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.AspNetCore.Model;

/// <summary>
/// A convention-based API model builder that extends a model, maps between
/// the model space and the object space, and expands a query expression.
/// </summary>
public partial class RestierWebApiModelExtender
{
    /// <summary>
    /// Gets the type of the target API that this model extender is associated with.
    /// </summary>
    public Type TargetApiType { get; }

    private readonly ICollection<PropertyInfo> _publicProperties = new List<PropertyInfo>();
    private readonly ICollection<EdmNavigationSource> _addedNavigationSources = new List<EdmNavigationSource>();

    private readonly IDictionary<IEdmEntityType, IEdmEntitySet[]> _entitySetextender =
        new Dictionary<IEdmEntityType, IEdmEntitySet[]>();

    private readonly IDictionary<IEdmEntityType, IEdmSingleton[]> _singletonextender =
        new Dictionary<IEdmEntityType, IEdmSingleton[]>();

    /// <summary>
    /// Initializes a new instance of the <see cref="RestierWebApiModelExtender"/> class.
    /// </summary>
    /// <param name="targetApiType">The target api type.</param>
    public RestierWebApiModelExtender(Type targetApiType) => this.TargetApiType = targetApiType;

    /// <summary>
    /// Gets the collection of entity set properties that have been found on the target API type.
    /// </summary>
    public ICollection<PropertyInfo> EntitySetProperties { get; } = new List<PropertyInfo>();

    /// <summary>
    /// Gets the collection of singleton properties that have been found on the target API type.
    /// </summary>
    public ICollection<PropertyInfo> SingletonProperties { get; } = new List<PropertyInfo>();

    private static bool IsEntitySetProperty(PropertyInfo property)
    {
        return property.PropertyType.IsGenericType &&
               property.PropertyType.GetGenericTypeDefinition() == typeof(IQueryable<>) &&
               property.PropertyType.GetGenericArguments()[0].IsClass;
    }

    private static bool IsSingletonProperty(PropertyInfo property) => !property.PropertyType.IsGenericType && property.PropertyType.IsClass;

    /// <summary>
    /// Gets the queryable source for an entity set or singleton based on the model reference in the context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public IQueryable GetEntitySetQuery(QueryExpressionContext context)
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

        var entitySetProperty = EntitySetProperties
            .SingleOrDefault(p => p.Name == entitySet.Name);
        if (entitySetProperty is not null)
        {
            object target = null;
            if (!entitySetProperty.GetMethod.IsStatic)
            {
                target = context.QueryContext.Api;
                if (target is null ||
                    !TargetApiType.IsInstanceOfType(target))
                {
                    return null;
                }
            }

            return entitySetProperty.GetValue(target) as IQueryable;
        }

        return null;
    }

    /// <summary>
    /// Gets the queryable source for a singleton based on the model reference in the context.
    /// </summary>
    /// <param name="context">The query context.</param>
    /// <returns>A queryable.</returns>
    public IQueryable GetSingletonQuery(QueryExpressionContext context)
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

        var singletonProperty = SingletonProperties
            .SingleOrDefault(p => p.Name == singleton.Name);
        if (singletonProperty is not null)
        {
            object target = null;
            if (!singletonProperty.GetMethod.IsStatic)
            {
                target = context.QueryContext.Api;
                if (target is null ||
                    !TargetApiType.IsInstanceOfType(target))
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

    /// <summary>
    /// Scans the target API type for declared public properties that can be used as entity sets or singletons.
    /// </summary>
    public void ScanForDeclaredPublicProperties()
    {
        var currentType = TargetApiType;
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
                    _publicProperties.All(p => p.Name != property.Name))
                {
                    _publicProperties.Add(property);
                }
            }

            currentType = currentType.BaseType;
        }
    }

    /// <summary>
    /// Builds entity sets and singletons in the model based on the public properties of the target API type.
    /// </summary>
    /// <param name="model">The model to add the Enity sets and singletons to.</param>
    public void BuildEntitySetsAndSingletons(EdmModel model)
    {
        foreach (var property in _publicProperties)
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

            var container = model.EnsureEntityContainer(TargetApiType);
            if (isEntitySet)
            {
                if (container.FindEntitySet(property.Name) is null)
                {
                    container.AddEntitySet(property.Name, entityType);
                }

                // If ODataConventionModelBuilder is used to build the model, and a entity set is added,
                // i.e. the entity set is already in the container,
                // we should add it into entitySetProperties and addedNavigationSources
                if (!EntitySetProperties.Contains(property))
                {
                    EntitySetProperties.Add(property);
                    _addedNavigationSources.Add(container.FindEntitySet(property.Name) as EdmEntitySet);
                }
            }
            else
            {
                if (container.FindSingleton(property.Name) is null)
                {
                    container.AddSingleton(property.Name, entityType);
                }

                if (!SingletonProperties.Contains(property))
                {
                    SingletonProperties.Add(property);
                    _addedNavigationSources.Add(container.FindSingleton(property.Name) as EdmSingleton);
                }
            }
        }
    }

    private IEdmEntitySet[] GetMatchingEntitySets(IEdmEntityType entityType, IEdmModel model)
    {
        if (!_entitySetextender.TryGetValue(entityType, out var matchingEntitySets))
        {
            matchingEntitySets = model.EntityContainer.EntitySets().Where(s => s.EntityType == entityType).ToArray();
            _entitySetextender.Add(entityType, matchingEntitySets);
        }

        return matchingEntitySets;
    }

    private IEdmSingleton[] GetMatchingSingletons(IEdmEntityType entityType, IEdmModel model)
    {
        if (!_singletonextender.TryGetValue(entityType, out var matchingSingletons))
        {
            matchingSingletons =  model.EntityContainer.Singletons().Where(s => s.EntityType == entityType).ToArray();
            _singletonextender.Add(entityType, matchingSingletons);
        }

        return matchingSingletons;
    }

    /// <summary>
    /// Adds navigation property bindings to the model based on the navigation sources added by this builder.
    /// </summary>
    /// <param name="model">The model to use.</param>
    public void AddNavigationPropertyBindings(IEdmModel model)
    {
        // Only add navigation property bindings for the navigation sources added by this builder.
        foreach (var navigationSource in _addedNavigationSources)
        {
            var sourceEntityType = navigationSource.EntityType;
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
}