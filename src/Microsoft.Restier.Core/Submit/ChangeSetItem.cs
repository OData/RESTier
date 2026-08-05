// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;

namespace Microsoft.Restier.Core.Submit
{

    /// <summary>
    /// Specifies the type of a change set item.
    /// </summary>
    internal enum ChangeSetItemType
    {
        /// <summary>
        /// Specifies a data modification item.
        /// </summary>
        DataModification
    }

    /// <summary>
    /// Possible states of an resource during a ChangeSet life cycle
    /// </summary>
    internal enum ChangeSetItemProcessingStage
    {
        /// <summary>
        /// If an new change set item is created
        /// </summary>
        Initialized,

        /// <summary>
        /// The resource has been validated.
        /// </summary>
        Validated,

        /// <summary>
        /// The resource set deleting, inserting or updating events are raised
        /// </summary>
        PreEventing,

        /// <summary>
        /// The resource was modified within its own pre eventing interception method. This indicates that the resource
        /// should be revalidated but its pre eventing interception point should not be invoked again.
        /// </summary>
        ChangedWithinOwnPreEventing,

        /// <summary>
        /// The resource's pre events have been raised
        /// </summary>
        PreEvented
    }

    /// <summary>
    /// Represents an item in a change set.
    /// </summary>
    public abstract class ChangeSetItem
    {
        internal ChangeSetItem(ChangeSetItemType type)
        {
            Type = type;
            ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.Initialized;
        }

        /// <summary>
        /// Gets the type of this change set item.
        /// </summary>
        internal ChangeSetItemType Type { get; private set; }

        /// <summary>
        /// Gets or sets the dynamic state of this change set item.
        /// </summary>
        internal ChangeSetItemProcessingStage ChangeSetItemProcessingStage { get; set; }

        /// <summary>
        /// Indicates whether this change set item is in a changed state.
        /// </summary>
        /// <returns>
        /// Whether this change set item is in a changed state.
        /// </returns>
        public bool HasChanged()
        {
            return ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.Initialized ||
                ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.ChangedWithinOwnPreEventing;
        }
    }

    /// <summary>
    /// Represents a data modification item in a change set.
    /// </summary>
    public class DataModificationItem : ChangeSetItem
    {
        private const string IfNoneMatchKey = "@IfNoneMatchKey";

        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationItem" /> class.
        /// </summary>
        /// <param name="resourceSetName">
        /// The name of the resource set in question.
        /// </param>
        /// <param name="expectedResourceType">
        /// The type of the expected resource type in question.
        /// </param>
        /// <param name="actualResourceType">
        /// The type of the actual resource type in question.
        /// </param>
        /// <param name="action">
        /// The RestierEntitySetOperations for the request.
        /// </param>
        /// <param name="resourceKey">
        /// The key of the resource being modified.
        /// </param>
        /// <param name="originalValues">
        /// Any original values of the resource that are known.
        /// </param>
        /// <param name="localValues">
        /// The local values of the resource.
        /// </param>
        public DataModificationItem(
            string resourceSetName,
            Type expectedResourceType,
            Type actualResourceType,
            RestierEntitySetOperation action,
            IReadOnlyDictionary<string, object> resourceKey,
            IReadOnlyDictionary<string, object> originalValues,
            IReadOnlyDictionary<string, object> localValues)
            : base(ChangeSetItemType.DataModification)
        {
            Ensure.NotNull(resourceSetName, nameof(resourceSetName));
            Ensure.NotNull(expectedResourceType, nameof(expectedResourceType));
            ResourceSetName = resourceSetName;
            ExpectedResourceType = expectedResourceType;
            ActualResourceType = actualResourceType;
            ResourceKey = resourceKey;
            OriginalValues = originalValues;
            LocalValues = localValues;
            EntitySetOperation = action;
        }

        /// <summary>
        /// Gets the name of the resource set in question.
        /// </summary>
        public string ResourceSetName { get; private set; }

        /// <summary>
        /// Gets the name of the expected resource type in question.
        /// </summary>
        public Type ExpectedResourceType { get; private set; }

        /// <summary>
        /// Gets the name of the actual resource type in question.
        /// In type inheritance case, this is different from expectedResourceType
        /// </summary>
        public Type ActualResourceType { get; private set; }

        /// <summary>
        /// Gets the key of the resource being modified.
        /// </summary>
        public IReadOnlyDictionary<string, object> ResourceKey { get; private set; }

        /// <summary>
        /// Gets or sets the action to be taken.
        /// </summary>
        public RestierEntitySetOperation EntitySetOperation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the resource should be fully replaced by the modification.
        /// </summary>
        /// <remarks>
        /// If true, all properties will be updated, even if the property isn't in LocalValues.
        /// If false, only properties identified in LocalValues will be updated on the resource.
        /// </remarks>
        public bool IsFullReplaceUpdateRequest { get; set; }

        /// <summary>
        /// Gets or sets the resource object in question.
        /// </summary>
        /// <remarks>
        /// Initially this will be <c>null</c>, however after the change
        /// set has been prepared it will represent the pending resource.
        /// </remarks>
        public object Resource { get; set; }

        /// <summary>
        /// Gets the original values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For new entities, this property is <c>null</c>.
        /// </remarks>
        public IReadOnlyDictionary<string, object> OriginalValues { get; private set; }

        /// <summary>
        /// Gets the current server values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For new entities, this property is <c>null</c>. For updated
        /// entities, it is <c>null</c> until the change set is prepared.
        /// </remarks>
        public IReadOnlyDictionary<string, object> ServerValues { get; private set; }

        /// <summary>
        /// Gets the local values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For entities pending deletion, this property is <c>null</c>.
        /// </remarks>
        public IReadOnlyDictionary<string, object> LocalValues { get; internal set; }

        /// <summary>
        /// Gets or sets the parent DataModificationItem for nested operations.
        /// Null for root/direct operations.
        /// </summary>
        public DataModificationItem ParentItem { get; set; }

        /// <summary>
        /// Gets or sets the CLR navigation property name on the parent entity
        /// that this item was nested under.
        /// </summary>
        public string ParentNavigationPropertyName { get; set; }

        /// <summary>
        /// Gets the child DataModificationItems for deep insert/update.
        /// Each child flows through the full submit pipeline.
        /// </summary>
        public IList<DataModificationItem> NestedItems { get; } = new List<DataModificationItem>();

        /// <summary>
        /// Navigation property names explicitly set to null in the payload.
        /// Used for relationship unlinking during deep update.
        /// </summary>
        public ISet<string> NullNavigationProperties { get; } = new HashSet<string>();

        /// <summary>
        /// LocalValues computed with isCreation=false. Used when the classifier
        /// reclassifies an Insert to Update. Null for root/non-reclassifiable items.
        /// </summary>
        internal IReadOnlyDictionary<string, object> UpdateLocalValues { get; set; }

        /// <summary>
        /// Gets the entity reference bindings: maps CLR navigation property name to bind reference(s).
        /// These are relationship-only operations — no CUD pipeline events fire for the target.
        /// </summary>
        public IDictionary<string, IList<BindReference>> NavigationBindings { get; } = new Dictionary<string, IList<BindReference>>();

        /// <summary>
        /// Gets the relationship removals generated by the deep update classifier.
        /// These are processed during EF initializer to unlink child entities.
        /// </summary>
        public IList<RelationshipRemoval> RelationshipRemovals { get; } = new List<RelationshipRemoval>();

        /// <summary>
        /// Flattens the DataModificationItem tree in depth-first pre-order,
        /// guaranteeing parent items appear before their children.
        /// </summary>
        /// <returns>An enumerable of all items in the tree.</returns>
        public IEnumerable<DataModificationItem> FlattenDepthFirst()
        {
            yield return this;
            foreach (var child in NestedItems)
            {
                foreach (var descendant in child.FlattenDepthFirst())
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Applies the current DataModificationItem's KeyValues and OriginalValues to the
        /// specified query and returns the new query.
        /// </summary>
        /// <param name="query">The IQueryable to apply the property values to.</param>
        /// <returns>
        /// The new IQueryable with the property values applied to it in a Where condition.
        /// </returns>
        public IQueryable ApplyTo(IQueryable query)
        {
            Ensure.NotNull(query, nameof(query));
            if (EntitySetOperation == RestierEntitySetOperation.Insert)
            {
                throw new InvalidOperationException(Resources.DataModificationNotSupportCreateResource);
            }

            var type = query.ElementType;
            var param = Expression.Parameter(type);
            Expression where = null;

            if (ResourceKey is not null)
            {
                foreach (var item in ResourceKey)
                {
                    where = ApplyPredicate(param, where, item);
                }
            }

            if (where is null)
            {
                throw new InvalidOperationException(Resources.DataModificationRequiresResourceKey);
            }

            var whereLambda = Expression.Lambda(where, param);
            return ExpressionHelpers.Where(query, whereLambda, type);
        }

        /// <summary>
        /// Validate the e-tag via applies the current DataModificationItem's OriginalValues to the
        /// specified query and returns result.
        /// </summary>
        /// <param name="query">The IQueryable to apply the property values to.</param>
        /// <returns>
        /// The object is e-tag checked passed.
        /// </returns>
        public object ValidateEtag(IQueryable query)
        {
            Ensure.NotNull(query, nameof(query));
            var type = query.ElementType;
            var param = Expression.Parameter(type);
            Expression where = null;

            if (OriginalValues is not null)
            {
                foreach (var item in OriginalValues)
                {
                    if (!item.Key.StartsWith("@", StringComparison.Ordinal))
                    {
                        where = ApplyPredicate(param, where, item);
                    }
                }

                if (OriginalValues.ContainsKey(IfNoneMatchKey))
                {
                    where = Expression.Not(where);
                }
            }

            var whereLambda = Expression.Lambda(where, param);
            var queryable = ExpressionHelpers.Where(query, whereLambda, type);

            var matchedResource = queryable.SingleOrDefault();
            if (matchedResource is null)
            {
                // If ETAG does not match, should return 412 Precondition Failed
                var message = string.Format(CultureInfo.InvariantCulture, Resources.PreconditionCheckFailed, new object[] { EntitySetOperation, query.SingleOrDefault() });
                throw new StatusCodeException(HttpStatusCode.PreconditionFailed, message);
            }

            return matchedResource;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="param"></param>
        /// <param name="where"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private static Expression ApplyPredicate(ParameterExpression param, Expression where, KeyValuePair<string, object> item)
        {
            var property = Expression.Property(param, item.Key);
            var itemValue = item.Value;

            if (itemValue.GetType() != property.Type)
            {
                itemValue = TypeConverter.ChangeType(itemValue, property.Type, CultureInfo.InvariantCulture);
            }

            // TODO GitHubIssue#31 : Check if LinqParameterContainer is necessary
            // Expression value = itemValue is not null
            //     ? LinqParameterContainer.Parameterize(itemValue.GetType(), itemValue)
            //     : Expression.Constant(value: null);
            Expression left = property;
            Expression right = Expression.Constant(itemValue, property.Type);
            if (property.Type == typeof(byte[]))
            {
                left = Expression.Call(typeof(BitConverter), "ToString", null, new Expression[] { property });
                right = Expression.Call(typeof(BitConverter), "ToString", null, new Expression[] { Expression.Constant(itemValue, property.Type) });
            }

            var equal = Expression.Equal(left, right);
            return where is null ? equal : Expression.AndAlso(where, equal);
        }
    }

    /// <summary>
    /// Represents a relationship to be removed during deep update.
    /// Stores entity set + key; resolved by EF initializer Phase 1.
    /// </summary>
    public class RelationshipRemoval
    {
        /// <summary>
        /// The navigation property name on the parent entity.
        /// </summary>
        public string NavigationPropertyName { get; set; }

        /// <summary>
        /// CLR name of the inverse navigation property on the child entity.
        /// Resolved from IEdmNavigationProperty.Partner during classification.
        /// </summary>
        public string InverseNavigationPropertyName { get; set; }

        /// <summary>
        /// CLR name of the FK property on the child entity (e.g., "PublisherId").
        /// Used to directly null the FK for reliable relationship severance.
        /// </summary>
        public string FkPropertyName { get; set; }

        /// <summary>
        /// The target entity set name.
        /// </summary>
        public string ResourceSetName { get; set; }

        /// <summary>
        /// The key of the child entity to unlink.
        /// </summary>
        public IReadOnlyDictionary<string, object> ResourceKey { get; set; }

        /// <summary>
        /// Resolved child entity instance (set during EF initializer Phase 1).
        /// </summary>
        public object ResolvedEntity { get; set; }
    }

    /// <summary>
    /// Represents a data modification item in a change set.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    public class DataModificationItem<T> : DataModificationItem
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationItem{T}" /> class.
        /// </summary>
        /// <param name="resourceSetName">
        /// The name of the resource set in question.
        /// </param>
        /// <param name="expectedResourceType">
        /// The type of the expected resource type in question.
        /// </param>
        /// <param name="actualResourceType">
        /// The type of the actual resource type in question.
        /// </param>
        /// <param name="action">
        /// The RestierEntitySetOperations for the request.
        /// </param>
        /// <param name="resourceKey">
        /// The key of the resource being modified.
        /// </param>
        /// <param name="originalValues">
        /// Any original values of the resource that are known.
        /// </param>
        /// <param name="localValues">
        /// The local values of the entity.
        /// </param>
        public DataModificationItem(
            string resourceSetName,
            Type expectedResourceType,
            Type actualResourceType,
            RestierEntitySetOperation action,
            IReadOnlyDictionary<string, object> resourceKey,
            IReadOnlyDictionary<string, object> originalValues,
            IReadOnlyDictionary<string, object> localValues)
            : base(
                  resourceSetName,
                  expectedResourceType,
                  actualResourceType,
                  action,
                  resourceKey,
                  originalValues,
                  localValues)
        {
        }

        /// <summary>
        /// Gets or sets the resource object in question.
        /// </summary>
        /// <remarks>
        /// Initially this will be <c>null</c>, however after the change
        /// set has been prepared it will represent the pending resource.
        /// </remarks>
        public new T Resource
        {
            get => base.Resource as T;

            set => base.Resource = value;
        }
    }
}
