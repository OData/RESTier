using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Domain.Submit;

namespace Microsoft.Data.Domain.Conventions
{
    /// <summary>
    /// A conventional change set entry authorizer.
    /// </summary>
    public class ConventionalChangeSetAuthorizer : IChangeSetEntryAuthorizer
    {
        private Type _targetType;

        private ConventionalChangeSetAuthorizer(Type targetType)
        {
            Ensure.NotNull(targetType, "targetType");
            this._targetType = targetType;
        }

        /// <inheritdoc/>
        public static void ApplyTo(
            DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");
            configuration.AddHookPoint(typeof(IChangeSetEntryAuthorizer),
                new ConventionalChangeSetAuthorizer(targetType));
        }

        /// <inheritdoc/>
        public Task<bool> AuthorizeAsync(
            SubmitContext context, ChangeSetEntry entry,
            CancellationToken cancellationToken)
        {
            bool result = true;

            Type returnType = typeof(bool);
            string methodName = ConventionalChangeSetAuthorizer.GetAuthorizeMethodName(entry);
            MethodInfo method = this._targetType.GetMethod(
                methodName,
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase |
                BindingFlags.DeclaredOnly);

            if (method != null && method.IsPrivate &&
                method.ReturnType == returnType)
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.DomainContext.GetProperty(
                        this._targetType.AssemblyQualifiedName);
                    if (target == null ||
                        !this._targetType.IsAssignableFrom(target.GetType()))
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
                        operationName = "Insert";
                    }
                    else if (dataModification.IsUpdate)
                    {
                        operationName = "Update";
                    }
                    else if (dataModification.IsDelete)
                    {
                        operationName = "Delete";
                    }
                    return "Can" + operationName + dataModification.EntitySetName;

                case ChangeSetEntryType.ActionInvocation:
                    ActionInvocationEntry actionEntry = (ActionInvocationEntry)entry;
                    return "CanExecute" + actionEntry.ActionName;

                default:
                    throw new InvalidOperationException("Invalid ChangeSetEntry Type: " + entry.Type);
            }
        }
    }
}
