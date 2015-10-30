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
    internal class ConventionBasedApiModelBuilder :
        IModelBuilder, IDelegateHookHandler<IModelBuilder>,
        IModelMapper, IDelegateHookHandler<IModelMapper>,
        IQueryExpressionExpander,
        IQueryExpressionSourcer, IDelegateHookHandler<IQueryExpressionSourcer>
    {
        private readonly Type targetType;
        private readonly ICollection<PropertyInfo> publicProperties = new List<PropertyInfo>();
        private readonly ICollection<PropertyInfo> entitySetProperties = new List<PropertyInfo>();
        private readonly ICollection<PropertyInfo> singletonProperties = new List<PropertyInfo>();

        private ConventionBasedApiModelBuilder(Type targetType)
        {
            this.targetType = targetType;
        }

        IModelBuilder IDelegateHookHandler<IModelBuilder>.InnerHandler { get; set; }

        IModelMapper IDelegateHookHandler<IModelMapper>.InnerHandler { get; set; }

        IQueryExpressionSourcer IDelegateHookHandler<IQueryExpressionSourcer>.InnerHandler { get; set; }

        /// <inheritdoc/>
        public static void ApplyTo(
            ApiConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");

            var provider = new ConventionBasedApiModelBuilder(targetType);
            configuration.AddHookHandler<IModelBuilder>(provider);
            configuration.AddHookHandler<IModelMapper>(provider);
            configuration.AddHookHandler<IQueryExpressionExpander>(provider);
            configuration.AddHookHandler<IQueryExpressionSourcer>(provider);
        }

        /// <inheritdoc/>
        public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");

            IEdmModel modelReturned = await GetModelReturnedByInnerHandlerAsync(context, cancellationToken);
            if (modelReturned == null)
            {
                // There is no model returned so return an empty model.
                var emptyModel = new EdmModel();
                emptyModel.EnsureEntityContainer(this.targetType);
                return emptyModel;
            }

            EdmModel edmModel = modelReturned as EdmModel;
            if (edmModel == null)
            {
                // The model returned is not an EDM model.
                return modelReturned;
            }

            this.ScanForDeclaredPublicProperties();
            this.BuildEntitySetsAndSingletons(context, edmModel);
            return edmModel;
        }

        /// <inheritdoc/>
        public bool TryGetRelevantType(
            ApiContext context,
            string name,
            out Type relevantType)
        {
            relevantType = null;
            var entitySetProperty = this.entitySetProperties.SingleOrDefault(p => p.Name == name);
            if (entitySetProperty != null)
            {
                relevantType = entitySetProperty.PropertyType.GetGenericArguments()[0];
            }

            if (relevantType == null)
            {
                var singletonProperty = this.singletonProperties.SingleOrDefault(p => p.Name == name);
                if (singletonProperty != null)
                {
                    relevantType = singletonProperty.PropertyType;
                }
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
            ApiContext context,
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
            IQueryable result = GetEntitySetQuery(context);

            if (result != null)
            {
                // Only Expand to expression of method call on ApiData class
                var methodCall = result.Expression as MethodCallExpression;
                if (methodCall != null)
                {
                    var method = methodCall.Method;
                    if (method.DeclaringType == typeof(ApiData) && method.Name != "Value")
                    {
                        return null;
                    }
                    else
                    {
                        return result.Expression;
                    }
                }

                return null;
            }

            return null;
        }

        /// <inheritdoc/>
        public Expression Source(QueryExpressionContext context, bool embedded)
        {
            var innerHandler = ((IDelegateHookHandler<IQueryExpressionSourcer>)this).InnerHandler;
            if (innerHandler != null)
            {
                var result = innerHandler.Source(context, embedded);
                if (result != null)
                {
                    return result;
                }
            }

            var query = GetEntitySetQuery(context) ?? GetSingletonQuery(context);
            if (query != null)
            {
                return Expression.Constant(query);
            }

            return null;
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
                        container.AddEntitySet(property.Name, entityType);
                    }
                }
                else
                {
                    if (container.FindSingleton(property.Name) == null)
                    {
                        this.singletonProperties.Add(property);
                        container.AddSingleton(property.Name, entityType);
                    }
                }
            }
        }
    }
}
