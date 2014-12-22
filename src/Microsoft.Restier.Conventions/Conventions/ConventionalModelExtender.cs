// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Conventions
{
    public class ConventionalModelExtender : IModelExtender
    {
        private Type _targetType;

        public ConventionalModelExtender(Type targetType)
        {
            _targetType = targetType;
        }

        public static void ApplyTo(DomainConfiguration configuration, Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");
            configuration.AddHookPoint(typeof(IModelExtender), new ConventionalModelExtender(targetType));
        }

        public Task ExtendModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            ExtendModel(context);
            return Task.WhenAll();
        }

        private void ExtendModel(ModelContext context)
        {
            var method = _targetType.GetMethod(
                "OnModelExtending",
                BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase |
                    BindingFlags.DeclaredOnly);
            var returnType = typeof(EdmModel);

            if (method == null || method.ReturnType != returnType)
            {
                return;
            }

            object target = null;
            if (!method.IsStatic)
            {
                target = context.DomainContext.GetProperty(_targetType.AssemblyQualifiedName);
                if (target == null || !_targetType.IsInstanceOfType(target))
                {
                    return;
                }
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != returnType)
            {
                return;
            }

            var model = context.Model;
            var result = (EdmModel)method.Invoke(target, new object[] { model });
            if (result != null && result != model)
            {
                context.Model = result;
            }
        }
    }
}
