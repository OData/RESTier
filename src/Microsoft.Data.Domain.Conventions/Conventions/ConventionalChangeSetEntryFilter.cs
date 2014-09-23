using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Domain.Submit;

namespace Microsoft.Data.Domain.Conventions
{
    /// <summary>
    /// A conventional change set entry filter.
    /// </summary>
    public class ConventionalChangeSetEntryFilter : IChangeSetEntryFilter
    {
        private Type _targetType;

        private ConventionalChangeSetEntryFilter(Type targetType)
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
            configuration.AddHookPoint(typeof(IChangeSetEntryFilter),
                new ConventionalChangeSetEntryFilter(targetType));
        }

        /// <inheritdoc/>
        public Task OnExecutingEntryAsync(
            SubmitContext context, ChangeSetEntry entry,
            CancellationToken cancellationToken)
        {
            return this.InvokeFilterMethodAsync(context, entry, "ing");
        }

        /// <inheritdoc/>
        public Task OnExecutedEntryAsync(
            SubmitContext context, ChangeSetEntry entry,
            CancellationToken cancellationToken)
        {
            return this.InvokeFilterMethodAsync(context, entry, "ed");
        }

        private Task InvokeFilterMethodAsync(
            SubmitContext context, ChangeSetEntry entry,
            string methodNameSuffix)
        {
            string methodName = ConventionalChangeSetEntryFilter.GetMethodName(entry, methodNameSuffix);
            object[] parameters = ConventionalChangeSetEntryFilter.GetParameters(entry);

            MethodInfo method = this._targetType.GetMethod(
                methodName,
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.IgnoreCase |
                BindingFlags.DeclaredOnly);

            if (method != null && 
                (method.ReturnType == typeof(void) ||
                typeof(Task).IsAssignableFrom(method.ReturnType)))
            {
                object target = null;
                if (!method.IsStatic)
                {
                    target = context.DomainContext.GetProperty(
                        this._targetType.AssemblyQualifiedName);
                    if (target == null ||
                        !this._targetType.IsAssignableFrom(target.GetType()))
                    {
                        return Task.WhenAll();
                    }
                }

                ParameterInfo[] methodParameters = method.GetParameters();
                if (ConventionalChangeSetEntryFilter.ParametersMatch(methodParameters, parameters))
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

        private static string GetMethodName(ChangeSetEntry entry, string suffix)
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
                        operationName = "Updat";
                    }
                    else if (dataModification.IsDelete)
                    {
                        operationName = "Delet";
                    }
                    return "On" + operationName + suffix + dataModification.EntitySetName;

                case ChangeSetEntryType.ActionInvocation:
                    ActionInvocationEntry actionEntry = (ActionInvocationEntry)entry;
                    return "OnExecut" + suffix + actionEntry.ActionName;

                default:
                    throw new InvalidOperationException("Invalid ChangeSetEntry Type: " + entry.Type);
            }
        }

        private static object[] GetParameters(ChangeSetEntry entry)
        {
            switch (entry.Type)
            {
                case ChangeSetEntryType.DataModification:
                    DataModificationEntry dataModification = (DataModificationEntry)entry;
                    return new object[] { dataModification.Entity };

                case ChangeSetEntryType.ActionInvocation:
                    ActionInvocationEntry actionEntry = (ActionInvocationEntry)entry;
                    return actionEntry.GetArgumentArray();

                default:
                    throw new InvalidOperationException("Invalid ChangeSetEntry Type: " + entry.Type);
            }
        }

        private static bool ParametersMatch(ParameterInfo[] methodParameters, object[] parameters)
        {
            bool match = methodParameters.Length == parameters.Length;
            if (match)
            {
                for (int i = 0; i < methodParameters.Length; i++)
                {
                    match &= methodParameters[i].ParameterType.IsAssignableFrom(parameters[i].GetType());
                }
            }
            return match;
        }
    }
}
