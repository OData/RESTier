// Copyright (c) Microsoft Corporation.  All rights reserved.
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
using Microsoft.Restier.AspNet.Formatter;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;


namespace Microsoft.Restier.AspNet.Operation
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

            // Authorization check
#pragma warning disable CA1062 // Validate arguments of public methods. JWS: Ensure.NotNull is there. Spurious warning.
            await InvokeAuthorizers(context, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1062 // Validate arguments of public methods.

            // model build does not support operation with same name
            // So method with same name but different signature is not considered.
            var method = context.Api.GetType().GetMethod(context.OperationName, 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (method == null)
            {
                throw new NotImplementedException(Resources.OperationNotImplemented);
            }

            var parameterArray = method.GetParameters();

            var model = await context.Api.GetModelAsync(cancellationToken).ConfigureAwait(false);

            // Parameters of method and model is exactly mapped or there is parsing error
            var parameters = new object[parameterArray.Length];

            var paraIndex = 0;
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
                        context.Request.GetRequestContainer()); // JWS: As long as OData requires the ServiceProvder,
                                                                //      we have to provide it. DI abuse smell.
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
            await PerformPreEvent(context, cancellationToken).ConfigureAwait(false);

            var result = await InvokeOperation(context.Api, method, parameters, model).ConfigureAwait(false);

            // Invoke preprocessing on the operation execution
            await PerformPostEvent(context, cancellationToken).ConfigureAwait(false);
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
                    throw new StatusCodeException(HttpStatusCode.NotFound, Resources.ResourceNotFound);
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
            var result = method.Invoke(instanceImplementMethod, parameters);
            var returnType = method.ReturnType;
            if (returnType == typeof(void))
            {
                return null;
            }

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
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

            var edmReturnType = returnType.GetReturnTypeReference(model);
            if (edmReturnType.IsCollection())
            {
                var elementClrType = returnType.GetElementType() ?? returnType.GenericTypeArguments[0];
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
            var typedQueryable = ExpressionHelperMethods.QueryableAsQueryable.Invoke(null, new object[] { castedResult }) as IQueryable;

            return typedQueryable;
        }

        private async Task InvokeAuthorizers(OperationContext context, CancellationToken cancellationToken)
        {
            if (operationAuthorizer == null)
            {
                return;
            }

            if (!await operationAuthorizer.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false))
            {
                throw new SecurityException(string.Format(CultureInfo.InvariantCulture, Resources.OperationUnAuthorizationExecution, context.OperationName));
            }
        }

        private async Task PerformPreEvent(OperationContext context, CancellationToken cancellationToken)
        {
            if (operationFilter != null)
            {
                await operationFilter.OnOperationExecutingAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task PerformPostEvent(OperationContext context, CancellationToken cancellationToken)
        {
            if (operationFilter != null)
            {
                await operationFilter.OnOperationExecutedAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
