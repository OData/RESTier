// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Submit;
using System.Diagnostics;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based change set item processor which calls logic like OnInserting and OnInserted.
    /// </summary>
    internal class ConventionBasedChangeSetItemFilter : IChangeSetItemFilter
    {
        private Type targetType;

        internal ConventionBasedChangeSetItemFilter(Type targetType)
        {
            Ensure.NotNull(targetType, "targetType");
            this.targetType = targetType;
        }

        /// <inheritdoc/>
        public static void ApplyTo(IServiceCollection services, Type targetType)
        {
            Ensure.NotNull(services, "services");
            Ensure.NotNull(targetType, "targetType");
            services.AddService<IChangeSetItemFilter>(
                (sp, next) => new ConventionBasedChangeSetItemFilter(targetType));
        }

        /// <inheritdoc/>
        public Task OnChangeSetItemProcessingAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken)
        {
            return this.InvokeProcessorMethodAsync(
                context, item, ConventionBasedChangeSetConstants.FilterMethodNamePreFilterSuffix);
        }

        /// <inheritdoc/>
        public Task OnChangeSetItemProcessedAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken)
        {
            return this.InvokeProcessorMethodAsync(
                context, item, ConventionBasedChangeSetConstants.FilterMethodNamePostFilterSuffix);
        }

        internal static string GetMethodName(ChangeSetItem item, string suffix, bool useOldMethod = false)
        {
            switch (item.Type)
            {
                case ChangeSetItemType.DataModification:
                    DataModificationItem dataModification = (DataModificationItem)item;
                    var operationName = GetOperationName(dataModification);
                    var entityName = useOldMethod ? dataModification.ResourceSetName : dataModification.ExpectedResourceType.Name;
                    return operationName + suffix + entityName;
                default:
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, item.Type));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        internal static string GetOperationName(DataModificationItem item)
        {
            string operationName = string.Empty;
            switch (item.DataModificationItemAction)
            {
                case DataModificationItemAction.Insert:
                    operationName = ConventionBasedChangeSetConstants.FilterMethodDataModificationInsert;
                    break;
                case DataModificationItemAction.Update:
                    operationName = ConventionBasedChangeSetConstants.FilterMethodDataModificationUpdate;
                    break;
                case DataModificationItemAction.Remove:
                    operationName = ConventionBasedChangeSetConstants.FilterMethodDataModificationDelete;
                    break;
            }
            return operationName;
        }

        private static object[] GetParameters(ChangeSetItem item)
        {
            switch (item.Type)
            {
                case ChangeSetItemType.DataModification:
                    DataModificationItem dataModification = (DataModificationItem)item;
                    return new object[] { dataModification.Resource };

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

        private Task InvokeProcessorMethodAsync(SubmitContext context, ChangeSetItem item, string methodNameSuffix)
        {
            string expectedMethodName = GetMethodName(item, methodNameSuffix);
            MethodInfo expectedMethod = this.targetType.GetQualifiedMethod(expectedMethodName);
            if (!IsUsable(expectedMethod))
            { 
                if (expectedMethod != null)
                {
                    Debug.WriteLine($"Restier Filter found '{expectedMethodName}' but it is unaccessible due to its protection level. Change it to be 'protected internal'.");
                }
                else
                {
                    var actualMethodName = GetMethodName(item, methodNameSuffix, true);
                    var actualMethod = this.targetType.GetQualifiedMethod(actualMethodName);
                    if (actualMethod != null)
                    {
                        Debug.WriteLine($"BREAKING: Restier Filter expected'{expectedMethodName}' but found '{actualMethodName}'. Please correct the method name.");
                    }
                }
            }
            else
            {
                object target = null;
                if (!expectedMethod.IsStatic)
                {
                    target = context.GetApiService<ApiBase>();
                    if (target == null ||
                        !this.targetType.IsInstanceOfType(target))
                    {
                        return Task.WhenAll();
                    }
                }

                object[] parameters = GetParameters(item);
                ParameterInfo[] methodParameters = expectedMethod.GetParameters();
                if (ParametersMatch(methodParameters, parameters))
                {
                    object result = expectedMethod.Invoke(target, parameters);
                    if (result is Task resultTask)
                    {
                        return resultTask;
                    }
                }
            }

            return Task.WhenAll();
        }

        private static bool IsUsable(MethodInfo info)
        {
            return (info != null && (info.ReturnType == typeof(void) || typeof(Task).IsAssignableFrom(info.ReturnType)));
        }


    }
}
