// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based change set item authorizer.
    /// </summary>
    public class ConventionBasedChangeSetItemAuthorizer : IChangeSetItemAuthorizer
    {
        private readonly Type targetApiType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedChangeSetItemAuthorizer"/> class.
        /// </summary>
        /// <param name="targetApiType">The target type to check for authorizer functions.</param>
        public ConventionBasedChangeSetItemAuthorizer(Type targetApiType)
        {
            Ensure.NotNull(targetApiType, nameof(targetApiType));
            this.targetApiType = targetApiType;
        }

        /// <inheritdoc/>
        public Task<bool> AuthorizeAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            Ensure.NotNull(item, nameof(item));

            var dataModification = (DataModificationItem)item;
            var expectedMethodName = ConventionBasedMethodNameFactory.GetEntitySetMethodName(dataModification, RestierPipelineState.Authorization);
            var expectedMethod = targetApiType.GetQualifiedMethod(expectedMethodName) ?? targetApiType.GetQualifiedMethod($"{expectedMethodName}Async");

            if (expectedMethod is null)
            {
                return Task.FromResult(true);
            }

            if (!expectedMethod.IsFamily && !expectedMethod.IsFamilyOrAssembly)
            {
                Trace.WriteLine($"Restier Authorizer found '{expectedMethod}' but it is inaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
                return Task.FromResult(true);
            }

            if (expectedMethod.ReturnType != typeof(bool) && !typeof(Task<bool>).IsAssignableFrom(expectedMethod.ReturnType))
            {
                Trace.WriteLine($"Restier Authorizer found '{expectedMethod}' but it does not return a boolean value. Your method will not be called until you correct the return type.");
                return Task.FromResult(true);
            }

            object target = null;
            if (!expectedMethod.IsStatic)
            {
                target = context.Api;
                if (!targetApiType.IsInstanceOfType(target))
                {
                    Trace.WriteLine("The Restier API is of the incorrect type.");
                    return Task.FromResult(true);
                }
            }

            var parameters = expectedMethod.GetParameters();
            if (parameters.Length > 0)
            {
                Trace.WriteLine($"Restier Authorizer found '{expectedMethod}', but it has an incorrect number of arguments. Found {parameters.Length} arguments, expected 0.");
                return Task.FromResult(true);
            }

            //RWM: We've bounced you out of every situation where we can't process anything. So do the work.
            try
            {
                var result = expectedMethod.Invoke(target, null);
                if (result is Task<bool> resultTask)
                {
                    return resultTask;
                }
                return Task.FromResult((bool)result);
            }
            catch (TargetInvocationException ex)
            {
                throw new ConventionInvocationException($"Authorizer {expectedMethod} invocation failed. Check the inner exception for more details.", ex.InnerException);
            }
        }

    }

}