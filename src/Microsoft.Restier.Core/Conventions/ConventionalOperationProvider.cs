// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Library.Expressions;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Shared;

namespace Microsoft.Restier.Core.Conventions
{
    internal class ConventionalOperationProvider : IModelBuilder, IDelegateHookHandler<IModelBuilder>
    {
        private readonly Type targetType;
        private readonly ICollection<ActionMethodInfo> actionInfos = new List<ActionMethodInfo>();
        private readonly ICollection<FunctionMethodInfo> functionInfos = new List<FunctionMethodInfo>();

        private ConventionalOperationProvider(Type targetType)
        {
            this.targetType = targetType;
        }

        public IModelBuilder InnerHandler { get; set; }

        public static void ApplyTo(DomainConfiguration configuration, Type targetType)
        {
            ConventionalOperationProvider provider = new ConventionalOperationProvider(targetType);
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
            this.ScanForOperations();
            this.BuildFunctions(context, model);
            this.BuildActions(context, model);
            return model;
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

        private static bool TryGetElementType(Type type, out Type elementType)
        {
            elementType = null;
            if (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                type.GetGenericTypeDefinition() == typeof(IQueryable<>)))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            return false;
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
            if (!TryGetElementType(firstParameter.ParameterType, out parameterType))
            {
                parameterType = firstParameter.ParameterType;
            }

            var edmType = model.FindDeclaredType(parameterType.FullName);
            var entityType = edmType as IEdmEntityType;
            if (entityType == null)
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
            if (TryGetElementType(type, out elementType))
            {
                return EdmCoreModel.GetCollection(GetTypeReference(elementType, model));
            }

            var edmType = model.FindDeclaredType(type.FullName);
            var entityType = edmType as IEdmEntityType;
            if (entityType != null)
            {
                return new EdmEntityTypeReference(entityType, true);
            }

            return GetPrimitiveTypeReference(type);
        }

        private static EdmTypeReference GetPrimitiveTypeReference(Type type)
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
                if (returnTypeReference.IsCollection())
                {
                    returnTypeReference = returnTypeReference.AsCollection().ElementType();
                }

                entitySet = model.EntityContainer.EntitySets()
                    .SingleOrDefault(e => e.EntityType().FullTypeName() == returnTypeReference.FullName());
            }

            if (entitySet != null)
            {
                return new EdmEntitySetReferenceExpression(entitySet);
            }

            return null;
        }

        private static object GetDomainInstance(DomainContext context)
        {
            return context.GetProperty(typeof(Domain).AssemblyQualifiedName);
        }

        private static EdmEntityContainer EnsureEntityContainer(InvocationContext context, EdmModel model)
        {
            var container = (EdmEntityContainer)model.EntityContainer;
            if (container == null)
            {
                var domainInstance = GetDomainInstance(context.DomainContext);
                var domainNamespace = domainInstance.GetType().Namespace;
                container = new EdmEntityContainer(domainNamespace, "DefaultContainer");
                model.AddElement(container);
            }

            return container;
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
                var functionAttribute = method.GetCustomAttributes<FunctionAttribute>(true).FirstOrDefault();
                if (functionAttribute != null)
                {
                    functionInfos.Add(new FunctionMethodInfo
                    {
                        Method = method,
                        FunctionAttribute = functionAttribute
                    });
                }

                var actionAttribute = method.GetCustomAttributes<ActionAttribute>(true).FirstOrDefault();
                if (actionAttribute != null)
                {
                    actionInfos.Add(new ActionMethodInfo
                    {
                        Method = method,
                        ActionAttribute = actionAttribute
                    });
                }
            }
        }

        private void BuildActions(InvocationContext context, EdmModel model)
        {
            foreach (ActionMethodInfo actionInfo in this.actionInfos)
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
                    var entityContainer = EnsureEntityContainer(context, model);
                    entityContainer.AddActionImport(
                        action.Name, action, entitySetReferenceExpression);
                }
            }
        }

        private void BuildFunctions(InvocationContext context, EdmModel model)
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
                    var entityContainer = EnsureEntityContainer(context, model);
                    entityContainer.AddFunctionImport(function.Name, function, entitySetReferenceExpression);
                }
            }
        }

        private class ActionMethodInfo
        {
            public MethodInfo Method { get; set; }

            public ActionAttribute ActionAttribute { get; set; }

            public string Name
            {
                get { return this.ActionAttribute.Name ?? this.Method.Name; }
            }

            public string Namespace
            {
                get { return this.ActionAttribute.Namespace ?? this.Method.DeclaringType.Namespace; }
            }

            public string EntitySet
            {
                get { return this.ActionAttribute.EntitySet;  }
            }
        }

        private class FunctionMethodInfo
        {
            public MethodInfo Method { get; set; }

            public FunctionAttribute FunctionAttribute { get; set; }

            public string Name
            {
                get { return this.FunctionAttribute.Name ?? this.Method.Name; }
            }

            public string Namespace
            {
                get { return this.FunctionAttribute.Namespace ?? this.Method.DeclaringType.Namespace; }
            }

            public string EntitySet
            {
                get { return this.FunctionAttribute.EntitySet; }
            }

            public bool IsComposable
            {
                get { return this.FunctionAttribute.IsComposable; }
            }
        }
    }
}
