// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Operation;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based change set item filter.
    /// </summary>
    internal class ConventionBasedOperationFilter : IOperationFilter
    {
        private Type targetType;

        private ConventionBasedOperationFilter(Type targetType)
        {
            Ensure.NotNull(targetType, nameof(targetType));
            this.targetType = targetType;
        }

        /// <inheritdoc/>
        public static void ApplyTo(IServiceCollection services, Type targetType)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(targetType, nameof(targetType));
            services.AddService<IOperationFilter>((sp, next) => new ConventionBasedOperationFilter(targetType));
        }

        /// <inheritdoc/>
        public Task OnOperationExecutingAsync(OperationContext context, CancellationToken cancellationToken)
        {
            return InvokeProcessorMethodAsync(context, RestierPipelineState.PreSubmit);
        }

        /// <inheritdoc/>
        public Task OnOperationExecutedAsync(OperationContext context, CancellationToken cancellationToken)
        {
            return InvokeProcessorMethodAsync(context, RestierPipelineState.PostSubmit);
        }

        private static bool ParametersMatch(ParameterInfo[] methodParameters, object[] parameters)
        {
            return methodParameters.Length == parameters.Length && !methodParameters.Where((mp, i) => !mp.ParameterType.IsInstanceOfType(parameters[i])).Any();
        }

        private Task InvokeProcessorMethodAsync(OperationContext context, RestierPipelineState pipelineState)
        {
            var methodName = ConventionBasedMethodNameFactory.GetFunctionMethodName(context, pipelineState, RestierOperationMethod.Execute);
            object[] parameters = null;
            if (context.ParameterValues != null)
            {
                context.ParameterValues.ToArray();
            }

            var method = targetType.GetQualifiedMethod(methodName);

            if (method != null && (method.ReturnType == typeof(void) || typeof(Task).IsAssignableFrom(method.ReturnType)))
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.ImplementInstance;
                    if (target == null || !targetType.IsInstanceOfType(target))
                    {
                        return Task.WhenAll();
                    }
                }

                var methodParameters = method.GetParameters();
                if (ParametersMatch(methodParameters, parameters))
                {
                    var result = method.Invoke(target, parameters);
                    if (result is Task resultTask)
                    {
                        return resultTask;
                    }
                }
            }

            return Task.WhenAll();
        }
    }
}
