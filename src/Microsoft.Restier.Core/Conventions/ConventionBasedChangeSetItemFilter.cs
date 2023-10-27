// Copyright (c) Microsoft Corporation.  All rights reserved.
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

            //RWM: This prefers the Sync name over the Async name, because in V1 Sync has been the only option for a decade. In v2, we'll probably just make everything Async without Sync calls.
            var expectedMethod = targetApiType.GetQualifiedMethod(expectedMethodName) ?? targetApiType.GetQualifiedMethod($"{expectedMethodName}Async");

            if (expectedMethod is null)
            {
                var actualMethodName = expectedMethodName.Replace(dataModification.ExpectedResourceType.Name, dataModification.ResourceSetName);
                var actualMethod = targetApiType.GetQualifiedMethod(actualMethodName);
                if (actualMethod is not null)
                {
                    Trace.WriteLine($"Restier ConventionBasedChangeSetItemFilter expected'{expectedMethodName}' but found '{actualMethodName}'. Your method will not be called until you correct the method name.");
                }

                return Task.CompletedTask;
            }

            if (!expectedMethod.IsFamily && !expectedMethod.IsFamilyOrAssembly)
            {
                Trace.WriteLine($"Restier ConventionBasedChangeSetItemFilter found '{expectedMethod}' but it is inaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
                return Task.CompletedTask;
            }

            if (expectedMethod.ReturnType != typeof(void) && !typeof(Task).IsAssignableFrom(expectedMethod.ReturnType))
            {
                Trace.WriteLine($"Restier ConventionBasedChangeSetItemFilter found '{expectedMethod}' but it does not return void or a Task. Your method will not be called until you correct the return type.");
                return Task.CompletedTask;
            }

            object target = null;
            if (!expectedMethod.IsStatic)
            {
                target = context.Api;
                if (target is null || !targetApiType.IsInstanceOfType(target))
                {
                    Trace.WriteLine("The Restier API is of the incorrect type.");
                    return Task.CompletedTask;
                }
            }

            var parameters = GetParameters(item);
            var methodParameters = expectedMethod.GetParameters();
            
            if (!ParametersMatch(methodParameters, parameters))
            {
                Trace.WriteLine($"Restier ConventionBasedChangeSetItemFilter found '{expectedMethod}', but it has an incorrect number of arguments or the types don't match. The number of arguments should be 1.");
                return Task.CompletedTask;
            }
            
            //RWM: We've bounced you out of every situation where we can't process anything. So do the work.
            try
            {
                var result = expectedMethod.Invoke(target, parameters);
                if (result is Task resultTask)
                {
                    return resultTask;
                }
                return Task.CompletedTask;
            }
            catch (TargetInvocationException ex)
            {
                throw new ConventionInvocationException($"ConventionBasedChangeSetItemFilter {expectedMethod} invocation failed. Check the inner exception for more details.", ex.InnerException);
            }
        }

    }

}