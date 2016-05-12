// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Library.Expressions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.WebApi.Model
{
    internal class RestierOperationModelBuilder : IModelBuilder
    {
        private readonly Type targetType;
        private readonly ICollection<FunctionMethodInfo> actionInfos = new List<FunctionMethodInfo>();
        private readonly ICollection<FunctionMethodInfo> functionInfos = new List<FunctionMethodInfo>();

        private RestierOperationModelBuilder(Type targetType)
        {
            this.targetType = targetType;
        }

        private IModelBuilder InnerHandler { get; set; }

        public static void ApplyTo(IServiceCollection services, Type targetType)
        {
            services.AddService<IModelBuilder>((sp, next) => new RestierOperationModelBuilder(targetType)
            {
                InnerHandler = next,
            });
        }

        public async Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            EdmModel model = null;
            if (this.InnerHandler != null)
            {
                model = await this.InnerHandler.GetModelAsync(context, cancellationToken) as EdmModel;
            }

            if (model == null)
            {
                // We don't plan to extend an empty model with operations.
                return null;
            }

            this.ScanForOperations();
            this.BuildFunctions(model);
            this.BuildActions(model);
            return model;
        }

        private static bool TryGetEntityType(IEdmModel model, Type type, out IEdmEntityType entityType)
        {
            var edmType = model.FindDeclaredType(type.FullName);
            entityType = edmType as IEdmEntityType;
            return entityType != null;
        }

        private static void BuildOperationParameters(EdmOperation operation, MethodInfo method, IEdmModel model)
        {
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                var parameterTypeReference = GetTypeReference(parameter.ParameterType, model);
                var operationParam = new EdmOperationParameter(
                    operation,
                    parameter.Name,
                    parameterTypeReference);

                operation.AddParameter(operationParam);
            }
        }

        private static bool TryGetBindingParameter(
            MethodInfo method, IEdmModel model, out ParameterInfo bindingParameter)
        {
            bindingParameter = null;
            var firstParameter = method.GetParameters().FirstOrDefault();
            if (firstParameter == null)
            {
                return false;
            }

            Type parameterType;
            if (!firstParameter.ParameterType.TryGetElementType(out parameterType))
            {
                parameterType = firstParameter.ParameterType;
            }

            if (!GetTypeReference(parameterType, model).IsEntity())
            {
                return false;
            }

            bindingParameter = firstParameter;
            return true;
        }

        private static IEdmTypeReference GetReturnTypeReference(Type type, IEdmModel model)
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

            return GetTypeReference(type, model);
        }

        private static IEdmTypeReference GetTypeReference(Type type, IEdmModel model)
        {
            Type elementType;
            if (type.TryGetElementType(out elementType))
            {
                return EdmCoreModel.GetCollection(GetTypeReference(elementType, model));
            }

            IEdmEntityType entityType;
            if (TryGetEntityType(model, type, out entityType))
            {
                return new EdmEntityTypeReference(entityType, true);
            }

            return type.GetPrimitiveTypeReference();
        }

        private static EdmPathExpression BuildEntitySetPathExpression(
            IEdmTypeReference returnTypeReference, ParameterInfo bindingParameter)
        {
            if (returnTypeReference != null &&
                returnTypeReference.IsEntity() &&
                bindingParameter != null)
            {
                return new EdmPathExpression(bindingParameter.Name);
            }

            return null;
        }

        private static EdmEntitySetReferenceExpression BuildEntitySetReferenceExpression(
            IEdmModel model, string entitySetName, IEdmTypeReference returnTypeReference)
        {
            IEdmEntitySet entitySet = null;
            if (entitySetName != null)
            {
                entitySet = model.EntityContainer.FindEntitySet(entitySetName);
            }

            if (entitySet == null && returnTypeReference != null)
            {
                entitySet = model.FindDeclaredEntitySetByTypeReference(returnTypeReference);
            }

            if (entitySet != null)
            {
                return new EdmEntitySetReferenceExpression(entitySet);
            }

            return null;
        }

        private void ScanForOperations()
        {
            var methods = this.targetType.GetMethods(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.Instance);

            foreach (var method in methods)
            {
                var functionAttribute = method.GetCustomAttributes<OperationAttribute>(true).FirstOrDefault();
                if (functionAttribute != null)
                {
                    if (!functionAttribute.HasSideEffects)
                    {
                        functionInfos.Add(new FunctionMethodInfo
                        {
                            Method = method,
                            OperationAttribute = functionAttribute
                        });
                    }
                    else
                    {
                        actionInfos.Add(new FunctionMethodInfo
                        {
                            Method = method,
                            OperationAttribute = functionAttribute
                        });
                    }
                }
            }
        }

        private void BuildActions(EdmModel model)
        {
            foreach (FunctionMethodInfo actionInfo in this.actionInfos)
            {
                var returnTypeReference = GetReturnTypeReference(actionInfo.Method.ReturnType, model);

                ParameterInfo bindingParameter;
                bool isBound = TryGetBindingParameter(actionInfo.Method, model, out bindingParameter);

                var action = new EdmAction(
                    actionInfo.Namespace,
                    actionInfo.Name,
                    returnTypeReference,
                    isBound,
                    BuildEntitySetPathExpression(returnTypeReference, bindingParameter));
                BuildOperationParameters(action, actionInfo.Method, model);
                model.AddElement(action);

                if (!isBound)
                {
                    var entitySetReferenceExpression = BuildEntitySetReferenceExpression(
                        model, actionInfo.EntitySet, returnTypeReference);
                    var entityContainer = model.EnsureEntityContainer(this.targetType);
                    entityContainer.AddActionImport(
                        action.Name, action, entitySetReferenceExpression);
                }
            }
        }

        private void BuildFunctions(EdmModel model)
        {
            foreach (FunctionMethodInfo functionInfo in this.functionInfos)
            {
                var returnTypeReference = GetReturnTypeReference(functionInfo.Method.ReturnType, model);

                ParameterInfo bindingParameter;
                bool isBound = TryGetBindingParameter(functionInfo.Method, model, out bindingParameter);

                var function = new EdmFunction(
                    functionInfo.Namespace,
                    functionInfo.Name,
                    returnTypeReference,
                    isBound,
                    BuildEntitySetPathExpression(returnTypeReference, bindingParameter),
                    functionInfo.IsComposable);
                BuildOperationParameters(function, functionInfo.Method, model);
                model.AddElement(function);

                if (!isBound)
                {
                    var entitySetReferenceExpression = BuildEntitySetReferenceExpression(
                        model, functionInfo.EntitySet, returnTypeReference);
                    var entityContainer = model.EnsureEntityContainer(this.targetType);
                    entityContainer.AddFunctionImport(
                        function.Name, function, entitySetReferenceExpression);
                }
            }
        }

        private class FunctionMethodInfo
        {
            public MethodInfo Method { get; set; }

            public OperationAttribute OperationAttribute { get; set; }

            public string Name
            {
                get { return this.OperationAttribute.Name ?? this.Method.Name; }
            }

            public string Namespace
            {
                get { return this.OperationAttribute.Namespace ?? this.Method.DeclaringType.Namespace; }
            }

            public string EntitySet
            {
                get { return this.OperationAttribute.EntitySet; }
            }

            public bool IsComposable
            {
                get { return this.OperationAttribute.IsComposable; }
            }
        }
    }
}
