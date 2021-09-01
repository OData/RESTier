// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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
            var result = true;

            var returnType = typeof(bool);
            var dataModification = (DataModificationItem)item;
            var methodName = ConventionBasedMethodNameFactory.GetEntitySetMethodName(dataModification, RestierPipelineState.Authorization);
            var method = targetApiType.GetQualifiedMethod(methodName);

            if (method == null)
            {
                return Task.FromResult(result);
            }

            if (!method.IsFamily && !method.IsFamilyOrAssembly)
            {
                Trace.WriteLine($"Restier Authorizer found '{methodName}' but it is unaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
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
            result = (bool)method.Invoke(target, null);
            return Task.FromResult(result);
        }

    }

}