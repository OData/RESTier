﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using EdmPathExpression = Microsoft.OData.Edm.EdmPathExpression;

namespace Microsoft.Restier.AspNet.Model
{

    /// <summary>
    /// 
    /// </summary>
    internal class RestierWebApiOperationModelBuilder : IModelBuilder
    {
        private readonly Type targetApiType;
        private readonly ICollection<OperationMethodInfo> operationInfos = new List<OperationMethodInfo>();

        internal RestierWebApiOperationModelBuilder(Type targetApiType, IModelBuilder innerModelBuilder)
        {
            this.targetApiType = targetApiType;
            this.InnerModelBuilder = innerModelBuilder;
        }

        private IModelBuilder InnerModelBuilder { get; }

        public IEdmModel GetModel(ModelContext context)
        {
            EdmModel model = null;
            if (InnerModelBuilder != null)
            {
                model = InnerModelBuilder.GetModel(context) as EdmModel;
            }

            if (model == null)
            {
                // We don't plan to extend an empty model with operations.
                return null;
            }

            ScanForOperations();

            string existingNamespace = null;
            if (model.DeclaredNamespaces != null)
            {
                existingNamespace = model.DeclaredNamespaces.FirstOrDefault();
            }

            BuildOperations(model, existingNamespace);
            return model;
        }

        private static void BuildOperationParameters(EdmOperation operation, MethodInfo method, IEdmModel model)
        {
            foreach (var parameter in method.GetParameters())
            {
                var parameterTypeReference = parameter.ParameterType.GetTypeReference(model);
                var operationParam = new EdmOperationParameter(
                    operation,
                    parameter.Name,
                    parameterTypeReference);

                operation.AddParameter(operationParam);
            }
        }

        private static EdmPathExpression BuildBoundOperationReturnTypePathExpression(
            IEdmTypeReference returnTypeReference, ParameterInfo bindingParameter, IEdmModel model)
        {
            // Bound actions or functions that return an entity or a collection of entities
            // MAY specify a value for the EntitySetPath attribute
            // if determination of the entity set for the return type is contingent on the binding parameter.
            // The value for the EntitySetPath attribute consists of a series of segments
            // joined together with forward slashes.
            // The first segment of the entity set path MUST be the name of the binding parameter.
            // The remaining segments of the entity set path MUST represent navigation segments or type casts.

            // Here, if the return type matches the binding parameter type, (and no bindingPath has already been set)
            // assume they are from the same entity set
            if (returnTypeReference != null &&
                  returnTypeReference.Definition.AsElementType() is IEdmEntityType returnType &&
                bindingParameter != null &&
                  bindingParameter.ParameterType.GetReturnTypeReference(model)?.Definition.AsElementType() is IEdmStructuredType parameterType &&
                parameterType.IsOrInheritsFrom(returnType))
            {
                return new EdmPathExpression(bindingParameter.Name);
            }

            return null;
        }

        private static IEdmExpression BuildEntitySetExpression(
            IEdmModel model, string entitySetName, IEdmTypeReference returnTypeReference)
        {
            if (entitySetName == null && returnTypeReference != null)
            {
                var entitySet = model.FindDeclaredEntitySetByTypeReference(returnTypeReference);
                if (entitySet != null)
                {
                    entitySetName = entitySet.Name;
                }
            }

            if (entitySetName != null)
            {
                return new EdmPathExpression(entitySetName);
            }

            return null;
        }

        private static string GetNamespaceName(OperationMethodInfo methodInfo, string modelNamespace)
        {
            // customized the namespace logic, customized namespace is P0
            var namespaceName = methodInfo.OperationAttribute.Namespace;

            if (namespaceName != null)
            {
                return namespaceName;
            }

            if (modelNamespace != null)
            {
                return modelNamespace;
            }

            // This returns defined class namespace
            return methodInfo.Namespace;
        }

        private void ScanForOperations()
        {
            var methods = targetApiType.GetMethods(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.Instance);

            foreach (var method in methods)
            {
                var operationAttribute = method.GetCustomAttributes<OperationAttribute>(true).FirstOrDefault();
                if (operationAttribute != null)
                {
                    operationInfos.Add(new OperationMethodInfo
                    {
                        Method = method,
                        OperationAttribute = operationAttribute
                    });
                }
            }
        }

        private void BuildOperations(EdmModel model, string modelNamespace)
        {
            foreach (var operationMethodInfo in operationInfos)
            {
                // With this method, if return type is nullable type,it will get underlying type
                var returnType = TypeHelper.GetUnderlyingTypeOrSelf(operationMethodInfo.Method.ReturnType);
                var returnTypeReference = returnType.GetReturnTypeReference(model);
                var isBound = operationMethodInfo.IsBound;
                var bindingParameter = operationMethodInfo.Method.GetParameters().FirstOrDefault();

                if (bindingParameter == null && isBound)
                {
                    // Ignore the method which is marked as bounded but no parameters
                    continue;
                }

                var namespaceName = GetNamespaceName(operationMethodInfo, modelNamespace);

                EdmOperation operation = null;
                EdmPathExpression path = null;
                if (isBound)
                {
                    path = string.IsNullOrEmpty(operationMethodInfo.EntitySet)
                        ? BuildBoundOperationReturnTypePathExpression(returnTypeReference, bindingParameter, model)
                        : new EdmPathExpression(operationMethodInfo.EntitySet);
                }

                if (operationMethodInfo.HasSideEffects)
                {
                    operation = new EdmAction(namespaceName, operationMethodInfo.Name, returnTypeReference, isBound, path);
                }
                else
                {
                    operation = new EdmFunction(namespaceName, operationMethodInfo.Name, returnTypeReference, isBound, path, operationMethodInfo.IsComposable);
                }

                BuildOperationParameters(operation, operationMethodInfo.Method, model);
                model.AddElement(operation);

                if (!isBound)
                {
                    // entitySetReferenceExpression refer to an entity set containing entities returned
                    // by this function/action import.
                    var entitySetExpression = BuildEntitySetExpression(model, operationMethodInfo.EntitySet, returnTypeReference);
                    var entityContainer = model.EnsureEntityContainer(targetApiType);
                    if (operationMethodInfo.HasSideEffects)
                    {
                        entityContainer.AddActionImport(operation.Name, (EdmAction)operation, entitySetExpression);
                    }
                    else
                    {
                        entityContainer.AddFunctionImport(operation.Name, (EdmFunction)operation, entitySetExpression);
                    }
                }
            }
        }

        private class OperationMethodInfo
        {
            public MethodInfo Method { get; set; }

            public OperationAttribute OperationAttribute { get; set; }

            public string Name => Method.Name;

            public string Namespace => OperationAttribute.Namespace ?? Method.DeclaringType.Namespace;

            public string EntitySet => OperationAttribute.EntitySet;

            public bool IsComposable => OperationAttribute.IsComposable;

            public bool IsBound => OperationAttribute.IsBound;

            public bool HasSideEffects => OperationAttribute.OperationType == OperationType.Action;
        }

    }

}