// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Publishers.OData.Formatter;
using Microsoft.Restier.Publishers.OData.Model;
using Microsoft.Restier.Publishers.OData.Properties;

namespace Microsoft.Restier.Publishers.OData.Operation
{
    internal class OperationExecutor : IOperationExecutor
    {
        public async Task<IQueryable> ExecuteOperationAsync(
            OperationContext context, CancellationToken cancellationToken)
        {
            // Authorization check
            await InvokeAuthorizers(context, cancellationToken);

            // model build does not support operation with same name
            // So method with same name but different signature is not considered.
            MethodInfo method = context.ImplementInstance.GetType().GetMethod(
                context.OperationName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (method == null)
            {
                throw new NotImplementedException(Resources.OperationNotImplemented);
            }

            var parameterArray = method.GetParameters();

            var model = context.GetApiService<IEdmModel>();

            // Parameters of method and model is exactly mapped or there is parsing error
            var parameters = new object[parameterArray.Length];

            int paraIndex = 0;
            if (context.BindingParameterValue != null)
            {
                // Add binding parameter which is first parameter of method
                parameters[0] = PrepareBindingParameter(parameterArray[0].ParameterType, context.BindingParameterValue);
                paraIndex = 1;
            }

            for (; paraIndex < parameterArray.Length; paraIndex++)
            {
                var parameter = parameterArray[paraIndex];
                var currentParameterValue = context.GetParameterValueFunc(parameter.Name);

                object convertedValue = null;
                if (context.IsFunction)
                {
                    var parameterTypeRef = parameter.ParameterType.GetTypeReference(model);

                    // Change to right CLR class for collection/Enum/Complex/Entity
                    convertedValue = DeserializationHelpers.ConvertValue(
                        currentParameterValue,
                        parameter.Name,
                        parameter.ParameterType,
                        parameterTypeRef,
                        model,
                        context.Request,
                        context.ServiceProvider);
                }
                else
                {
                    convertedValue = DeserializationHelpers.ConvertCollectionType(
                        currentParameterValue, parameter.ParameterType);
                }

                parameters[paraIndex] = convertedValue;
            }

            context.ParameterValues = parameters;

            // Invoke preprocessing on the operation execution
            PerformPreEvent(context, cancellationToken);

            var result = await InvokeOperation(context.ImplementInstance, method, parameters, model);

            // Invoke preprocessing on the operation execution
            PerformPostEvent(context, cancellationToken);
            return result;
        }

        private static object PrepareBindingParameter(Type bindingType, IEnumerable bindingParameterValue)
        {
            var enumerableType = bindingType.FindGenericType(typeof(IEnumerable<>));

            // This means binding to a single entity
            if (enumerableType == null)
            {
                var entity = bindingParameterValue.SingleOrDefault();
                if (entity == null)
                {
                    throw new ResourceNotFoundException(Resources.ResourceNotFound);
                }

                return entity;
            }

            // This means function is bound to an entity set.
            // IQueryable should always have generic type argument
            var elementClrType = enumerableType.GenericTypeArguments[0];

            // For entity set, user can write as ICollection<> or IEnumerable<> or array as method parameters
            if (bindingType.IsArray)
            {
                var toArrayMethodInfo = ExpressionHelperMethods.EnumerableToArrayGeneric
                    .MakeGenericMethod(elementClrType);
                var arrayResult = toArrayMethodInfo.Invoke(null, new object[] { bindingParameterValue });
                return arrayResult;
            }

            if (bindingType.FindGenericType(typeof(ICollection<>)) != null)
            {
                var toListMethodInfo = ExpressionHelperMethods.EnumerableToListGeneric
                    .MakeGenericMethod(elementClrType);
                var listResult = toListMethodInfo.Invoke(null, new object[] { bindingParameterValue });
                return listResult;
            }

            return bindingParameterValue;
        }

        private static async Task<IQueryable> InvokeOperation(
            object instanceImplementMethod, MethodInfo method, object[] parameters, IEdmModel model)
        {
            object result = method.Invoke(instanceImplementMethod, parameters);
            var returnType = method.ReturnType;
            if (returnType == typeof(void))
            {
                return null;
            }

            var task = result as Task;
            if (task != null)
            {
                await task;
                if (returnType.GenericTypeArguments.Any())
                {
                    returnType = returnType.GenericTypeArguments.First();
                    var resultProperty = typeof(Task<>).MakeGenericType(returnType).GetProperty("Result");
                    result = resultProperty.GetValue(task);
                }
                else
                {
                    return null;
                }
            }

            IEdmTypeReference edmReturnType = returnType.GetReturnTypeReference(model);
            if (edmReturnType.IsCollection())
            {
                var elementClrType = returnType.GetElementType() ??
                                     returnType.GenericTypeArguments[0];
                if (result == null)
                {
                    return ExpressionHelpers.CreateEmptyQueryable(elementClrType);
                }

                var enumerableType = result.GetType().FindGenericType(typeof(IEnumerable<>));
                if (enumerableType != null)
                {
                    return ((IEnumerable)result).AsQueryable();
                }

                // Should never hint this path, add here to make sure collection result will not hint single result part
                return ExpressionHelpers.CreateEmptyQueryable(elementClrType);
            }

            // This means this is single result
            // cannot return new[] { result }.AsQueryable(); as need to return in its own type but not object type
            var objectQueryable = new[] { result }.AsQueryable();
            var castMethodInfo = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(returnType);
            var castedResult = castMethodInfo.Invoke(null, new object[] { objectQueryable });
            var typedQueryable = ExpressionHelperMethods.QueryableAsQueryable
                .Invoke(null, new object[] { castedResult }) as IQueryable;

            return typedQueryable;
        }

        private static async Task InvokeAuthorizers(
            OperationContext context,
            CancellationToken cancellationToken)
        {
            var authorizor = context.GetApiService<IOperationAuthorizer>();
            if (authorizor == null)
            {
                return;
            }

            if (!await authorizor.AuthorizeAsync(context, cancellationToken))
            {
                throw new SecurityException(string.Format(
                    CultureInfo.InvariantCulture, Resources.OperationUnAuthorizationExecution, context.OperationName));
            }
        }

        private static void PerformPreEvent(OperationContext context, CancellationToken cancellationToken)
        {
            var processor = context.GetApiService<IOperationFilter>();
            if (processor != null)
            {
                processor.OnOperationExecutingAsync(context, cancellationToken);
            }
        }

        private static void PerformPostEvent(OperationContext context, CancellationToken cancellationToken)
        {
            var processor = context.GetApiService<IOperationFilter>();
            if (processor != null)
            {
                processor.OnOperationExecutedAsync(context, cancellationToken);
            }
        }
    }
}
