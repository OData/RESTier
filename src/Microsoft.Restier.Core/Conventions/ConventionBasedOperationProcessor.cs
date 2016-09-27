﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Operation;

namespace Microsoft.Restier.Core.Conventions
{
    /// <summary>
    /// A convention-based change set item filter.
    /// </summary>
    internal class ConventionBasedOperationProcessor : IOperationProcessor
    {
        private Type targetType;

        private ConventionBasedOperationProcessor(Type targetType)
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
            services.AddService<IOperationProcessor>(
                (sp, next) => new ConventionBasedOperationProcessor(targetType));
        }

        /// <inheritdoc/>
        public Task OnExecutingOperationAsync(
            OperationContext context,
            CancellationToken cancellationToken)
        {
            return this.InvokeProcessorMethodAsync(
                context, ConventionBasedChangeSetConstants.FilterMethodNamePreFilterSuffix);
        }

        /// <inheritdoc/>
        public Task OnExecutedOperationAsync(
            OperationContext context,
            CancellationToken cancellationToken)
        {
            return this.InvokeProcessorMethodAsync(
                context, ConventionBasedChangeSetConstants.FilterMethodNamePostFilterSuffix);
        }

        private static bool ParametersMatch(ParameterInfo[] methodParameters, object[] parameters)
        {
            return methodParameters.Length == parameters.Length
                && !methodParameters.Where((mp, i) => !mp.ParameterType.IsInstanceOfType(parameters[i])).Any();
        }

        private Task InvokeProcessorMethodAsync(
            OperationContext context,
            string methodNameSuffix)
        {
            string methodName = ConventionBasedChangeSetConstants.FilterMethodActionInvocationExecute +
                    methodNameSuffix + context.OperationName;
            object[] parameters = null;
            if (context.ParametersValue != null)
            {
                context.ParametersValue.ToArray();
            }

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
