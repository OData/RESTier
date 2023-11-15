﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData.Edm;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.Restier.AspNetCore.Formatter;
using Microsoft.Restier.AspNetCore.Model;
using AspNetResources = Microsoft.Restier.AspNetCore.Resources;
#else
using Microsoft.Restier.AspNet.Formatter;
using Microsoft.Restier.AspNet.Model;
using AspNetResources = Microsoft.Restier.AspNet.Resources;
#endif
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;

#if NETCOREAPP3_1_OR_GREATER
namespace Microsoft.Restier.AspNetCore.Operation
#else
namespace Microsoft.Restier.AspNet.Operation
#endif
{
    /// <summary>
    /// Executes an operation by invoking a method on the <see cref="ApiBase"/> instance through reflection.
    /// </summary>
    public class RestierOperationExecutor : IOperationExecutor
    {
        private readonly IOperationAuthorizer operationAuthorizer;
        private readonly IOperationFilter operationFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierOperationExecutor"/> class.
        /// </summary>
        /// <param name="operationAuthorizer">The operation authorizer to be used for authorization.</param>
        /// <param name="operationFilter">The operation filter to be used for filtering.</param>
        public RestierOperationExecutor(IOperationAuthorizer operationAuthorizer, IOperationFilter operationFilter)
        {
            this.operationAuthorizer = operationAuthorizer;
            this.operationFilter = operationFilter;
        }

        /// <summary>
        /// Asynchronously executes an operation.
        /// </summary>
        /// <param name="context">
        /// The operation context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a operation result.
        /// </returns>
        public async Task<IQueryable> ExecuteOperationAsync(OperationContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));

            if (context is not RestierOperationContext restierOperationContext)
            {
                throw new NotImplementedException(string.Format(CultureInfo.InvariantCulture, AspNetResources.NoSupportedOperationContext, context.GetType()));
            }

            // Authorization check
#pragma warning disable CA1062 // Validate arguments of public methods. JWS: Ensure.NotNull is there. Spurious warning.
            await InvokeAuthorizers(restierOperationContext, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1062 // Validate arguments of public methods.

            // model build does not support operation with same name
            // So method with same name but different signature is not considered.
            var method = context.Api.GetType().GetMethod(
                restierOperationContext.OperationName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (method is null)
            {
                throw new NotImplementedException(AspNetResources.OperationNotImplemented);
            }

            var parameterArray = method.GetParameters();

            var model = restierOperationContext.Api.GetModel();

            // Parameters of method and model is exactly mapped or there is parsing error
            var parameters = new object[parameterArray.Length];

            var paraIndex = 0;
            if (restierOperationContext.BindingParameterValue is not null)
            {
                // Add binding parameter which is first parameter of method
                parameters[0] = PrepareBindingParameter(parameterArray[0].ParameterType, restierOperationContext.BindingParameterValue);
                paraIndex = 1;
            }

            for (; paraIndex < parameterArray.Length; paraIndex++)
            {
                var parameter = parameterArray[paraIndex];
                var currentParameterValue = restierOperationContext.GetParameterValueFunc(parameter.Name);

                object convertedValue;
                if (restierOperationContext.IsFunction)
                {
                    var parameterTypeRef = parameter.ParameterType.GetTypeReference(model);

                    // Change to right CLR class for collection/Enum/Complex/Entity
                    // JWS: As long as OData requires the ServiceProvider, we have to provide it. DI abuse smell.
                    convertedValue = DeserializationHelpers.ConvertValue(
                        currentParameterValue,
                        parameter.Name,
                        parameter.ParameterType,
                        parameterTypeRef,
                        model,
                        restierOperationContext.Request,
                        restierOperationContext.Request.GetRequestContainer());
                }
                else
                {
                    convertedValue = DeserializationHelpers.ConvertCollectionType(
                        currentParameterValue, parameter.ParameterType);
                }

                parameters[paraIndex] = convertedValue;
            }

            restierOperationContext.ParameterValues = parameters;

            // RWM: Invoke pre-operation processing.
            await PerformPreEvent(restierOperationContext, cancellationToken).ConfigureAwait(false);

            // RWM: Invoke the operation.
            var result = await InvokeOperation(restierOperationContext.Api, method, parameters, model).ConfigureAwait(false);

            // Invoke post-operation processing.
            await PerformPostEvent(restierOperationContext, cancellationToken).ConfigureAwait(false);
            return result;
        }

        private static object PrepareBindingParameter(Type bindingType, IEnumerable bindingParameterValue)
        {
            var enumerableType = bindingType.FindGenericType(typeof(IEnumerable<>));

            // This means binding to a single entity
            if (enumerableType is null)
            {
                return bindingParameterValue.SingleOrDefault() ??
                    throw new StatusCodeException(HttpStatusCode.NotFound, AspNetResources.ResourceNotFound);
            }

            // This means function is bound to an entity set.
            // IQueryable should always have generic type argument
            var elementClrType = enumerableType.GenericTypeArguments[0];

            // For entity set, user can write as ICollection<> or IEnumerable<> or array as method parameters
            if (bindingType.IsArray)
            {
                var toArrayMethodInfo = ExpressionHelperMethods.EnumerableToArrayGeneric
                    .MakeGenericMethod(elementClrType);
                return toArrayMethodInfo.Invoke(null, new object[] { bindingParameterValue });
            }

            if (bindingType.FindGenericType(typeof(ICollection<>)) is not null)
            {
                var toListMethodInfo = ExpressionHelperMethods.EnumerableToListGeneric
                    .MakeGenericMethod(elementClrType);
                return toListMethodInfo.Invoke(null, new object[] { bindingParameterValue });
            }

            return bindingParameterValue;
        }

        private static async Task<IQueryable> InvokeOperation(
            object instanceImplementMethod, MethodInfo method, object[] parameters, IEdmModel model)
        {
            var result = method.Invoke(instanceImplementMethod, parameters);
            var returnType = method.ReturnType;
            if (returnType == typeof(void))
            {
                return null;
            }

            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                if (!returnType.GenericTypeArguments.Any())
                {
                    return null;
                }

                returnType = returnType.GenericTypeArguments.First();
                result = typeof(Task<>).MakeGenericType(returnType).GetProperty("Result").GetValue(task);
            }

            var edmReturnType = returnType.GetReturnTypeReference(model);
            if (edmReturnType.IsCollection())
            {
                var elementClrType = returnType.GetElementType() ?? returnType.GenericTypeArguments[0];
                if (result is null)
                {
                    return ExpressionHelpers.CreateEmptyQueryable(elementClrType);
                }

                if (result.GetType().FindGenericType(typeof(IEnumerable<>)) is not null)
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

            return ExpressionHelperMethods.QueryableAsQueryable.Invoke(null, new object[] { castedResult }) as IQueryable;
        }

        private async Task InvokeAuthorizers(OperationContext context, CancellationToken cancellationToken)
        {
            if (operationAuthorizer is null)
            {
                return;
            }

            if (!await operationAuthorizer.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false))
            {
                throw new SecurityException(string.Format(CultureInfo.InvariantCulture, AspNetResources.OperationUnAuthorizationExecution, context.OperationName));
            }
        }

        private async Task PerformPreEvent(OperationContext context, CancellationToken cancellationToken)
        {
            if (operationFilter is not null)
            {
                await operationFilter.OnOperationExecutingAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task PerformPostEvent(OperationContext context, CancellationToken cancellationToken)
        {
            if (operationFilter is not null)
            {
                await operationFilter.OnOperationExecutedAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
