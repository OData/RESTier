// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using EdmPathExpression = Microsoft.OData.Edm.EdmPathExpression;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore.Model
#else
namespace Microsoft.Restier.AspNet.Model
#endif
{
    /// <summary>
    /// Builds operations based on the model.
    /// </summary>
    internal class RestierWebApiOperationModelBuilder : IModelBuilder
    {

        #region Private Members

        private readonly Type targetApiType;
        private readonly List<OperationMethodInfo> operationInfos = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the inner model builder.
        /// </summary>
        private IModelBuilder InnerModelBuilder { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierWebApiOperationModelBuilder"/> class.
        /// </summary>
        /// <param name="targetApiType">/The target type.</param>
        /// <param name="innerModelBuilder">The inner model Builder.</param>
        internal RestierWebApiOperationModelBuilder(Type targetApiType, IModelBuilder innerModelBuilder)
        {
            this.targetApiType = targetApiType;
            InnerModelBuilder = innerModelBuilder;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public IEdmModel GetModel(ModelContext context)
        {
            EdmModel model = null;
            if (InnerModelBuilder is not null)
            {
                model = InnerModelBuilder.GetModel(context) as EdmModel;
            }

            if (model is null)
            {
                // We don't plan to extend an empty model with operations.
                return null;
            }

            ScanForOperations();

            string existingNamespace = null;
            if (model.DeclaredNamespaces is not null)
            {
                existingNamespace = model.DeclaredNamespaces.FirstOrDefault();
            }

            BuildOperations(model, existingNamespace);
            return model;
        }

        #endregion

        #region Private Methods

        private static EdmPathExpression BuildBoundOperationReturnTypePathExpression(IEdmTypeReference returnTypeReference, ParameterInfo bindingParameter, IEdmModel model)
        {

            IEdmStructuredType parameterType;
            IEdmEntityType returnType;

            // @mikepizzo: If the return type matches the binding parameter type, (and no bindingPath has already been set)
            // assume they are from the same entity set.


            if (returnTypeReference is not null &&
                  (returnType = returnTypeReference.Definition.AsElementType() as IEdmEntityType) is not null &&
                bindingParameter is not null &&
                  (parameterType = bindingParameter.ParameterType.GetReturnTypeReference(model)?.Definition.AsElementType() as IEdmStructuredType) is not null &&
                parameterType.IsOrInheritsFrom(returnType))
            {
                return new EdmPathExpression(bindingParameter.Name);
            }

            return null;
        }

        private static IEdmExpression BuildEntitySetExpression(IEdmModel model, string entitySetName, IEdmTypeReference returnTypeReference)
        {
            if (entitySetName is null && returnTypeReference is not null)
            {
                var entitySet = model.FindDeclaredEntitySetByTypeReference(returnTypeReference);
                if (entitySet is not null)
                {
                    entitySetName = entitySet.Name;
                }
            }

            if (entitySetName is not null)
            {
                return new EdmPathExpression(entitySetName);
            }

            return null;
        }

        private static void BuildOperationParameters(EdmOperation operation, MethodInfo method, IEdmModel model)
        {
            foreach (var parameter in method.GetParameters())
            {
                var parameterTypeReference = parameter.ParameterType.GetTypeReference(model);
                var operationParam = new EdmOperationParameter(operation, parameter.Name, parameterTypeReference);
                operation.AddParameter(operationParam);
            }
        }

        private void BuildOperations(EdmModel model, string modelNamespace)
        {

            foreach (var operationInfo in operationInfos)
            {
                EdmOperation operation = null;
                EdmPathExpression path = null;

                // With this method, if return type is nullable type,it will get underlying type
                var returnType = TypeHelper.GetUnderlyingTypeOrSelf(operationInfo.Method.ReturnType);
                var returnTypeReference = returnType.GetReturnTypeReference(model);
                var namespaceName = GetNamespaceName(operationInfo, modelNamespace);

                // @robertmclaws: We're setting isBound here, so we can negate it later if a BindingParameter is not found.
                var isBound = operationInfo.OperationAttribute is BoundOperationAttribute;

                if (isBound)
                {
                    var bindingParameter = operationInfo.Method.GetParameters().FirstOrDefault();
                    if (bindingParameter is not null)
                    {
                        path = !string.IsNullOrWhiteSpace(operationInfo.EntitySetPath)
                            ? new EdmPathExpression(operationInfo.EntitySetPath)
                            : BuildBoundOperationReturnTypePathExpression(returnTypeReference, bindingParameter, model);
                    }
                    else
                    {
                        Trace.TraceWarning($"Restier: The operation '{operationInfo.Name}' was marked with [BoundOperation], but no parameters were " +
                            $"specified to bind against. Restier will register this as an unbound operation instead. Please change the method to add a parameter," +
                            $"or use [UnboundOperation] instead.");
                        isBound = false;
                    }
                }

                switch (operationInfo.OperationType)
                {
                    case OperationType.Action:
                        operation = new EdmAction(namespaceName, operationInfo.Name, returnTypeReference, isBound, path);
                        break;
                    case OperationType.Function:
                        operation = new EdmFunction(namespaceName, operationInfo.Name, returnTypeReference, isBound, path, operationInfo.IsComposable);
                        break;
                }

                BuildOperationParameters(operation, operationInfo.Method, model);
                model.AddElement(operation);

                //RWM: Bound Operations are done at this point. Unbound operations are referenced in the EntityContainer.
                if (isBound) continue;

                // entitySetReferenceExpression refer to an entity set containing entities returned by this function/action import.
                var entitySetExpression = BuildEntitySetExpression(model, operationInfo.EntitySet, returnTypeReference);
                var entityContainer = model.EnsureEntityContainer(targetApiType);

                switch (operationInfo.OperationType)
                {
                    case OperationType.Action:
                        entityContainer.AddActionImport(operation.Name, (EdmAction)operation, entitySetExpression);
                        break;
                    case OperationType.Function:
                        entityContainer.AddFunctionImport(operation.Name, (EdmFunction)operation, entitySetExpression);
                        break;
                }

            }

        }

        private static string GetNamespaceName(OperationMethodInfo methodInfo, string modelNamespace)
        {
            // customized the namespace logic, customized namespace is P0
            var namespaceName = methodInfo.OperationAttribute.Namespace;

            if (namespaceName is not null)
            {
                return namespaceName;
            }

            if (modelNamespace is not null)
            {
                return modelNamespace;
            }

            // This returns defined class namespace
            return methodInfo.Namespace;
        }

        private void ScanForOperations()
        {
            var methods = targetApiType
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
                // @robertmclaws: Let's limit what we return to exclude getters/setters and any methods on System.Object.
                .Where(c => !c.IsSpecialName && c.DeclaringType != typeof(object));

            operationInfos.AddRange(methods
                .Select(c => new OperationMethodInfo
                {
                    Method = c,
                    OperationAttribute = c.GetCustomAttribute<OperationAttribute>(true)
                })
                .Where(c => c.OperationAttribute is not null)
                .ToList());
        }

        #endregion

        private class OperationMethodInfo
        {
            public MethodInfo Method { get; set; }

            public OperationAttribute OperationAttribute { get; set; }

            public string Name => Method.Name;

            public string Namespace => OperationAttribute.Namespace ?? Method.DeclaringType.Namespace;

            public string EntitySet => (OperationAttribute as UnboundOperationAttribute)?.EntitySet ?? null;

            public string EntitySetPath => (OperationAttribute as BoundOperationAttribute)?.EntitySetPath ?? null;

            public bool IsComposable => OperationAttribute.IsComposable;

            public OperationType OperationType => OperationAttribute.OperationType;
        }

    }

}