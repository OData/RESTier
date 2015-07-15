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
    /// <summary>
    /// The conventional implementation of <see cref="IModelExtender" />.
    /// </summary>
    public class ConventionalModelExtender : IModelExtender
    {
        private Type targetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionalModelExtender" /> class.
        /// </summary>
        /// <param name="targetType">The target type on which to invoke model extending.</param>
        public ConventionalModelExtender(Type targetType)
        {
            this.targetType = targetType;
        }

        /// <summary>
        /// Apply a <see cref="ConventionalModelExtender"/> instance to the <see cref="DomainConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The domain configuration.</param>
        /// <param name="targetType">The target type on which to invoke model extending.</param>
        public static void ApplyTo(DomainConfiguration configuration, Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");
            configuration.AddHookPoint(typeof(IModelExtender), new ConventionalModelExtender(targetType));
        }

        /// <summary>
        /// Asynchronously extends the model.
        /// </summary>
        /// <param name="context">The context that contains the model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public Task ExtendModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            ExtendModel(context);
            return Task.WhenAll();
        }

        private void ExtendModel(ModelContext context)
        {
            var method = this.targetType.GetQualifiedMethod("OnModelExtending");
            var returnType = typeof(EdmModel);

            if (method == null || method.ReturnType != returnType)
            {
                return;
            }

            object target = null;
            if (!method.IsStatic)
            {
                target = context.DomainContext.GetProperty(targetType.AssemblyQualifiedName);
                if (target == null || !targetType.IsInstanceOfType(target))
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
