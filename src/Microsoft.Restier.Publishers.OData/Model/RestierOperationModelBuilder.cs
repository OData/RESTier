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
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using EdmPathExpression = Microsoft.OData.Edm.EdmPathExpression;

namespace Microsoft.Restier.Publishers.OData.Model
{
    internal class RestierOperationModelBuilder : IModelBuilder
    {
        private readonly Type targetType;
        private readonly ICollection<OperationMethodInfo> operationInfos = new List<OperationMethodInfo>();

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

            string existingNamespace = null;
            if (model.DeclaredNamespaces != null)
            {
                existingNamespace = model.DeclaredNamespaces.FirstOrDefault();
            }

            this.BuildOperations(model, existingNamespace);
            return model;
        }

        private static void BuildOperationParameters(EdmOperation operation, MethodInfo method, IEdmModel model)
        {
            foreach (ParameterInfo parameter in method.GetParameters())
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
            IEdmTypeReference returnTypeReference, ParameterInfo bindingParameter)
        {
            // Bound actions or functions that return an entity or a collection of entities
            // MAY specify a value for the EntitySetPath attribute
            // if determination of the entity set for the return type is contingent on the binding parameter.
            // The value for the EntitySetPath attribute consists of a series of segments
            // joined together with forward slashes.
            // The first segment of the entity set path MUST be the name of the binding parameter.
            // The remaining segments of the entity set path MUST represent navigation segments or type casts.
            if (returnTypeReference != null &&
                returnTypeReference.IsEntity() &&
                bindingParameter != null)
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
            string namespaceName = methodInfo.OperationAttribute.Namespace;

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
            var methods = this.targetType.GetMethods(
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
            foreach (OperationMethodInfo operationMethodInfo in this.operationInfos)
            {
                // With this method, if return type is nullable type,it will get underlying type
                var returnType = TypeHelper.GetUnderlyingTypeOrSelf(operationMethodInfo.Method.ReturnType);
                var returnTypeReference = returnType.GetReturnTypeReference(model);
                bool isBound = operationMethodInfo.IsBound;
                var bindingParameter = operationMethodInfo.Method.GetParameters().FirstOrDefault();

                if (bindingParameter == null && isBound)
                {
                    // Ignore the method which is marked as bounded but no parameters
                    continue;
                }

                string namespaceName = GetNamespaceName(operationMethodInfo, modelNamespace);

                EdmOperation operation = null;
                EdmPathExpression path = null;
                if (isBound)
                {
                    // Unbound actions or functions should not have EntitySetPath attribute
                    path = BuildBoundOperationReturnTypePathExpression(returnTypeReference, bindingParameter);
                }

                if (operationMethodInfo.HasSideEffects)
                {
                    operation = new EdmAction(
                        namespaceName,
                        operationMethodInfo.Name,
                        returnTypeReference,
                        isBound,
                        path);
                }
                else
                {
                    operation = new EdmFunction(
                        namespaceName,
                        operationMethodInfo.Name,
                        returnTypeReference,
                        isBound,
                        path,
                        operationMethodInfo.IsComposable);
                }

                BuildOperationParameters(operation, operationMethodInfo.Method, model);
                model.AddElement(operation);

                if (!isBound)
                {
                    // entitySetReferenceExpression refer to an entity set containing entities returned
                    // by this function/action import.
                    var entitySetExpression = BuildEntitySetExpression(
                        model, operationMethodInfo.EntitySet, returnTypeReference);
                    var entityContainer = model.EnsureEntityContainer(this.targetType);
                    if (operationMethodInfo.HasSideEffects)
                    {
                        entityContainer.AddActionImport(operation.Name, (EdmAction)operation, entitySetExpression);
                    }
                    else
                    {
                        entityContainer.AddFunctionImport(
                            operation.Name, (EdmFunction)operation, entitySetExpression);
                    }
                }
            }
        }

        private class OperationMethodInfo
        {
            public MethodInfo Method { get; set; }

            public OperationAttribute OperationAttribute { get; set; }

            public string Name
            {
                get { return this.Method.Name; }
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

            public bool IsBound
            {
                get { return this.OperationAttribute.IsBound; }
            }

            public bool HasSideEffects
            {
                get { return this.OperationAttribute.HasSideEffects; }
            }
        }
    }
}
