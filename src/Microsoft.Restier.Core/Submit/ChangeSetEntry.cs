// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents an entry in a change set.
    /// </summary>
    public abstract class ChangeSetEntry
    {
        internal ChangeSetEntry(ChangeSetEntryType type)
        {
            this.Type = type;

            this.ChangeSetEntityState = DynamicChangeSetEntityState.Changed;
        }

        /// <summary>
        /// Gets the type of this change set entry.
        /// </summary>
        public ChangeSetEntryType Type { get; private set; }

        /// <summary>
        /// Gets or sets the dynamic state of this change set entry.
        /// </summary>
        public DynamicChangeSetEntityState ChangeSetEntityState { get; set; }

        /// <summary>
        /// Indicates if this change set entry is in a changed state.
        /// </summary>
        /// <returns>
        /// If this change set entry is in a changed state.
        /// </returns>
        public bool HasChanged()
        {
            return this.ChangeSetEntityState == DynamicChangeSetEntityState.Changed ||
                this.ChangeSetEntityState == DynamicChangeSetEntityState.ChangedWithinOwnPreEventing;
        }
    }

    /// <summary>
    /// Specifies the type of a change set entry.
    /// </summary>
    public enum ChangeSetEntryType
    {
        /// <summary>
        /// Specifies a data modification entry.
        /// </summary>
        DataModification,

        /// <summary>
        /// Specifies an action invocation entry.
        /// </summary>
        ActionInvocation
    }

    /// <summary>
    /// Possible states of an entity during a ChangeSet life cycle
    /// </summary>
    public enum DynamicChangeSetEntityState
    {
        /// <summary>
        /// If an entity has changed it gets this state
        /// </summary>
        Changed,

        /// <summary>
        /// The entity has been validated.
        /// </summary>
        Validated,

        /// <summary>
        /// The entity set deleting, inserting or updating events are raised
        /// </summary>
        PreEventing,

        /// <summary>
        /// The entity was modified within its own pre eventing interception method.  This indicates that the entity
        /// should be revalidated but its eventing interception point should not be invoked again.
        /// </summary>
        ChangedWithinOwnPreEventing,

        /// <summary>
        /// The entity's pre events have been raised
        /// </summary>
        PreEvented
    }

    /// <summary>
    /// This enum controls the actions requested for an entity.
    /// </summary>
    /// <remarks>
    /// This is required because during the post-CUD events, the EntityState has been lost.  This enum allows the DomainService to remember
    /// which pre-CUD event was raised for the Entity.
    /// </remarks>
    public enum AddAction
    {
        /// <summary>
        /// Specifies an undefined action.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Specifies the entity is being updated.
        /// </summary>
        Updating,

        /// <summary>
        /// Specifies the entity is being inserted.
        /// </summary>
        Inserting,

        /// <summary>
        /// Specifies the entity is being removed.
        /// </summary>
        Removing
    }

    /// <summary>
    /// Represents a data modification entry in a change set.
    /// </summary>
    public class DataModificationEntry : ChangeSetEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationEntry" /> class.
        /// </summary>
        /// <param name="entitySetName">
        /// The name of the entity set in question.
        /// </param>
        /// <param name="entityTypeName">
        /// The name of the entity type in question.
        /// </param>
        /// <param name="entityKey">
        /// The key of the entity being modified.
        /// </param>
        /// <param name="originalValues">
        /// Any original values of the entity that are known.
        /// </param>
        /// <param name="localValues">
        /// The local values of the entity.
        /// </param>
        public DataModificationEntry(
            string entitySetName, string entityTypeName,
            IReadOnlyDictionary<string, object> entityKey,
            IReadOnlyDictionary<string, object> originalValues,
            IReadOnlyDictionary<string, object> localValues)
            : base(ChangeSetEntryType.DataModification)
        {
            Ensure.NotNull(entitySetName, "entitySetName");
            Ensure.NotNull(entityTypeName, "entityTypeName");
            this.EntitySetName = entitySetName;
            this.EntityTypeName = entityTypeName;
            this.EntityKey = entityKey;
            this.OriginalValues = originalValues;
            this.LocalValues = localValues;
            this.AddAction = AddAction.Undefined;
        }

        /// <summary>
        /// Gets the name of the entity set in question.
        /// </summary>
        public string EntitySetName { get; private set; }

        /// <summary>
        /// Gets the name of the entity type in question.
        /// </summary>
        public string EntityTypeName { get; private set; }

        /// <summary>
        /// Gets the key of the entity being modified.
        /// </summary>
        public IReadOnlyDictionary<string, object> EntityKey { get; private set; }

        /// <summary>
        /// Gets or sets the action to be taken.
        /// </summary>
        public AddAction AddAction { get; set; }

        /// <summary>
        /// Gets a value indicating whether the modification is a new entity.
        /// </summary>
        public bool IsNew
        {
            get
            {
                return this.OriginalValues == null && this.EntityKey == null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the modification is updating an entity.
        /// </summary>
        public bool IsUpdate
        {
            get
            {
                return this.OriginalValues != null && this.LocalValues != null;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entity should be fully replaced by the modification.
        /// </summary>
        /// <remarks>
        /// If true, all properties will be updated, even if the property isn't in LocalValues.
        /// If false, only properties identified in LocalValues will be updated on the entity.
        /// </remarks>
        public bool IsFullReplaceUpdate { get; set; }

        /// <summary>
        /// Gets a value indicating whether the modification is deleting an entity.
        /// </summary>
        public bool IsDelete
        {
            get
            {
                return this.LocalValues == null;
            }
        }

        /// <summary>
        /// Gets or sets the entity object in question.
        /// </summary>
        /// <remarks>
        /// Initially this will be <c>null</c>, however after the change
        /// set has been prepared it will represent the pending entity.
        /// </remarks>
        public object Entity { get; set; }

        /// <summary>
        /// Gets the original values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For new entities, this property is <c>null</c>.
        /// </remarks>
        public IReadOnlyDictionary<string, object> OriginalValues
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the current server values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For new entities, this property is <c>null</c>. For updated
        /// entities, it is <c>null</c> until the change set is prepared.
        /// </remarks>
        public IReadOnlyDictionary<string, object> ServerValues
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the local values for properties that have changed.
        /// </summary>
        /// <remarks>
        /// For entities pending deletion, this property is <c>null</c>.
        /// </remarks>
        public IReadOnlyDictionary<string, object> LocalValues
        {
            get;
            private set;
        }

        /// <summary>
        /// Applies the current DataModificationEntry's KeyValues and OriginalValues to the
        /// specified query and returns the new query.
        /// </summary>
        /// <param name="query">The IQueryable to apply the property values to.</param>
        /// <returns>
        /// The new IQueryable with the property values applied to it in a Where condition.
        /// </returns>
        public IQueryable ApplyTo(IQueryable query)
        {
            Ensure.NotNull(query);
            if (this.IsNew)
            {
                throw new InvalidOperationException(Resources.DataModificationNotSupportCreateEntity);
            }

            Type type = query.ElementType;
            ParameterExpression param = Expression.Parameter(type);
            Expression where = null;

            if (this.EntityKey != null)
            {
                foreach (KeyValuePair<string, object> item in this.EntityKey)
                {
                    where = ApplyPredicate(param, where, item);
                }
            }

            if (where == null)
            {
                throw new InvalidOperationException(Resources.DataModificationRequiresEntityKey);
            }

            if (this.OriginalValues != null)
            {
                foreach (KeyValuePair<string, object> item in this.OriginalValues)
                {
                    if (!item.Key.StartsWith("@", StringComparison.Ordinal))
                    {
                        where = ApplyPredicate(param, where, item);
                    }
                }
            }

            LambdaExpression whereLambda = Expression.Lambda(where, param);
            return ExpressionHelpers.Where(query, whereLambda, type);
        }

        private static Expression ApplyPredicate(ParameterExpression param, Expression where, KeyValuePair<string, object> item)
        {
            MemberExpression name = Expression.Property(param, item.Key);
            object itemValue = item.Value;
            // TODO GitHubIssue#31 : Check if LinqParameterContainer is necessary in DataModificationEntry::ApplyPredicate
            //Expression value = itemValue != null
            //    ? LinqParameterContainer.Parameterize(itemValue.GetType(), itemValue)
            //    : Expression.Constant(value: null);
            BinaryExpression equal = Expression.Equal(name, Expression.Constant(item.Value));
            return where == null ? equal : Expression.AndAlso(where, equal);
        }
    }

    /// <summary>
    /// Represents a data modification entry in a change set.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class DataModificationEntry<T> : DataModificationEntry
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataModificationEntry{T}" /> class.
        /// </summary>
        /// <param name="entitySetName">
        /// The name of the entity set in question.
        /// </param>
        /// <param name="entityTypeName">
        /// The name of the entity type in question.
        /// </param>
        /// <param name="entityKey">
        /// The key of the entity being modified.
        /// </param>
        /// <param name="originalValues">
        /// Any original values of the entity that are known.
        /// </param>
        /// <param name="localValues">
        /// The local values of the entity.
        /// </param>
        public DataModificationEntry(
            string entitySetName, string entityTypeName,
            IReadOnlyDictionary<string, object> entityKey,
            IReadOnlyDictionary<string, object> originalValues,
            IReadOnlyDictionary<string, object> localValues)
            : base(entitySetName, entityTypeName, entityKey, originalValues, localValues)
        {
        }

        /// <summary>
        /// Gets or sets the entity object in question.
        /// </summary>
        /// <remarks>
        /// Initially this will be <c>null</c>, however after the change
        /// set has been prepared it will represent the pending entity.
        /// </remarks>
        public new T Entity
        {
            get
            {
                return base.Entity as T;
            }

            set
            {
                base.Entity = value;
            }
        }
    }

    /// <summary>
    /// Represents an action invocation entry in a change set.
    /// </summary>
    public class ActionInvocationEntry : ChangeSetEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionInvocationEntry" /> class.
        /// </summary>
        /// <param name="actionName">
        /// An action name.
        /// </param>
        /// <param name="arguments">
        /// A set of arguments to pass to the action.
        /// </param>
        public ActionInvocationEntry(
            string actionName,
            IDictionary<string, object> arguments)
            : base(ChangeSetEntryType.ActionInvocation)
        {
            Ensure.NotNull(actionName, "actionName");
            this.ActionName = actionName;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Gets or sets the operation (action) request.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Gets the set of arguments to pass to the action.
        /// </summary>
        public IDictionary<string, object> Arguments { get; private set; }

        /// <summary>
        /// Gets or sets the result of the action.
        /// </summary>
        /// <remarks>
        /// Initially this will be <c>null</c>, however after the action
        /// has been invoked it will contain the result.
        /// </remarks>
        public object Result { get; set; }

        /// <summary>
        /// Gets an array of the arguments to pass to the action.
        /// </summary>
        /// <returns>
        /// An array of the arguments to pass to the action.
        /// </returns>
        public object[] GetArgumentArray()
        {
            if (this.Arguments == null)
            {
                return new object[] { };
            }
            else
            {
                return this.Arguments.Select(a => a.Value).ToArray();
            }
        }
    }
}
