// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A convention-based change set item authorizer.
    /// </summary>
    internal class ConventionBasedChangeSetItemAuthorizer : IChangeSetItemAuthorizer
    {
        private Type targetType;

        private ConventionBasedChangeSetItemAuthorizer(Type targetType)
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
            services.AddService<IChangeSetItemAuthorizer>(
                (sp, next) => new ConventionBasedChangeSetItemAuthorizer(targetType));
        }

        /// <inheritdoc/>
        public Task<bool> AuthorizeAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");
            bool result = true;

            Type returnType = typeof(bool);
            string methodName = GetAuthorizeMethodName(item);
            MethodInfo method = this.targetType.GetQualifiedMethod(methodName);

            if (method != null && method.IsFamily &&
                method.ReturnType == returnType)
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.GetApiService<ApiBase>();
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

        private static string GetAuthorizeMethodName(ChangeSetItem item)
        {
            switch (item.Type)
            {
                case ChangeSetItemType.DataModification:
                    DataModificationItem dataModification = (DataModificationItem)item;
                    string operationName = null;
                    if (dataModification.DataModificationItemAction == DataModificationItemAction.Insert)
                    {
                        operationName = ConventionBasedChangeSetConstants.AuthorizeMethodDataModificationInsert;
                    }
                    else if (dataModification.DataModificationItemAction == DataModificationItemAction.Update)
                    {
                        operationName = ConventionBasedChangeSetConstants.AuthorizeMethodDataModificationUpdate;
                    }
                    else if (dataModification.DataModificationItemAction == DataModificationItemAction.Remove)
                    {
                        operationName = ConventionBasedChangeSetConstants.AuthorizeMethodDataModificationDelete;
                    }

                    return operationName + dataModification.ResourceSetName;

                default:
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, item.Type));
            }
        }
    }
}
