// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Operation;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based operation authorizer.
    /// </summary>
    public class ConventionBasedOperationAuthorizer : IOperationAuthorizer
    {
        private Type targetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedOperationAuthorizer"/> class.
        /// </summary>
        /// <param name="targetType">The target type to check for authorizer functions.</param>
        public ConventionBasedOperationAuthorizer(Type targetType)
        {
            Ensure.NotNull(targetType, nameof(targetType));
            this.targetType = targetType;
        }

        /// <inheritdoc/>
        public Task<bool> AuthorizeAsync(OperationContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            var result = true;

            var returnType = typeof(bool);
            var methodName = ConventionBasedMethodNameFactory.GetFunctionMethodName(context, RestierPipelineState.Authorization, RestierOperationMethod.Execute);
            var method = targetType.GetQualifiedMethod(methodName);

            if (method != null && method.IsFamily && method.ReturnType == returnType)
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.Api;
                    if (target == null || !targetType.IsInstanceOfType(target))
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
