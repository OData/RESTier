// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents the default submit handler.
    /// </summary>
    internal static class DefaultSubmitHandler
    {
        /// <summary>
        /// The maximum numbers of loops for the pre-persisting events save loop
        /// and the post-persisting events save loops allowed before the DataService
        /// stops processing to prevent potential infinite loop.
        /// </summary>
        private const int MaxLoop = 200;

        /// <summary>
        /// Asynchronously executes the submit flow.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a submit result.
        /// </returns>
        public static async Task<SubmitResult> SubmitAsync(
            SubmitContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");

            var preparer = context.GetHookHandler<IChangeSetPreparer>();
            if (preparer == null)
            {
                throw new NotSupportedException();
            }

            await preparer.PrepareAsync(context, cancellationToken);

            // authorize
            var authorized = true;
            foreach (var authorizer in context
                .GetHookPoints<ISubmitAuthorizer>().Reverse())
            {
                authorized = await authorizer.AuthorizeAsync(
                    context, cancellationToken);
                if (!authorized || context.Result != null)
                {
                    break;
                }
            }

            if (!authorized)
            {
                // TODO GitHubIssue#32 : Figure out a more appropriate exception
                throw new SecurityException();
            }

            if (context.Result != null)
            {
                return context.Result;
            }

            ChangeSet eventsChangeSet = context.ChangeSet;

            IEnumerable<ChangeSetEntry> currentChangeSetItems;
            int outerLoopCount = 0;

            do
            {
                outerLoopCount++;
                int innerLoopCount = 0;
                do
                {
                    innerLoopCount++;
                    eventsChangeSet.AnEntityHasChanged = false;
                    currentChangeSetItems = eventsChangeSet.Entries.ToArray();

                    if (eventsChangeSet.AnEntityHasChanged)
                    {
                        eventsChangeSet.AnEntityHasChanged = false;
                        currentChangeSetItems = eventsChangeSet.Entries.ToArray();
                    }

                    await PerformValidate(context, currentChangeSetItems, cancellationToken);

                    await PerformPreEvent(context, currentChangeSetItems, cancellationToken);
                }
                while (eventsChangeSet.AnEntityHasChanged && (innerLoopCount < MaxLoop));

                VerifyNoEntityHasChanged(eventsChangeSet);

                await PerformPersist(context, currentChangeSetItems, cancellationToken);

                eventsChangeSet.Entries.Clear();

                await PerformPostEvent(context, currentChangeSetItems, cancellationToken);
            }
            while (eventsChangeSet.AnEntityHasChanged && (outerLoopCount < MaxLoop));

            VerifyNoEntityHasChanged(eventsChangeSet);

            return context.Result;
        }

        private static string GetAuthorizeFailedMessage(ChangeSetEntry entry)
        {
            switch (entry.Type)
            {
                case ChangeSetEntryType.DataModification:
                    DataModificationEntry dataModification = (DataModificationEntry)entry;
                    string message = null;
                    if (dataModification.IsNew)
                    {
                        message = Resources.NoPermissionToInsertEntity;
                    }
                    else if (dataModification.IsUpdate)
                    {
                        message = Resources.NoPermissionToUpdateEntity;
                    }
                    else if (dataModification.IsDelete)
                    {
                        message = Resources.NoPermissionToDeleteEntity;
                    }
                    else
                    {
                        throw new NotSupportedException(Resources.DataModificationMustBeCUD);
                    }

                    return string.Format(CultureInfo.InvariantCulture, message, dataModification.EntitySetName);

                case ChangeSetEntryType.ActionInvocation:
                    ActionInvocationEntry actionInvocation = (ActionInvocationEntry)entry;
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.NoPermissionToInvokeAction,
                        actionInvocation.ActionName);

                default:
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.InvalidChangeSetEntryType,
                        entry.Type));
            }
        }

        private static void VerifyNoEntityHasChanged(ChangeSet changeSet)
        {
            if (changeSet.AnEntityHasChanged)
            {
                throw new InvalidOperationException(Resources.ErrorInVerifyingNoEntityHasChanged);
            }
        }

        private static async Task PerformValidate(
            SubmitContext context,
            IEnumerable<ChangeSetEntry> changeSetItems,
            CancellationToken cancellationToken)
        {
            await InvokeAuthorizers(context, changeSetItems, cancellationToken);

            await InvokeValidators(context, changeSetItems, cancellationToken);

            foreach (ChangeSetEntry item in changeSetItems.Where(i => i.HasChanged()))
            {
                if (item.ChangeSetEntityState == DynamicChangeSetEntityState.ChangedWithinOwnPreEventing)
                {
                    item.ChangeSetEntityState = DynamicChangeSetEntityState.PreEvented;
                }
                else
                {
                    item.ChangeSetEntityState = DynamicChangeSetEntityState.Validated;
                }
            }
        }

        private static async Task InvokeAuthorizers(
            SubmitContext context,
            IEnumerable<ChangeSetEntry> changeSetItems,
            CancellationToken cancellationToken)
        {
            var authorizer = context.GetHookHandler<IChangeSetEntryAuthorizer>();
            if (authorizer == null)
            {
                return;
            }

            foreach (ChangeSetEntry entry in changeSetItems.Where(i => i.HasChanged()))
            {
                if (!await authorizer.AuthorizeAsync(context, entry, cancellationToken))
                {
                    var message = DefaultSubmitHandler.GetAuthorizeFailedMessage(entry);
                    throw new SecurityException(message);
                }
            }
        }

        private static async Task InvokeValidators(
            SubmitContext context,
            IEnumerable<ChangeSetEntry> changeSetItems,
            CancellationToken cancellationToken)
        {
            ValidationResults validationResults = new ValidationResults();

            foreach (ChangeSetEntry entry in changeSetItems.Where(i => i.HasChanged()))
            {
                foreach (var validator in context
                    .GetHookPoints<IChangeSetEntryValidator>().Reverse())
                {
                    await validator.ValidateEntityAsync(context, entry, validationResults, cancellationToken);
                }
            }

            if (validationResults.HasErrors)
            {
                string validationErrorMessage = Resources.ValidationFailsTheOperation;
                throw new ValidationException(validationErrorMessage)
                {
                    ValidationResults = validationResults.Errors
                };
            }
        }

        private static async Task PerformPreEvent(
            SubmitContext context,
            IEnumerable<ChangeSetEntry> changeSetItems,
            CancellationToken cancellationToken)
        {
            foreach (ChangeSetEntry entry in changeSetItems)
            {
                if (entry.ChangeSetEntityState == DynamicChangeSetEntityState.Validated)
                {
                    entry.ChangeSetEntityState = DynamicChangeSetEntityState.PreEventing;

                    var filter = context.GetHookHandler<IChangeSetEntryFilter>();
                    if (filter != null)
                    {
                        await filter.OnExecutingEntryAsync(context, entry, cancellationToken);
                    }

                    if (entry.ChangeSetEntityState == DynamicChangeSetEntityState.PreEventing)
                    {
                        // if the state is still the intermediate state,
                        // the entity was not changed during processing
                        // and can move to the next step
                        entry.ChangeSetEntityState = DynamicChangeSetEntityState.PreEvented;
                    }
                    else if (entry.ChangeSetEntityState == DynamicChangeSetEntityState.Changed /*&&
                        entity.Details.EntityState == originalEntityState*/)
                    {
                        entry.ChangeSetEntityState = DynamicChangeSetEntityState.ChangedWithinOwnPreEventing;
                    }
                }
            }
        }

        private static async Task PerformPersist(
            SubmitContext context,
            IEnumerable<ChangeSetEntry> changeSetItems,
            CancellationToken cancellationToken)
        {
            // Once the change is persisted, the EntityState is lost.
            // In order to invoke the correct post-CUD event, remember which action was performed on the entity.
            foreach (ChangeSetEntry item in changeSetItems)
            {
                if (item.Type == ChangeSetEntryType.DataModification)
                {
                    DataModificationEntry dataModification = (DataModificationEntry)item;
                    if (dataModification.IsNew)
                    {
                        dataModification.AddAction = AddAction.Inserting;
                    }
                    else if (dataModification.IsUpdate)
                    {
                        dataModification.AddAction = AddAction.Updating;
                    }
                    else if (dataModification.IsDelete)
                    {
                        dataModification.AddAction = AddAction.Removing;
                    }
                }
            }

            var executor = context.GetHookHandler<ISubmitExecutor>();
            if (executor == null)
            {
                throw new NotSupportedException();
            }

            context.Result = await executor.ExecuteSubmitAsync(context, cancellationToken);
        }

        private static async Task PerformPostEvent(
            SubmitContext context,
            IEnumerable<ChangeSetEntry> changeSetItems,
            CancellationToken cancellationToken)
        {
            foreach (ChangeSetEntry entry in changeSetItems)
            {
                var filter = context.GetHookHandler<IChangeSetEntryFilter>();
                if (filter != null)
                {
                    await filter.OnExecutedEntryAsync(context, entry, cancellationToken);
                }
            }
        }
    }
}
