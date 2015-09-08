// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin
{
    public class ConventionalActionProvider :IModelBuilder, IDelegateHookHandler<IModelBuilder>
    {
        private Type targetType;

        private ConventionalActionProvider(Type targetType)
        {
            this.targetType = targetType;
        }

        public IModelBuilder InnerHandler { get; set; }

        public static void ApplyTo(DomainConfiguration configuration, Type targetType)
        {
            ConventionalActionProvider provider = new ConventionalActionProvider(targetType);
            configuration.AddHookHandler<IModelBuilder>(provider);
        }

        public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            EdmModel model = null;
            if (this.InnerHandler != null)
            {
                model = await this.InnerHandler.GetModelAsync(context, cancellationToken) as EdmModel;
            }

            Debug.Assert(model != null);

            var entityContainer = model.EntityContainer as EdmEntityContainer;
            Debug.Assert(entityContainer != null);

            foreach (ActionMethodInfo actionInfo in this.ActionInfos)
            {
                var returnTypeReference = ConventionalActionProvider.GetReturnTypeReference(actionInfo.Method.ReturnType);
                var action = new EdmAction(entityContainer.Namespace, actionInfo.ActionName, returnTypeReference);

                foreach (ParameterInfo parameter in actionInfo.Method.GetParameters())
                {
                    EdmOperationParameter actionParam = new EdmOperationParameter(
                        action,
                        parameter.Name,
                        ConventionalActionProvider.GetTypeReference(parameter.ParameterType));

                    action.AddParameter(actionParam);
                }

                model.AddElement(action);

                if (!action.IsBound)
                {
                    EdmActionImport actionImport = new EdmActionImport(entityContainer, action.Name, action);
                    entityContainer.AddElement(actionImport);
                }
            }

            return model;
        }

        private static EdmTypeReference GetReturnTypeReference(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // if the action returns a Task<T>, map that to just be returning a T
                type = type.GetGenericArguments()[0];
            }
            else if (type == typeof(Task))
            {
                // if the action returns a concrete Task, map that to being a void return type.
                type = typeof(void);
            }

            return ConventionalActionProvider.GetTypeReference(type);
        }

        private static EdmTypeReference GetTypeReference(Type type)
        {
            // Only handle primitive type right now
            bool isNullable;
            EdmPrimitiveTypeKind? primitiveTypeKind = EdmHelpers.GetPrimitiveTypeKind(type, out isNullable);

            if (!primitiveTypeKind.HasValue)
            {
                return null;
            }

            return new EdmPrimitiveTypeReference(
                EdmCoreModel.Instance.GetPrimitiveType(primitiveTypeKind.Value),
                isNullable);
        }

        private IEnumerable<ActionMethodInfo> ActionInfos
        {
            get
            {
                MethodInfo[] methods = this.targetType.GetMethods(
                    BindingFlags.NonPublic |
                    BindingFlags.Public |
                    BindingFlags.Static |
                    BindingFlags.Instance);

                return methods
                    .Select(m => new ActionMethodInfo
                    {
                        Method = m,
                        ActionAttribute = m.GetCustomAttributes<ActionAttribute>(true).FirstOrDefault()
                    })
                    .Where(m => m.ActionAttribute != null);
            }
        }

        private class ActionMethodInfo
        {
            public MethodInfo Method { get; set; }
            public ActionAttribute ActionAttribute { get; set; }

            public string ActionName
            {
                get { return this.ActionAttribute.Name ?? this.Method.Name; }
            }

            public string ActionNamespace
            {
                get { return this.ActionAttribute.Namespace ?? this.Method.DeclaringType.Namespace; }
            }
        }
    }
}
