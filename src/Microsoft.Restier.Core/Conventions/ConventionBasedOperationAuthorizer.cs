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

            var expectedMethodName = ConventionBasedMethodNameFactory.GetFunctionMethodName(context, RestierPipelineState.Authorization, RestierOperationMethod.Execute);

            //RWM: This prefers the Sync name over the Async name, because in V1 Sync has been the only option for a decade. In v2, we'll probably just make everything Async without Sync calls.
            var expectedMethod = targetApiType.GetQualifiedMethod(expectedMethodName) ?? targetApiType.GetQualifiedMethod($"{expectedMethodName}Async");

            if (expectedMethod is null)
            {
                return Task.FromResult(result);
            }

            if (!expectedMethod.IsFamily && !expectedMethod.IsFamilyOrAssembly)
            {
                Trace.WriteLine($"Restier ConventionBasedOperationAuthorizer found '{expectedMethodName}' but it is inaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
                return Task.FromResult(result);
            }

            if (expectedMethod.ReturnType != typeof(bool))
            {
                Trace.WriteLine($"Restier ConventionBasedOperationAuthorizer found '{expectedMethodName}' but it does not return a boolean value. Your method will not be called until you correct the return type.");
                return Task.FromResult(result);
            }

            object target = null;
            if (!expectedMethod.IsStatic)
            {
                target = context.Api;
                if (!targetApiType.IsInstanceOfType(target))
                {
                    Trace.WriteLine("The Restier API is of the incorrect type.");
                    return Task.FromResult(result);
                }
            }

            var parameters = expectedMethod.GetParameters();
            if (parameters.Length > 0)
            {
                Trace.WriteLine($"Restier ConventionBasedOperationAuthorizer found '{expectedMethodName}', but it has an incorrect number of arguments. Found {parameters.Length} arguments, expected 0.");
                return Task.FromResult(result);
            }

            //RWM: We've bounced you out of every situation where we can't process anything. So do the work.
            try
            {
                result = (bool)expectedMethod.Invoke(target, null);
                return Task.FromResult(result);
            }
            catch (TargetInvocationException ex)
            {
                throw new ConventionInvocationException($"ConventionBasedOperationAuthorizer {expectedMethodName} invocation failed. Check the inner exception for more details.", ex.InnerException);
            }
        }

    }

}
