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

namespace Microsoft.Restier.AspNetCore.Model;

/// <summary>
/// Builds operations based on the model.
/// </summary>
public class RestierWebApiOperationModelBuilder : IModelBuilder
{
    private readonly Type targetApiType;
    private readonly List<OperationMethodInfo> operationInfos = new();
    private readonly RestierWebApiModelExtender restierWebApiModelExtender;

    /// <summary>
    /// Gets or sets the inner model builder.
    /// </summary>
    public IModelBuilder Inner { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestierWebApiOperationModelBuilder"/> class.
    /// </summary>
    /// <param name="targetApiType">/The target type.</param>
    /// <param name="restierWebApiModelExtender">The model extender to check EntitySets against.</param>
    public RestierWebApiOperationModelBuilder(Type targetApiType, RestierWebApiModelExtender restierWebApiModelExtender)
    {
        Ensure.NotNull(targetApiType, nameof(targetApiType));
        Ensure.NotNull(restierWebApiModelExtender, nameof(restierWebApiModelExtender));
        this.targetApiType = targetApiType;
        this.restierWebApiModelExtender = restierWebApiModelExtender;
    }

    /// <inheritdoc />
    public IEdmModel GetEdmModel()
    {
        EdmModel model = null;
        if (Inner is not null)
        {
            model = Inner.GetEdmModel() as EdmModel;
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

    private IEdmExpression BuildEntitySetExpression(IEdmModel model, string entitySetName, IEdmTypeReference returnTypeReference)
    {
        if (entitySetName is null && returnTypeReference is not null)
        {
            var entitySets = model.FindDeclaredEntitySetsByTypeReference(returnTypeReference);

            foreach (var entitySet in entitySets)
            {
                if (restierWebApiModelExtender.EntitySetProperties.Any(p => p.Name == entitySet.Name))
                {
                    continue;
                }

                // return the original entityset, not a resource from the API.
                return new EdmPathExpression(entitySet.Name);
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

            // Dedup by namespace+name — RestierOperationExecutor dispatches by name only
            // (see RestierOperationExecutor.cs and the comment around the GetMethod call),
            // so a same-name pair would be either unreachable or trigger AmbiguousMatchException.
            // Same-signature overloads are out of scope; see the magical-operations design.
            var alreadyDeclared = model.SchemaElements.OfType<IEdmOperation>()
                .Any(op => op.Namespace == namespaceName && op.Name == operationInfo.Name);
            if (alreadyDeclared)
            {
                Trace.TraceWarning(
                    $"Restier: An operation named '{namespaceName}.{operationInfo.Name}' is already declared on the model " +
                    $"(likely via a custom ODataModelBuilder registration). Skipping the duplicate registration from " +
                    $"[Operation] attribute. Remove either the manual registration or the [Operation] attribute to silence this warning. " +
                    $"Note: same-name overloads are not supported by RestierOperationExecutor (resolves by name only).");
                continue;
            }

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