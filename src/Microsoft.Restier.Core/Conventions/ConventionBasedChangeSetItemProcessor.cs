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
    /// A convention-based change set item processor which calls logic like OnInserting and OnInserted.
    /// </summary>
    internal class ConventionBasedChangeSetItemProcessor : IChangeSetItemProcessor
    {
        private Type targetType;

        private ConventionBasedChangeSetItemProcessor(Type targetType)
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
            services.AddService<IChangeSetItemProcessor>(
                (sp, next) => new ConventionBasedChangeSetItemProcessor(targetType));
        }

        /// <inheritdoc/>
        public Task OnProcessingChangeSetItemAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken)
        {
            return this.InvokeProcessorMethodAsync(
                context, item, ConventionBasedChangeSetConstants.FilterMethodNamePreFilterSuffix);
        }

        /// <inheritdoc/>
        public Task OnProcessedChangeSetItemAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken)
        {
            return this.InvokeProcessorMethodAsync(
                context, item, ConventionBasedChangeSetConstants.FilterMethodNamePostFilterSuffix);
        }

        private static string GetMethodName(ChangeSetItem item, string suffix)
        {
            switch (item.Type)
            {
                case ChangeSetItemType.DataModification:
                    DataModificationItem dataModification = (DataModificationItem)item;
                    string operationName = null;
                    if (dataModification.DataModificationItemAction == DataModificationItemAction.Insert)
                    {
                        operationName = ConventionBasedChangeSetConstants.FilterMethodDataModificationInsert;
                    }
                    else if (dataModification.DataModificationItemAction == DataModificationItemAction.Update)
                    {
                        operationName = ConventionBasedChangeSetConstants.FilterMethodDataModificationUpdate;
                    }
                    else if (dataModification.DataModificationItemAction == DataModificationItemAction.Remove)
                    {
                        operationName = ConventionBasedChangeSetConstants.FilterMethodDataModificationDelete;
                    }

                    return operationName + suffix + dataModification.EntitySetName;

                default:
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, item.Type));
            }
        }

        private static object[] GetParameters(ChangeSetItem item)
        {
            switch (item.Type)
            {
                case ChangeSetItemType.DataModification:
                    DataModificationItem dataModification = (DataModificationItem)item;
                    return new object[] { dataModification.Entity };

                default:
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, item.Type));
            }
        }

        private static bool ParametersMatch(ParameterInfo[] methodParameters, object[] parameters)
        {
            return methodParameters.Length == parameters.Length
                && !methodParameters.Where((mp, i) => !mp.ParameterType.IsInstanceOfType(parameters[i])).Any();
        }

        private Task InvokeProcessorMethodAsync(
            SubmitContext context,
            ChangeSetItem item,
            string methodNameSuffix)
        {
            string methodName = GetMethodName(item, methodNameSuffix);
            object[] parameters = GetParameters(item);

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
                if (ParametersMatch(methodParameters, parameters))
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
