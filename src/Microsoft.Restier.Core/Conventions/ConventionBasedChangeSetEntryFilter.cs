// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Properties;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core.Conventions
{
    /// <summary>
    /// A convention-based change set entry filter.
    /// </summary>
    internal class ConventionBasedChangeSetEntryFilter : IChangeSetEntryFilter
    {
        private Type targetType;

        private ConventionBasedChangeSetEntryFilter(Type targetType)
        {
            Ensure.NotNull(targetType, "targetType");
            this.targetType = targetType;
        }

        /// <inheritdoc/>
        public static void ApplyTo(
            IServiceCollection services,
            Type targetType)
        {
            Ensure.NotNull(services, "services");
            Ensure.NotNull(targetType, "targetType");
            services.CutoffPrevious<IChangeSetEntryFilter>((sp) => new ConventionBasedChangeSetEntryFilter(targetType));
        }

        /// <inheritdoc/>
        public Task OnExecutingEntryAsync(
            SubmitContext context,
            ChangeSetEntry entry,
            CancellationToken cancellationToken)
        {
            return this.InvokeFilterMethodAsync(
                context, entry, ConventionBasedChangeSetConstants.FilterMethodNamePreFilterSuffix);
        }

        /// <inheritdoc/>
        public Task OnExecutedEntryAsync(
            SubmitContext context,
            ChangeSetEntry entry,
            CancellationToken cancellationToken)
        {
            return this.InvokeFilterMethodAsync(
                context, entry, ConventionBasedChangeSetConstants.FilterMethodNamePostFilterSuffix);
        }

        private static string GetMethodName(ChangeSetEntry entry, string suffix)
        {
            switch (entry.Type)
            {
                case ChangeSetEntryType.DataModification:
                    DataModificationEntry dataModification = (DataModificationEntry)entry;
                    string operationName = null;
                    if (dataModification.IsNew)
                    {
                        operationName = ConventionBasedChangeSetConstants.FilterMethodDataModificationInsert;
                    }
                    else if (dataModification.IsUpdate)
                    {
                        operationName = ConventionBasedChangeSetConstants.FilterMethodDataModificationUpdate;
                    }
                    else if (dataModification.IsDelete)
                    {
                        operationName = ConventionBasedChangeSetConstants.FilterMethodDataModificationDelete;
                    }

                    return operationName + suffix + dataModification.EntitySetName;

                case ChangeSetEntryType.ActionInvocation:
                    ActionInvocationEntry actionEntry = (ActionInvocationEntry)entry;
                    return ConventionBasedChangeSetConstants.FilterMethodActionInvocationExecute +
                           suffix + actionEntry.ActionName;

                default:
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, entry.Type));
            }
        }

        private static object[] GetParameters(ChangeSetEntry entry)
        {
            switch (entry.Type)
            {
                case ChangeSetEntryType.DataModification:
                    DataModificationEntry dataModification = (DataModificationEntry)entry;
                    return new object[] { dataModification.Entity };

                case ChangeSetEntryType.ActionInvocation:
                    ActionInvocationEntry actionEntry = (ActionInvocationEntry)entry;
                    return actionEntry.GetArgumentArray();

                default:
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, entry.Type));
            }
        }

        private static bool ParametersMatch(ParameterInfo[] methodParameters, object[] parameters)
        {
            return methodParameters.Length == parameters.Length
                && !methodParameters.Where((mp, i) => !mp.ParameterType.IsInstanceOfType(parameters[i])).Any();
        }

        private Task InvokeFilterMethodAsync(
            SubmitContext context,
            ChangeSetEntry entry,
            string methodNameSuffix)
        {
            string methodName = ConventionBasedChangeSetEntryFilter.GetMethodName(entry, methodNameSuffix);
            object[] parameters = ConventionBasedChangeSetEntryFilter.GetParameters(entry);

            MethodInfo method = this.targetType.GetQualifiedMethod(methodName);

            if (method != null &&
                (method.ReturnType == typeof(void) ||
                typeof(Task).IsAssignableFrom(method.ReturnType)))
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.GetApiService<ApiBase>();
                    if (target == null ||
                        !this.targetType.IsInstanceOfType(target))
                    {
                        return Task.WhenAll();
                    }
                }

                ParameterInfo[] methodParameters = method.GetParameters();
                if (ConventionBasedChangeSetEntryFilter.ParametersMatch(methodParameters, parameters))
                {
                    object result = method.Invoke(target, parameters);
                    Task resultTask = result as Task;
                    if (resultTask != null)
                    {
                        return resultTask;
                    }
                }
            }

            return Task.WhenAll();
        }
    }
}
