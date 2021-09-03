// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Operation;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based change set item filter.
    /// </summary>
    public class ConventionBasedOperationFilter : IOperationFilter
    {
        private readonly Type targetApiType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedOperationFilter"/> class.
        /// </summary>
        /// <param name="targetApiType">The target type to check for filter functions.</param>
        public ConventionBasedOperationFilter(Type targetApiType)
        {
            Ensure.NotNull(targetApiType, nameof(targetApiType));
            this.targetApiType = targetApiType;
        }

        /// <inheritdoc/>
        public Task OnOperationExecutingAsync(OperationContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            return InvokeProcessorMethodAsync(context, RestierPipelineState.PreSubmit);
        }

        /// <inheritdoc/>
        public Task OnOperationExecutedAsync(OperationContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            return InvokeProcessorMethodAsync(context, RestierPipelineState.PostSubmit);
        }

        private static bool ParametersMatch(ParameterInfo[] methodParameters, object[] parameters)
        {
            return methodParameters.Length == parameters.Length && !methodParameters.Where((mp, i) => !mp.ParameterType.IsInstanceOfType(parameters[i])).Any();
        }

        private Task InvokeProcessorMethodAsync(OperationContext context, RestierPipelineState pipelineState)
        {
            var parameters = context.ParameterValues?.ToArray() ?? Array.Empty<object>();
            var expectedMethodName = ConventionBasedMethodNameFactory.GetFunctionMethodName(context, pipelineState, RestierOperationMethod.Execute);
            var expectedMethod = targetApiType.GetQualifiedMethod(expectedMethodName);

            if (expectedMethod == null)
            {
                return Task.CompletedTask;
            }

            if (!expectedMethod.IsFamily && !expectedMethod.IsFamilyOrAssembly)
            {
                Trace.WriteLine($"Restier Filter found '{expectedMethod}' but it is inaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
                return Task.CompletedTask;
            }

            if (expectedMethod.ReturnType != typeof(void) && !typeof(Task).IsAssignableFrom(expectedMethod.ReturnType))
            {
                Trace.WriteLine($"Restier Filter found '{expectedMethod}' but it does not return void or a Task. Your method will not be called until you correct the return type.");
                return Task.CompletedTask;
            }

            object target = null;
            if (!expectedMethod.IsStatic)
            {
                target = context.Api;
                if (!targetApiType.IsInstanceOfType(target))
                {
                    Trace.WriteLine("The Restier API is of the incorrect type.");
                    return Task.CompletedTask;
                }
            }

            var methodParameters = expectedMethod.GetParameters();
            if (ParametersMatch(methodParameters, parameters))
            {
                try
                {
                    var result = expectedMethod.Invoke(target, parameters);
                    if (result is Task resultTask)
                    {
                        return resultTask;
                    }
                }
                catch (TargetInvocationException ex)
                {
                    throw new ConventionInvocationException($"Authorizer {expectedMethod} invocation failed. Check the inner exception for more details.", ex.InnerException);
                }
            }

            Trace.WriteLine($"Restier Authorizer found '{expectedMethod}', but it has an incorrect number of arguments or the types don't match. The number of arguments should be 1.");
            return Task.CompletedTask;
        }
    }
}
