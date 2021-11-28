// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
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
        private readonly Type targetApiType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedOperationAuthorizer"/> class.
        /// </summary>
        /// <param name="targetApiType">The target type to check for authorizer functions.</param>
        public ConventionBasedOperationAuthorizer(Type targetApiType)
        {
            Ensure.NotNull(targetApiType, nameof(targetApiType));
            this.targetApiType = targetApiType;
        }

        /// <inheritdoc/>
        public Task<bool> AuthorizeAsync(OperationContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            var result = true;

            var returnType = typeof(bool);
            var methodName = ConventionBasedMethodNameFactory.GetFunctionMethodName(context, RestierPipelineState.Authorization, RestierOperationMethod.Execute);
            var method = targetApiType.GetQualifiedMethod(methodName);

            if (method is null)
            {
                return Task.FromResult(result);
            }

            if (!method.IsFamily && !method.IsFamilyOrAssembly)
            {
                Trace.WriteLine($"Restier Authorizer found '{methodName}' but it is inaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
                return Task.FromResult(result);
            }

            if (method.ReturnType != returnType)
            {
                Trace.WriteLine($"Restier Authorizer found '{methodName}' but it does not return a boolean value. Your method will not be called until you correct the return type.");
                return Task.FromResult(result);
            }

            object target = null;
            if (!method.IsStatic)
            {
                target = context.Api;
                if (!targetApiType.IsInstanceOfType(target))
                {
                    Trace.WriteLine("The Restier API is of the incorrect type.");
                    return Task.FromResult(result);
                }
            }

            var parameters = method.GetParameters();
            if (parameters.Length > 0)
            {
                Trace.WriteLine($"Restier Authorizer found '{methodName}', but it has an incorrect number of arguments. Found {parameters.Length} arguments, expected 0.");
                return Task.FromResult(result);
            }

            //RWM: We've bounced you out of every situation where we can't process anything. So do the work.
            try
            {
                result = (bool)method.Invoke(target, null);
                return Task.FromResult(result);
            }
            catch (TargetInvocationException ex)
            {
                throw new ConventionInvocationException($"Authorizer {methodName} invocation failed. Check the inner exception for more details.", ex.InnerException);
            }
        }

    }

}
