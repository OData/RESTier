// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents the default submit handler.
    /// </summary>
    internal static class DefaultSubmitHandler
    {
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
        public static async Task<SubmitResult> SubmitAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));

            var preparer = context.GetApiService<IChangeSetInitializer>();
            if (preparer == null)
            {
                throw new NotSupportedException(Resources.ChangeSetPreparerMissing);
            }

            await preparer.InitializeAsync(context, cancellationToken).ConfigureAwait(false);

            if (context.Result != null)
            {
                return context.Result;
            }

            var eventsChangeSet = context.ChangeSet;

            IEnumerable<ChangeSetItem> currentChangeSetItems = eventsChangeSet.Entries.ToArray();

            await PerformValidate(context, currentChangeSetItems, cancellationToken).ConfigureAwait(false);

            await PerformPreEvent(context, currentChangeSetItems, cancellationToken).ConfigureAwait(false);

            await PerformPersist(context, cancellationToken).ConfigureAwait(false);

            context.ChangeSet.Entries.Clear();

            await PerformPostEvent(context, currentChangeSetItems, cancellationToken).ConfigureAwait(false);

            return context.Result;
        }

        private static string GetAuthorizeFailedMessage(ChangeSetItem item)
        {
            switch (item.Type)
            {
                case ChangeSetItemType.DataModification:
                    var dataModification = (DataModificationItem)item;
                    string message = null;
                    if (dataModification.EntitySetOperation == RestierEntitySetOperation.Insert)
                    {
                        message = Resources.NoPermissionToInsertEntity;
                    }
                    else if (dataModification.EntitySetOperation == RestierEntitySetOperation.Update)
                    {
                        message = Resources.NoPermissionToUpdateEntity;
                    }
                    else if (dataModification.EntitySetOperation == RestierEntitySetOperation.Delete)
                    {
                        message = Resources.NoPermissionToDeleteEntity;
                    }
                    else
                    {
                        throw new NotSupportedException(Resources.DataModificationMustBeCUD);
                    }

                    return string.Format(CultureInfo.InvariantCulture, message, dataModification.ResourceSetName);

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, item.Type));
            }
        }

        private static async Task PerformValidate(SubmitContext context, IEnumerable<ChangeSetItem> changeSetItems, CancellationToken cancellationToken)
        {
            await InvokeAuthorizers(context, changeSetItems, cancellationToken).ConfigureAwait(false);

            await InvokeValidators(context, changeSetItems, cancellationToken).ConfigureAwait(false);

            foreach (var item in changeSetItems.Where(i => i.HasChanged()))
            {
                if (item.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.ChangedWithinOwnPreEventing)
                {
                    item.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.PreEvented;
                }
                else
                {
                    item.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.Validated;
                }
            }
        }

        private static async Task InvokeAuthorizers(SubmitContext context, IEnumerable<ChangeSetItem> changeSetItems, CancellationToken cancellationToken)
        {
            var authorizer = context.GetApiService<IChangeSetItemAuthorizer>();
            if (authorizer == null)
            {
                return;
            }

            foreach (var item in changeSetItems.Where(i => i.HasChanged()))
            {
                if (!await authorizer.AuthorizeAsync(context, item, cancellationToken).ConfigureAwait(false))
                {
                    var message = GetAuthorizeFailedMessage(item);
                    throw new SecurityException(message);
                }
            }
        }

        private static async Task InvokeValidators(SubmitContext context, IEnumerable<ChangeSetItem> changeSetItems, CancellationToken cancellationToken)
        {
            var validator = context.GetApiService<IChangeSetItemValidator>();
            if (validator == null)
            {
                return;
            }

            var validationResults = new Collection<ChangeSetItemValidationResult>();

            foreach (var entry in changeSetItems.Where(i => i.HasChanged()))
            {
                await validator.ValidateChangeSetItemAsync(context, entry, validationResults, cancellationToken).ConfigureAwait(false);
            }

            var errors = validationResults.Where(result => result.Severity == EventLevel.Error);

            if (errors.Any())
            {
                var validationErrorMessage = Resources.ValidationFailsTheOperation;
                throw new ChangeSetValidationException(validationErrorMessage)
                {
                    ValidationResults = errors
                };
            }
        }

        private static async Task PerformPreEvent(SubmitContext context, IEnumerable<ChangeSetItem> changeSetItems, CancellationToken cancellationToken)
        {
            foreach (var item in changeSetItems)
            {
                if (item.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.Validated)
                {
                    item.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.PreEventing;

                    var processor = context.GetApiService<IChangeSetItemFilter>();
                    if (processor != null)
                    {
                        await processor.OnChangeSetItemProcessingAsync(context, item, cancellationToken).ConfigureAwait(false);
                    }

                    if (item.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.PreEventing)
                    {
                        // if the state is still the intermediate state,
                        // the entity was not changed during processing
                        // and can move to the next step
                        item.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.PreEvented;
                    }
                    else if (item.ChangeSetItemProcessingStage == ChangeSetItemProcessingStage.Initialized /*&&
                        entity.Details.EntityState == originalEntityState*/)
                    {
                        item.ChangeSetItemProcessingStage = ChangeSetItemProcessingStage.ChangedWithinOwnPreEventing;
                    }
                }
            }
        }

        private static async Task PerformPersist(SubmitContext context, CancellationToken cancellationToken)
        {
            var executor = context.GetApiService<ISubmitExecutor>();
            if (executor == null)
            {
                throw new NotSupportedException(Resources.SubmitExecutorMissing);
            }

            context.Result = await executor.ExecuteSubmitAsync(context, cancellationToken).ConfigureAwait(false);
        }

        private static async Task PerformPostEvent(SubmitContext context, IEnumerable<ChangeSetItem> changeSetItems, CancellationToken cancellationToken)
        {
            //TODO: Check this for unnecessary allocations.
            foreach (var item in changeSetItems)
            {
                var processor = context.GetApiService<IChangeSetItemFilter>();
                if (processor != null)
                {
                    await processor.OnChangeSetItemProcessedAsync(context, item, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
