// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Microsoft.Data.Domain.Model;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Domain;

namespace Microsoft.Data.Domain.Conventions
{
    public class ConventionalActionProvider : IModelExtender
    {
        private Type targetType;

        private ConventionalActionProvider(Type targetType)
        {
            this.targetType = targetType;
        }

        public static void ApplyTo(DomainConfiguration configuration, Type targetType)
        {
            ConventionalActionProvider provider = new ConventionalActionProvider(targetType);
            configuration.AddHookPoint(typeof(IModelExtender), provider);
        }

        public Task ExtendModelAsync(
            ModelContext context,
            CancellationToken cancellationToken)
        {
            var model = context.Model;
            var entityContainer = model.EntityContainer as EdmEntityContainer;

            foreach (ActionMethodInfo actionInfo in this.ActionInfos)
            {
                var returnTypeReference = ConventionalActionProvider.GetReturnTypeReference(actionInfo.Method.ReturnType);
                var action = new EdmAction(actionInfo.ActionNamespace, actionInfo.ActionName, returnTypeReference);

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
            return Task.FromResult<object>(null);
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
                    .Select(m => new ActionMethodInfo {
                        Method = m,
                        ActionAttribute = m.GetCustomAttributes<ActionAttribute>(true).FirstOrDefault() })
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
