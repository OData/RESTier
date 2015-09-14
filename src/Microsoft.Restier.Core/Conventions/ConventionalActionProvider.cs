// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Library.Expressions;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Shared;

namespace Microsoft.Restier.Core.Conventions
{
    internal class ConventionalActionProvider : IModelBuilder, IDelegateHookHandler<IModelBuilder>
    {
        private Type targetType;

        private ConventionalActionProvider(Type targetType)
        {
            this.targetType = targetType;
        }

        public IModelBuilder InnerHandler { get; set; }

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

            Ensure.NotNull(model, "model");

            var entityContainer = (EdmEntityContainer)model.EntityContainer;
            foreach (ActionMethodInfo actionInfo in this.ActionInfos)
            {
                EdmTypeReference returnTypeReference = null;
                var returnType = model.FindDeclaredType(actionInfo.Method.ReturnType.FullName);
                var entityReturnType = returnType as IEdmEntityType;
                if (entityReturnType != null)
                {
                    returnTypeReference = new EdmEntityTypeReference(entityReturnType, true);
                }
                else
                {
                    returnTypeReference =
                        ConventionalActionProvider.GetReturnTypeReference(actionInfo.Method.ReturnType);
                }

                var parameters = actionInfo.Method.GetParameters();

                bool isBound = false;
                var firstParameter = parameters.FirstOrDefault();
                if (firstParameter != null)
                {
                    var parameterType = model.FindDeclaredType(firstParameter.ParameterType.FullName);
                    var entityParameterType = parameterType as IEdmEntityType;
                    if (entityParameterType != null)
                    {
                        isBound = true;
                    }
                }

                var action = new EdmAction(
                    actionInfo.ActionNamespace,
                    actionInfo.ActionName,
                    returnTypeReference,
                    isBound,
                    entityReturnType != null ? new EdmPathExpression(firstParameter.Name) : null);

                foreach (ParameterInfo parameter in parameters)
                {
                    EdmTypeReference parameterTypeReference = null;
                    var parameterType = model.FindDeclaredType(parameter.ParameterType.FullName);
                    var entityParameterType = parameterType as IEdmEntityType;
                    if (entityParameterType != null)
                    {
                        parameterTypeReference = new EdmEntityTypeReference(entityParameterType, false);
                    }
                    else
                    {
                        parameterTypeReference = ConventionalActionProvider.GetTypeReference(parameter.ParameterType);
                    }

                    EdmOperationParameter actionParam = new EdmOperationParameter(
                        action,
                        parameter.Name,
                        parameterTypeReference);

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
