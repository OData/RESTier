// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;

namespace Microsoft.Data.Domain.Conventions
{
    using Model;
    using Query;

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
                    // TODO: add new entity type representing entity shape
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
