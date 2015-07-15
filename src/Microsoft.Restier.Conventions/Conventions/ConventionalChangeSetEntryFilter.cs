// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Conventions.Properties;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Conventions
{
    /// <summary>
    /// A conventional change set entry filter.
    /// </summary>
    public class ConventionalChangeSetEntryFilter : IChangeSetEntryFilter
    {
        private Type targetType;

        private ConventionalChangeSetEntryFilter(Type targetType)
        {
            Ensure.NotNull(targetType, "targetType");
            this.targetType = targetType;
        }

        /// <inheritdoc/>
        public static void ApplyTo(
            DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");
            configuration.AddHookPoint(
                typeof(IChangeSetEntryFilter),
                new ConventionalChangeSetEntryFilter(targetType));
        }

        /// <inheritdoc/>
        public Task OnExecutingEntryAsync(
            SubmitContext context,
            ChangeSetEntry entry,
            CancellationToken cancellationToken)
        {
            return this.InvokeFilterMethodAsync(context, entry, "ing");
        }

        /// <inheritdoc/>
        public Task OnExecutedEntryAsync(
            SubmitContext context,
            ChangeSetEntry entry,
            CancellationToken cancellationToken)
        {
            return this.InvokeFilterMethodAsync(context, entry, "ed");
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
                    operationName = "Insert";
                }
                else if (dataModification.IsUpdate)
                {
                    operationName = "Updat";
                }
                else if (dataModification.IsDelete)
                {
                    operationName = "Delet";
                }

                return "On" + operationName + suffix + dataModification.EntitySetName;

            case ChangeSetEntryType.ActionInvocation:
                ActionInvocationEntry actionEntry = (ActionInvocationEntry)entry;
                return "OnExecut" + suffix + actionEntry.ActionName;

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
            string methodName = ConventionalChangeSetEntryFilter.GetMethodName(entry, methodNameSuffix);
            object[] parameters = ConventionalChangeSetEntryFilter.GetParameters(entry);

            MethodInfo method = this.targetType.GetQualifiedMethod(methodName);

            if (method != null &&
                (method.ReturnType == typeof(void) ||
                typeof(Task).IsAssignableFrom(method.ReturnType)))
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.DomainContext.GetProperty(
                        this.targetType.AssemblyQualifiedName);
                    if (target == null ||
                        !this.targetType.IsAssignableFrom(target.GetType()))
                    {
                        return Task.WhenAll();
                    }
                }

                ParameterInfo[] methodParameters = method.GetParameters();
                if (ConventionalChangeSetEntryFilter.ParametersMatch(methodParameters, parameters))
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
