// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Properties;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core.Conventions
{
    /// <summary>
    /// A convention-based change set entry authorizer.
    /// </summary>
    internal class ConventionBasedChangeSetAuthorizer : IChangeSetEntryAuthorizer
    {
        private Type targetType;

        private ConventionBasedChangeSetAuthorizer(Type targetType)
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
            services.CutoffPrevious<IChangeSetEntryAuthorizer>(new ConventionBasedChangeSetAuthorizer(targetType));
        }

        /// <inheritdoc/>
        public Task<bool> AuthorizeAsync(
            SubmitContext context,
            ChangeSetEntry entry,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");
            bool result = true;

            Type returnType = typeof(bool);
            string methodName = ConventionBasedChangeSetAuthorizer.GetAuthorizeMethodName(entry);
            MethodInfo method = this.targetType.GetQualifiedMethod(methodName);

            if (method != null && method.IsFamily &&
                method.ReturnType == returnType)
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.ApiContext.
                        ServiceProvider.GetService(targetType);
                    if (target == null ||
                        !this.targetType.IsAssignableFrom(target.GetType()))
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

        private static string GetAuthorizeMethodName(ChangeSetEntry entry)
        {
            switch (entry.Type)
            {
                case ChangeSetEntryType.DataModification:
                    DataModificationEntry dataModification = (DataModificationEntry)entry;
                    string operationName = null;
                    if (dataModification.IsNew)
                    {
                        operationName = ConventionBasedChangeSetConstants.AuthorizeMethodDataModificationInsert;
                    }
                    else if (dataModification.IsUpdate)
                    {
                        operationName = ConventionBasedChangeSetConstants.AuthorizeMethodDataModificationUpdate;
                    }
                    else if (dataModification.IsDelete)
                    {
                        operationName = ConventionBasedChangeSetConstants.AuthorizeMethodDataModificationDelete;
                    }

                    return operationName + dataModification.EntitySetName;

                case ChangeSetEntryType.ActionInvocation:
                    ActionInvocationEntry actionEntry = (ActionInvocationEntry)entry;
                    return ConventionBasedChangeSetConstants.AuthorizeMethodActionInvocationExecute +
                        actionEntry.ActionName;

                default:
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, entry.Type));
            }
        }
    }
}
