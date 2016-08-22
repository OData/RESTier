// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Operation;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based operation authorizer.
    /// </summary>
    internal class ConventionBasedOperationAuthorizer : IOperationAuthorizer
    {
        private Type targetType;

        private ConventionBasedOperationAuthorizer(Type targetType)
        {
            Ensure.NotNull(targetType, "targetType");
            this.targetType = targetType;
        }

        public static void ApplyTo(
            IServiceCollection services,
            Type targetType)
        {
            Ensure.NotNull(services, "services");
            Ensure.NotNull(targetType, "targetType");
            services.AddService<IOperationAuthorizer>((sp, next) => new ConventionBasedOperationAuthorizer(targetType));
        }

        /// <inheritdoc/>
        public Task<bool> AuthorizeAsync(
            OperationContext context,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");
            bool result = true;

            Type returnType = typeof(bool);
            var methodName = ConventionBasedChangeSetConstants.AuthorizeMethodActionInvocationExecute +
                        context.OperationName;
            MethodInfo method = this.targetType.GetQualifiedMethod(methodName);

            if (method != null && method.IsFamily &&
                method.ReturnType == returnType)
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.ImplementInstance;
                    if (target == null ||
                        !this.targetType.IsInstanceOfType(target))
                    {
                        return Task.FromResult(result);
                    }
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    result = (bool)method.Invoke(target, null);
                }
            }

            return Task.FromResult(result);
        }
    }
}
