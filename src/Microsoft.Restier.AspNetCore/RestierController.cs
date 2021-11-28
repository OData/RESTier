// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.AspNetCore.Operation;
using Microsoft.Restier.AspNetCore.Query;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNetCore
{
    // This is a must for creating response with correct extension method
    using ODataPath = Microsoft.AspNet.OData.Routing.ODataPath;

    /// <summary>
    /// The all-in-one controller class to handle API requests.
    /// </summary>
    [ODataFormatting]
    [RestierExceptionFilter]
    public class RestierController : ODataController
    {
        private const string IfMatchKey = "@IfMatchKey";
        private const string IfNoneMatchKey = "@IfNoneMatchKey";

        private ApiBase api;
        private ODataValidationSettings validationSettings;
        private IOperationExecutor operationExecutor;
        private ODataQuerySettings querySettings;

        private bool shouldReturnCount;
        private bool shouldWriteRawValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierController"/> class.
        /// </summary>
        public RestierController()
        {
        }

        /// <summary>
        /// Handles a GET request to query entities.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the response message.</returns>
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            EnsureInitialized();

            var path = GetPath();
            var lastSegment = path.Segments.LastOrDefault();
            if (lastSegment is null)
            {
                throw new InvalidOperationException(Resources.ControllerRequiresPath);
            }

            IQueryable result = null;

            // Get queryable path builder to builder
            var queryable = GetQuery(path);
            ETag etag;

            // TODO #365 Do not support additional path segment after function call now
            if (lastSegment is OperationImportSegment unboundSegment)
            {
                var operation = unboundSegment.OperationImports.FirstOrDefault();
                Func<string, object> getParaValueFunc = p => unboundSegment.Parameters.FirstOrDefault(c => c.Name == p).Value;
                result = await ExecuteOperationAsync(getParaValueFunc, operation.Name, true, null, cancellationToken).ConfigureAwait(false);

                var applied = ApplyQueryOptions(result, path, true);
                result = applied.Queryable;
                etag = applied.Etag;
            }
            else
            {
                if (queryable is null)
                {
                    return NotFound(Resources.ResourceNotFound);
                }

                if (lastSegment is OperationSegment)
                {
                    result = await ExecuteQuery(queryable, cancellationToken).ConfigureAwait(false);

                    var boundSeg = (OperationSegment)lastSegment;
                    var operation = boundSeg.Operations.FirstOrDefault();
                    Func<string, object> getParaValueFunc = p => boundSeg.Parameters.FirstOrDefault(c => c.Name == p).Value;
                    result = await ExecuteOperationAsync(getParaValueFunc, operation.Name, true, result, cancellationToken).ConfigureAwait(false);

                    var applied = ApplyQueryOptions(result, path, true);
                    result = applied.Queryable;
                    etag = applied.Etag;
                }
                else
                {
                    var applied = ApplyQueryOptions(queryable, path, false);
                    result = await ExecuteQuery(applied.Queryable, cancellationToken).ConfigureAwait(false);
                    etag = applied.Etag;
                }
            }

            return CreateQueryResponse(result, path.EdmType, etag);
        }

        /// <summary>
        /// Handles a POST request to create an entity.
        /// </summary>
        /// <param name="edmEntityObject">The entity object to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the creation result.</returns>
        public async Task<IActionResult> Post(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            if (edmEntityObject is null)
            {
                throw new ODataException("A POST requires an object to be present in the request body.");
            }

            EnsureInitialized();

            CheckModelState();
            var path = GetPath();
            if (path.NavigationSource is not IEdmEntitySet entitySet)
            {
                throw new NotImplementedException(Resources.InsertOnlySupportedOnEntitySet);
            }

            // In case of type inheritance, the actual type will be different from entity type
            var expectedEntityType = path.EdmType;
            var actualEntityType = path.EdmType as IEdmStructuredType;
            if (edmEntityObject.ActualEdmType is not null)
            {
                expectedEntityType = edmEntityObject.ExpectedEdmType;
                actualEntityType = edmEntityObject.ActualEdmType;
            }

            var model = api.GetModel();

            var postItem = new DataModificationItem(
                entitySet.Name,
                expectedEntityType.GetClrType(model),
                actualEntityType.GetClrType(model),
                RestierEntitySetOperation.Insert,
                null,
                null,
                edmEntityObject.CreatePropertyDictionary(actualEntityType, api, true));

            var changeSetProperty = HttpContext.GetChangeSet();
            if (changeSetProperty is null)
            {
                var changeSet = new ChangeSet();
                changeSet.Entries.Add(postItem);

                var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(postItem);

                await changeSetProperty.OnChangeSetCompleted().ConfigureAwait(false);
            }

            return CreateCreatedODataResult(postItem.Resource);
        }

        /// <summary>
        /// Handles a PUT request to fully update an entity.
        /// </summary>
        /// <param name="edmEntityObject">The entity object to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the updated result.</returns>
        public async Task<IActionResult> Put(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
            => await Update(edmEntityObject, true, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Handles a PATCH request to partially update an entity.
        /// </summary>
        /// <param name="edmEntityObject">The entity object to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the updated result.</returns>
        public async Task<IActionResult> Patch(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
            => await Update(edmEntityObject, false, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Handles a DELETE request to delete an entity.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the deletion result.</returns>
        public async Task<IActionResult> Delete(CancellationToken cancellationToken)
        {
            EnsureInitialized();
            var path = GetPath();
            if (!(path.NavigationSource is IEdmEntitySet entitySet))
            {
                throw new NotImplementedException(Resources.DeleteOnlySupportedOnEntitySet);
            }

            var propertiesInEtag = GetOriginalValues(entitySet);
            if (propertiesInEtag is null)
            {
                throw new StatusCodeException((HttpStatusCode)428, Resources.PreconditionRequired);
            }

            var model = api.GetModel();

            var deleteItem = new DataModificationItem(
                entitySet.Name,
                path.EdmType.GetClrType(model),
                null,
                RestierEntitySetOperation.Delete,
                RestierQueryBuilder.GetPathKeyValues(path),
                propertiesInEtag,
                null);

            var changeSetProperty = HttpContext.GetChangeSet();
            if (changeSetProperty is null)
            {
                var changeSet = new ChangeSet();
                changeSet.Entries.Add(deleteItem);

                var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(deleteItem);

                await changeSetProperty.OnChangeSetCompleted().ConfigureAwait(false);
            }

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Handles a POST request to an action.
        /// </summary>
        /// <param name="parameters">Parameters from action request content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the action result.</returns>
        public async Task<IActionResult> PostAction(ODataActionParameters parameters, CancellationToken cancellationToken)
        {
            EnsureInitialized();
            CheckModelState();
            var path = GetPath();

            var lastSegment = path.Segments.LastOrDefault();
            if (lastSegment is null)
            {
                throw new InvalidOperationException(Resources.ControllerRequiresPath);
            }

            IQueryable result = null;
            object GetParaValueFunc(string p)
            {
                if (parameters is null)
                {
                    return null;
                }

                if (!parameters.ContainsKey(p))
                {
                    throw new NullReferenceException($"The key {p} was not found in the parameters the ASP.NET Core ModelBinder retrieved from the POST body.");
                }

                return parameters[p];
            }

            if (lastSegment is OperationImportSegment segment)
            {
                var unboundSegment = segment;
                var operation = unboundSegment.OperationImports.FirstOrDefault();
                result = await ExecuteOperationAsync(GetParaValueFunc, operation.Name, false, null, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Get queryable path builder to builder
                var queryable = GetQuery(path);
                if (queryable is null)
                {
                    return NotFound(Resources.ResourceNotFound);
                }

                if (lastSegment is OperationSegment)
                {
                    var operationSegment = lastSegment as OperationSegment;
                    var operation = operationSegment.Operations.FirstOrDefault();
                    var queryResult = await ExecuteQuery(queryable, cancellationToken).ConfigureAwait(false);
                    result = await ExecuteOperationAsync(GetParaValueFunc, operation.Name, false, queryResult, cancellationToken).ConfigureAwait(false);
                }
            }

            if (path.EdmType is null)
            {
                // This is a void action, return 204 directly
                return StatusCode((int)HttpStatusCode.NoContent);
            }

            return CreateQueryResponse(result, path.EdmType, null);
        }

        private static IEdmTypeReference GetTypeReference(IEdmType edmType)
        {
            Ensure.NotNull(edmType, nameof(edmType));

            var isNullable = false;
            switch (edmType.TypeKind)
            {
                case EdmTypeKind.Collection:
                    return new EdmCollectionTypeReference(edmType as IEdmCollectionType);
                case EdmTypeKind.Complex:
                    return new EdmComplexTypeReference(edmType as IEdmComplexType, isNullable);
                case EdmTypeKind.Entity:
                    return new EdmEntityTypeReference(edmType as IEdmEntityType, isNullable);
                case EdmTypeKind.EntityReference:
                    return new EdmEntityReferenceTypeReference(edmType as IEdmEntityReferenceType, isNullable);
                case EdmTypeKind.Enum:
                    return new EdmEnumTypeReference(edmType as IEdmEnumType, isNullable);
                case EdmTypeKind.Primitive:
                    return new EdmPrimitiveTypeReference(edmType as IEdmPrimitiveType, isNullable);
                default:
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.EdmTypeNotSupported, edmType.ToTraceString()));
            }
        }

        private async Task<IActionResult> Update(
            EdmEntityObject edmEntityObject,
            bool isFullReplaceUpdate,
            CancellationToken cancellationToken)
        {
            EnsureInitialized();
            CheckModelState();
            var path = GetPath();
            var entitySet = path.NavigationSource as IEdmEntitySet;
            if (entitySet is null)
            {
                throw new NotImplementedException(Resources.UpdateOnlySupportedOnEntitySet);
            }

            var propertiesInEtag = GetOriginalValues(entitySet);
            if (propertiesInEtag is null)
            {
                throw new StatusCodeException((HttpStatusCode)428, Resources.PreconditionRequired);
            }

            // In case of type inheritance, the actual type will be different from entity type
            // This is only needed for put case, and does not need for patch case
            // For put request, it will create a new, blank instance of the entity.
            // copy over the key values and set any updated values from the client on the new instance.
            // Then apply all the properties of the new instance to the instance to be updated.
            // This will set any unspecified properties to their default value.
            var expectedEntityType = path.EdmType;
            var actualEntityType = path.EdmType as IEdmStructuredType;
            if (edmEntityObject.ActualEdmType is not null)
            {
                expectedEntityType = edmEntityObject.ExpectedEdmType;
                actualEntityType = edmEntityObject.ActualEdmType;
            }

            var model = api.GetModel();

            var updateItem = new DataModificationItem(
                entitySet.Name,
                expectedEntityType.GetClrType(model),
                actualEntityType.GetClrType(model),
                RestierEntitySetOperation.Update,
                RestierQueryBuilder.GetPathKeyValues(path),
                propertiesInEtag,
                edmEntityObject.CreatePropertyDictionary(actualEntityType, api, false))
            {
                IsFullReplaceUpdateRequest = isFullReplaceUpdate,
            };

            var changeSetProperty = HttpContext.GetChangeSet();
            if (changeSetProperty is null)
            {
                var changeSet = new ChangeSet();
                changeSet.Entries.Add(updateItem);

                // RWM: Seems like we should be using the result here. For something else.
                var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(updateItem);

                await changeSetProperty.OnChangeSetCompleted().ConfigureAwait(false);
            }

            return CreateUpdatedODataResult(updateItem.Resource);
        }

        private IActionResult CreateQueryResponse(IQueryable query, IEdmType edmType, ETag etag)
        {
            var typeReference = GetTypeReference(edmType);
            BaseSingleResult singleResult = null;
            IActionResult response = null;

            if (typeReference.IsPrimitive())
            {
                if (shouldReturnCount || shouldWriteRawValue)
                {
                    var rawResult = new RawResult(query, typeReference);
                    singleResult = rawResult;
                    response = Ok(rawResult);
                }
                else
                {
                    var primitiveResult = new PrimitiveResult(query, typeReference);
                    singleResult = primitiveResult;
                    response = Ok(primitiveResult);
                }
            }

            if (typeReference.IsComplex())
            {
                var complexResult = new ComplexResult(query, typeReference);
                singleResult = complexResult;
                response = Ok(complexResult);
            }

            if (typeReference.IsEnum())
            {
                if (shouldWriteRawValue)
                {
                    var rawResult = new RawResult(query, typeReference);
                    singleResult = rawResult;
                    response = Ok(rawResult);
                }
                else
                {
                    var enumResult = new EnumResult(query, typeReference);
                    singleResult = enumResult;
                    response = Ok(enumResult);
                }
            }

            if (singleResult is not null)
            {
                if (singleResult.Result is null)
                {
                    // Per specification, If the property is single-valued and has the null value,
                    // the service responds with 204 No Content.
                    return NoContent();
                }

                return response;
            }

            if (typeReference.IsCollection())
            {
                var elementType = typeReference.AsCollection().ElementType();
                if (elementType.IsPrimitive() || elementType.IsEnum())
                {
                    return Ok(new NonResourceCollectionResult(query, typeReference));
                }

                return Ok(new ResourceSetResult(query, typeReference));
            }

            var entityResult = query.SingleOrDefault();
            if (entityResult is null)
            {
                return NoContent();
            }

            // Check the ETag here
            if (etag is not null)
            {
                // request with If-Match header, if match, then should return whole content
                // request with If-Match header, if not match, then should return 412
                // request with If-None-Match header, if match, then should return 304
                // request with If-None-Match header, if not match, then should return whole content
                etag.EntityType = query.ElementType;
                query = etag.ApplyTo(query);
                entityResult = query.SingleOrDefault();
                if (entityResult is null && !etag.IsIfNoneMatch)
                {
                    return StatusCode((int)HttpStatusCode.PreconditionFailed);
                }
                else if (entityResult is null)
                {
                    return StatusCode((int)HttpStatusCode.NotModified);
                }
            }

            return Ok(entityResult);
        }

        private IQueryable GetQuery(ODataPath path)
        {
            var builder = new RestierQueryBuilder(api, path);
            var queryable = builder.BuildQuery();
            shouldReturnCount = builder.IsCountPathSegmentPresent;
            shouldWriteRawValue = builder.IsValuePathSegmentPresent;

            return queryable;
        }

        private (IQueryable Queryable, ETag Etag) ApplyQueryOptions(IQueryable queryable, ODataPath path, bool applyCount)
        {
            ETag etag = null;

            if (shouldWriteRawValue)
            {
                // Query options don't apply to $value.
                return (queryable, null);
            }

            var feature = HttpContext.ODataFeature();
            var model = api.GetModel();
            var queryContext = new ODataQueryContext(model, queryable.ElementType, path);
            var queryOptions = new ODataQueryOptions(queryContext, Request);

            // Get etag for query request
            if (queryOptions.IfMatch is not null)
            {
                etag = queryOptions.IfMatch;
            }
            else if (queryOptions.IfNoneMatch is not null)
            {
                etag = queryOptions.IfNoneMatch;
            }

            // TODO GitHubIssue#41 : Ensure stable ordering for query
            if (shouldReturnCount)
            {
                // Query options other than $filter and $search don't apply to $count.
                queryable = queryOptions.ApplyTo(queryable, querySettings, AllowedQueryOptions.All ^ AllowedQueryOptions.Filter);
                return (queryable, etag);
            }

            if (queryOptions.Count is not null && !applyCount)
            {
                var queryExecutorOptions = api.GetApiService<RestierQueryExecutorOptions>();
                queryExecutorOptions.IncludeTotalCount = queryOptions.Count.Value;
                queryExecutorOptions.SetTotalCount = value => feature.TotalCount = value;
            }

            // Validate query before apply, and query setting like MaxExpansionDepth can be customized here
            queryOptions.Validate(validationSettings);

            // Entity count can NOT be evaluated at this point of time because the source
            // expression is just a placeholder to be replaced by the expression sourcer.
            if (!applyCount)
            {
                queryable = queryOptions.ApplyTo(queryable, querySettings, AllowedQueryOptions.Count);
            }
            else
            {
                queryable = queryOptions.ApplyTo(queryable, querySettings);
            }

            return (queryable, etag);
        }

        private async Task<IQueryable> ExecuteQuery(IQueryable queryable, CancellationToken cancellationToken)
        {
            var queryRequest = new QueryRequest(queryable)
            {
                ShouldReturnCount = shouldReturnCount,
            };

            var queryResult = await api.QueryAsync(queryRequest, cancellationToken).ConfigureAwait(false);
            var result = queryResult.Results.AsQueryable();
            return result;
        }

        private ODataPath GetPath()
        {
            var properties = HttpContext.ODataFeature();
            if (properties is null)
            {
                throw new InvalidOperationException(Resources.InvalidODataInfoInRequest);
            }

            var path = properties.Path;
            if (path is null)
            {
                throw new InvalidOperationException(Resources.InvalidEmptyPathInRequest);
            }

            return path;
        }

        private Task<IQueryable> ExecuteOperationAsync(
            Func<string, object> getParaValueFunc,
            string operationName,
            bool isFunction,
            IQueryable bindingParameterValue,
            CancellationToken cancellationToken)
        {
            var context = new RestierOperationContext(
                api,
                getParaValueFunc,
                operationName,
                isFunction,
                bindingParameterValue)
            {
                Request = Request,
            };
            var result = operationExecutor.ExecuteOperationAsync(context, cancellationToken);
            return result;
        }

        private IReadOnlyDictionary<string, object> GetOriginalValues(IEdmEntitySet entitySet)
        {
            var originalValues = new Dictionary<string, object>();

            if (Request.Headers.TryGetValue("IfMatch", out var ifMatchValues))
            {
                var etagHeaderValue = EntityTagHeaderValue.Parse(ifMatchValues.SingleOrDefault());
                var etag = Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(IfMatchKey, etagHeaderValue.Tag);
                return originalValues;
            }

            if (Request.Headers.TryGetValue("IfNoneMatch", out var ifNoneMatchValues))
            {
                var etagHeaderValue = EntityTagHeaderValue.Parse(ifNoneMatchValues.SingleOrDefault());
                var etag = Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(IfNoneMatchKey, etagHeaderValue.Tag);
                return originalValues;
            }

            // return 428(Precondition Required) if entity requires concurrency check.
            var model = api.GetModel();
            var needEtag = model.IsConcurrencyCheckEnabled(entitySet);
            if (needEtag)
            {
                return null;
            }

            return originalValues;
        }

        private static IActionResult CreateCreatedODataResult(object entity) => CreateResult(typeof(CreatedODataResult<>), entity);

        private static IActionResult CreateUpdatedODataResult(object entity) => CreateResult(typeof(UpdatedODataResult<>), entity);

        private static IActionResult CreateResult(Type resultType, object result)
        {
            var genericResultType = resultType.MakeGenericType(result.GetType());

            return (IActionResult)Activator.CreateInstance(genericResultType, result);
        }

        private void CheckModelState()
        {
            if (!ModelState.IsValid)
            {
                var errorList = (
                    from item in ModelState
                    where item.Value.Errors.Any()
                    select
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{{ Error: {0}, Exception {1} }}",
                            item.Value.Errors[0].ErrorMessage,
                            item.Value.Errors[0].Exception.Message)).ToList();

                throw new ODataException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.ModelStateIsNotValid,
                        string.Join(";", errorList)));
            }
        }

        private void EnsureInitialized()
        {
            var container = HttpContext.Request.GetRequestContainer();
            api = container.GetRequiredService<ApiBase>();
            querySettings = container.GetRequiredService<ODataQuerySettings>();
            validationSettings = container.GetRequiredService<ODataValidationSettings>();
            operationExecutor = container.GetRequiredService<IOperationExecutor>();
        }
    }
}
