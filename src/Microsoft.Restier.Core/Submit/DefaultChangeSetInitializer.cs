using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Submit
{

    /// <summary>
    /// Provides a default implementation of the <see cref="IChangeSetInitializer"/> interface.
    /// </summary>
    public class DefaultChangeSetInitializer : IChangeSetInitializer
    {

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            if (context.ChangeSet == null)
            {
                context.ChangeSet = new ChangeSet();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Resolves the CLR PropertyInfo for a navigation property on an entity type.
        /// </summary>
        protected static PropertyInfo GetNavigationPropertyInfo(Type entityType, string navigationPropertyName)
        {
            Ensure.NotNull(entityType, nameof(entityType));
            Ensure.NotNull(navigationPropertyName, nameof(navigationPropertyName));
            return entityType.GetProperty(navigationPropertyName)
                ?? throw new InvalidOperationException($"Navigation property '{navigationPropertyName}' not found on type '{entityType.Name}'.");
        }

        /// <summary>
        /// Reads key property values from a materialized entity using the EDM model.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> GetKeyValues(object entity, IEdmEntityType edmType, IEdmModel model)
        {
            Ensure.NotNull(entity, nameof(entity));
            Ensure.NotNull(edmType, nameof(edmType));

            var keys = new Dictionary<string, object>();
            foreach (var keyProperty in edmType.Key())
            {
                var clrProperty = entity.GetType().GetProperty(keyProperty.Name);
                if (clrProperty is not null)
                {
                    keys[keyProperty.Name] = clrProperty.GetValue(entity);
                }
            }

            return keys;
        }

        /// <summary>
        /// Checks whether a navigation property has containment semantics.
        /// </summary>
        protected static bool IsContainedNavigation(IEdmModel model, IEdmEntityType entityType, string navigationPropertyName)
        {
            Ensure.NotNull(model, nameof(model));
            Ensure.NotNull(entityType, nameof(entityType));

            var navProp = entityType.FindProperty(navigationPropertyName) as IEdmNavigationProperty;
            return navProp?.ContainsTarget ?? false;
        }

        /// <summary>
        /// Sets a navigation property reference on an entity (for single nav props).
        /// </summary>
        protected static void SetNavigationProperty(object entity, string navigationPropertyName, object relatedEntity)
        {
            var navPropInfo = GetNavigationPropertyInfo(entity.GetType(), navigationPropertyName);
            navPropInfo.SetValue(entity, relatedEntity);
        }

        /// <summary>
        /// Adds an entity to a collection navigation property.
        /// </summary>
        protected static void AddToCollectionNavigationProperty(object entity, string navigationPropertyName, object relatedEntity)
        {
            var navPropInfo = GetNavigationPropertyInfo(entity.GetType(), navigationPropertyName);
            var collection = navPropInfo.GetValue(entity);
            if (collection is null)
            {
                throw new InvalidOperationException($"Collection navigation property '{navigationPropertyName}' on type '{entity.GetType().Name}' is null. Ensure it is initialized.");
            }

            // Use IList.Add for broad compatibility (ObservableCollection, List, etc.)
            if (collection is IList list)
            {
                list.Add(relatedEntity);
                return;
            }

            // Fall back to reflection-based Add
            var addMethod = collection.GetType().GetMethod("Add");
            if (addMethod is not null)
            {
                addMethod.Invoke(collection, new[] { relatedEntity });
                return;
            }

            throw new InvalidOperationException($"Cannot add to collection navigation property '{navigationPropertyName}' — no Add method found.");
        }

    }

}