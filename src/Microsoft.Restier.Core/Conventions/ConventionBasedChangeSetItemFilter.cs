﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based change set item processor which calls logic like OnInserting and OnInserted.
    /// </summary>
    public class ConventionBasedChangeSetItemFilter : IChangeSetItemFilter
    {
        private readonly Type targetApiType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedChangeSetItemFilter"/> class.
        /// </summary>
        /// <param name="targetApiType">The target type to check for filter functions.</param>
        public ConventionBasedChangeSetItemFilter(Type targetApiType)
        {
            Ensure.NotNull(targetApiType, nameof(targetApiType));
            this.targetApiType = targetApiType;
        }

        /// <inheritdoc/>
        public Task OnChangeSetItemProcessingAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
            Ensure.NotNull(item, nameof(item));
            Ensure.NotNull(context, nameof(context));
            return InvokeProcessorMethodAsync(context, item, RestierPipelineState.PreSubmit);
        }

        /// <inheritdoc/>
        public Task OnChangeSetItemProcessedAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
            Ensure.NotNull(item, nameof(item));
            Ensure.NotNull(context, nameof(context));
            return InvokeProcessorMethodAsync(context, item, RestierPipelineState.PostSubmit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static object[] GetParameters(ChangeSetItem item)
        {
            switch (item.Type)
            {
                case ChangeSetItemType.DataModification:
                    var dataModification = (DataModificationItem)item;
                    return new object[] { dataModification.Resource };

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, item.Type));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodParameters"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static bool ParametersMatch(ParameterInfo[] methodParameters, object[] parameters)
        {
            return methodParameters.Length == parameters.Length && !methodParameters.Where((mp, i) => !mp.ParameterType.IsInstanceOfType(parameters[i])).Any();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="item"></param>
        /// <param name="pipelineState"></param>
        /// <returns></returns>
        private Task InvokeProcessorMethodAsync(SubmitContext context, ChangeSetItem item, RestierPipelineState pipelineState)
        {
            var dataModification = (DataModificationItem)item;
            var expectedMethodName = ConventionBasedMethodNameFactory.GetEntitySetMethodName(dataModification, pipelineState);
            var expectedMethod = targetApiType.GetQualifiedMethod(expectedMethodName);
            if (!IsUsable(expectedMethod))
            {
                if (expectedMethod != null)
                {
                    Trace.WriteLine($"Restier Filter found '{expectedMethodName}' but it is unaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
                }
                else
                {
                    var actualMethodName = expectedMethodName.Replace(dataModification.ExpectedResourceType.Name, dataModification.ResourceSetName);
                    var actualMethod = targetApiType.GetQualifiedMethod(actualMethodName);
                    if (actualMethod != null)
                    {
                        Trace.WriteLine($"BREAKING: Restier Filter expected'{expectedMethodName}' but found '{actualMethodName}'. Your method will not be called until you correct the method name.");
                    }
                }
            }
            else
            {
                object target = null;
                if (!expectedMethod.IsStatic)
                {
                    target = context.Api;
                    if (target == null || !targetApiType.IsInstanceOfType(target))
                    {
                        return Task.WhenAll();
                    }
                }

                var parameters = GetParameters(item);
                var methodParameters = expectedMethod.GetParameters();
                if (ParametersMatch(methodParameters, parameters))
                {
                    var result = expectedMethod.Invoke(target, parameters);
                    if (result is Task resultTask)
                    {
                        return resultTask;
                    }
                }
            }

            return Task.WhenAll();
        }

        private static bool IsUsable(MethodInfo info) => (info != null && (info.ReturnType == typeof(void) || typeof(Task).IsAssignableFrom(info.ReturnType)));


    }
}
