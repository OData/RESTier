// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.AspNetCore.Operation;
using Microsoft.Restier.AspNetCore.Query;
using Microsoft.Restier.AspNetCore.Submit;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Restier.AspNetCore
{
    /// <summary>
    /// The all-in-one controller class to handle API requests.
    /// </summary>
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
        /// Handles a GET request for the OData $metadata document.
        /// </summary>
        /// <returns>The EDM model for the current route.</returns>
        public IActionResult GetMetadata()
        {
            var model = HttpContext.ODataFeature().Model;
            return Ok(model);
        }

        /// <summary>
        /// Handles a GET request for the OData service document.
        /// </summary>
        /// <returns>The OData service document for the current route.</returns>
        public IActionResult GetServiceDocument()
        {
            var model = HttpContext.ODataFeature().Model;
            return Ok(model);
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
            var lastSegment = path.LastOrDefault() ?? 
                throw new InvalidOperationException(Resources.ControllerRequiresPath);

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

                var queryRequest = new QueryRequest(result)
                {
                    ShouldReturnCount = shouldReturnCount,
                };

                etag = ApplyQueryOptions(queryRequest, path, true);
                result = queryRequest.Query;
            }
            else
            {
                if (queryable is null)
                {
                    return NotFound(Resources.ResourceNotFound);
                }

                if (lastSegment is OperationSegment segment)
                {
                    var queryRequest = new QueryRequest(queryable)
                    {
                        ShouldReturnCount = shouldReturnCount,
                    };

                    result = await ExecuteQuery(queryRequest, cancellationToken).ConfigureAwait(false);

                    var operation = segment.Operations.FirstOrDefault();
                    Func<string, object> getParaValueFunc = p => segment.Parameters.FirstOrDefault(c => c.Name == p).Value;
                    result = await ExecuteOperationAsync(getParaValueFunc, operation.Name, true, result, cancellationToken).ConfigureAwait(false);
                    queryRequest = new QueryRequest(result)
                    {
                        ShouldReturnCount = shouldReturnCount,
                    };
                    etag = ApplyQueryOptions(queryRequest, path, true);
                    result = queryRequest.Query;
                }
                else
                {
                    var queryRequest = new QueryRequest(queryable)
                    {
                        ShouldReturnCount = shouldReturnCount,
                    };
                    etag = ApplyQueryOptions(queryRequest, path, false);
                    result = await ExecuteQuery(queryRequest, cancellationToken).ConfigureAwait(false);
                }
            }

            return await CreateQueryResponse(result, path.GetEdmType(), etag, path, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles a POST request to create an entity.
        /// </summary>
        /// <param name="edmEntityObject">The entity object to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the creation result.</returns>
        public async Task<IActionResult> Post(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            var path = GetPath();
            var lastSegment = path.Last();

            // if the request is to a function or function import, return MethodNotAllowed
            if (lastSegment is OperationSegment operationSegment && 
                operationSegment.Operations.FirstOrDefault().IsFunction())
            {
                return MethodNotAllowed();
            }

            if (lastSegment is OperationImportSegment operationImportSegment && 
                operationImportSegment.OperationImports.FirstOrDefault().IsFunctionImport())
            {
                return MethodNotAllowed();
            }

            if (path.NavigationSource() is not IEdmEntitySet entitySet)
            {
                throw new NotImplementedException(Resources.InsertOnlySupportedOnEntitySet);
            }

            if (edmEntityObject is null)
            {
                var odataVersion = Request.Headers["OData-Version"].FirstOrDefault()?.Trim();
                if (string.Equals(odataVersion, "4.01", StringComparison.Ordinal))
                {
                    throw new ODataException(
                        "OData-Version 4.01 is not supported for deep operations. " +
                        "ASP.NET Core OData 9.x does not support untyped (EdmEntityObject) deserialization with 4.01. " +
                        "Remove the OData-Version header or use OData-Version: 4.0.");
                }

                throw new ODataException("A POST requires an object to be present in the request body.");
            }

            EnsureInitialized();
            CheckModelState();

            // In case of type inheritance, the actual type will be different from entity type
            var expectedEntityType = path.GetEdmType();
            var actualEntityType = path.GetEdmType() as IEdmStructuredType;
            if (edmEntityObject.ActualEdmType is not null)
            {
                expectedEntityType = edmEntityObject.ExpectedEdmType;
                actualEntityType = edmEntityObject.ActualEdmType;
            }

            var model = api.Model;

            var postItem = new DataModificationItem(
                entitySet.Name,
                expectedEntityType.GetClrType(model),
                actualEntityType.GetClrType(model),
                RestierEntitySetOperation.Insert,
                null,
                null,
                edmEntityObject.CreatePropertyDictionary(actualEntityType, api, true));

            // Extract nested entities for deep insert
            var deepSettings = HttpContext.Request.GetRouteServices().GetService<DeepOperationSettings>() ?? new DeepOperationSettings();
            if (deepSettings.MaxDepth > 0)
            {
                var extractor = new DeepOperationExtractor(model, api, deepSettings);
                extractor.ExtractNestedItems(edmEntityObject, actualEntityType, postItem, isCreation: true);
            }

            var changeSetProperty = HttpContext.GetChangeSet();
            if (changeSetProperty is null)
            {
                var changeSet = new ChangeSet();
                foreach (var item in postItem.FlattenDepthFirst())
                {
                    changeSet.Entries.Enqueue(item);
                }

                try
                {
                    // TODO: RWM: Feels like we should be doing something with this.
                    var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsRelationshipConstraintViolation(ex))
                {
                    return BadRequest($"A relationship constraint was violated: {ex.GetBaseException().Message}");
                }
            }
            else
            {
                foreach (var item in postItem.FlattenDepthFirst())
                {
                    changeSetProperty.ChangeSet.Entries.Enqueue(item);
                }

                await changeSetProperty.OnChangeSetCompleted().ConfigureAwait(false);
            }

            // OData 4.01 requires 201 responses to be expanded to at least the depth present
            // in the deep insert request. Setting SelectExpandClause on ODataFeature drives
            // the serializer to expand nested navigation properties in the response body.
            // Fix: child SelectExpandClause must be non-null (empty clause instead of null)
            // to avoid NullReferenceException in SelectedPropertiesNode.Create.
            var selectExpandClause = DeepOperationResponseBuilder.BuildSelectExpandClause(postItem, model, entitySet);
            if (selectExpandClause is not null)
            {
                HttpContext.ODataFeature().SelectExpandClause = selectExpandClause;
            }

            return CreateCreatedODataResult(postItem.Resource);
        }

        private IActionResult MethodNotAllowed()
        {
            //var response = new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
            //response.Content = new StringContent(String.Empty);
            //response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            //response.Content.Headers.Allow.Add("GET");

            HttpContext.Response.Headers.Append("Allow", "GET");
            return new StatusCodeResult(405);
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
            if (path.NavigationSource() is not IEdmEntitySet entitySet)
            {
                throw new NotImplementedException(Resources.DeleteOnlySupportedOnEntitySet);
            }

            var propertiesInEtag = GetOriginalValues(entitySet) ??
                throw new StatusCodeException((HttpStatusCode)428, Resources.PreconditionRequired);

            var model = api.Model;

            var deleteItem = new DataModificationItem(
                entitySet.Name,
                path.GetEdmType().GetClrType(model),
                null,
                RestierEntitySetOperation.Delete,
                RestierQueryBuilder.GetPathKeyValues(path, model),
                propertiesInEtag,
                null);

            var changeSetProperty = HttpContext.GetChangeSet();
            if (changeSetProperty is null)
            {
                var changeSet = new ChangeSet();
                changeSet.Entries.Enqueue(deleteItem);

                //RWM: Seems like we should be using the result here for something else.
                var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Enqueue(deleteItem);

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

            var lastSegment = path.LastOrDefault() ??
                throw new InvalidOperationException(Resources.ControllerRequiresPath);

            IQueryable result = null;
            object GetParaValueFunc(string p)
            {
                if (parameters is null)
                {
                    return null;
                }

                parameters.TryGetValue(p, out var parameter);
                return parameter;
            }

            if (lastSegment is OperationImportSegment segment)
            {
                var operation = segment.OperationImports.FirstOrDefault();
                result = await ExecuteOperationAsync(GetParaValueFunc, operation.Name, false, null, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Get queryable path builder to builder
                var queryable = GetQuery(path);

                var queryRequest = new QueryRequest(queryable)
                {
                    ShouldReturnCount = shouldReturnCount,
                };

                if (queryable is null)
                {
                    return NotFound(Resources.ResourceNotFound);
                }

                if (lastSegment is OperationSegment operationSegment)
                {
                    var operation = operationSegment.Operations.FirstOrDefault();
                    var queryResult = await ExecuteQuery(queryRequest, cancellationToken).ConfigureAwait(false);
                    result = await ExecuteOperationAsync(GetParaValueFunc, operation.Name, false, queryResult, cancellationToken).ConfigureAwait(false);
                }
            }

            if (path.GetEdmType() is null)
            {
                // This is a void action, return 204 directly
                Trace.TraceWarning($"The operation '{path}' did not return a type. Sending a 204 status code instead.");
                return StatusCode((int)HttpStatusCode.NoContent);
            }

            return await CreateQueryResponse(result, path.GetEdmType(), null, path, cancellationToken).ConfigureAwait(false);
        }

        private static IEdmTypeReference GetTypeReference(IEdmType edmType)
        {
            Ensure.NotNull(edmType, nameof(edmType));

            var isNullable = false;
            return edmType.TypeKind switch
            {
                EdmTypeKind.Collection      => new EdmCollectionTypeReference(edmType as IEdmCollectionType),
                EdmTypeKind.Complex         => new EdmComplexTypeReference(edmType as IEdmComplexType, isNullable),
                EdmTypeKind.Entity          => new EdmEntityTypeReference(edmType as IEdmEntityType, isNullable),
                EdmTypeKind.EntityReference => new EdmEntityReferenceTypeReference(edmType as IEdmEntityReferenceType, isNullable),
                EdmTypeKind.Enum            => new EdmEnumTypeReference(edmType as IEdmEnumType, isNullable),
                EdmTypeKind.Primitive       => new EdmPrimitiveTypeReference(edmType as IEdmPrimitiveType, isNullable),
                _ => throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.EdmTypeNotSupported, edmType.ToTraceString())),
            };
        }

        private async Task<IActionResult> Update(
            EdmEntityObject edmEntityObject,
            bool isFullReplaceUpdate,
            CancellationToken cancellationToken)
        {
            var path = GetPath();
            var entitySet = path.NavigationSource() as IEdmEntitySet;
            if (entitySet is null)
            {
                throw new NotImplementedException(Resources.UpdateOnlySupportedOnEntitySet);
            }

            if (edmEntityObject is null)
            {
                var odataVersion = Request.Headers["OData-Version"].FirstOrDefault()?.Trim();
                if (string.Equals(odataVersion, "4.01", StringComparison.Ordinal))
                {
                    throw new ODataException(
                        "OData-Version 4.01 is not supported for deep operations. " +
                        "ASP.NET Core OData 9.x does not support untyped (EdmEntityObject) deserialization with 4.01. " +
                        "Remove the OData-Version header or use OData-Version: 4.0.");
                }

                throw new ODataException("An update requires an object to be present in the request body.");
            }

            EnsureInitialized();
            CheckModelState();

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
            var expectedEntityType = path.GetEdmType();
            var actualEntityType = path.GetEdmType() as IEdmStructuredType;
            if (edmEntityObject.ActualEdmType is not null)
            {
                expectedEntityType = edmEntityObject.ExpectedEdmType;
                actualEntityType = edmEntityObject.ActualEdmType;
            }

            var model = api.Model;

            var updateItem = new DataModificationItem(
                entitySet.Name,
                expectedEntityType.GetClrType(model),
                actualEntityType.GetClrType(model),
                RestierEntitySetOperation.Update,
                RestierQueryBuilder.GetPathKeyValues(path, model),
                propertiesInEtag,
                edmEntityObject.CreatePropertyDictionary(actualEntityType, api, false))
            {
                IsFullReplaceUpdateRequest = isFullReplaceUpdate,
            };

            // Extract nested entities for deep update
            var deepSettings = HttpContext.Request.GetRouteServices().GetService<DeepOperationSettings>() ?? new DeepOperationSettings();
            if (deepSettings.MaxDepth > 0)
            {
                var extractor = new DeepOperationExtractor(model, api, deepSettings);
                extractor.ExtractNestedItems(edmEntityObject, actualEntityType, updateItem, isCreation: false);
            }

            // Classify nested items (Insert vs Update, generate relationship removals)
            if (updateItem.NestedItems.Count > 0
                || updateItem.NullNavigationProperties.Count > 0
                || updateItem.NavigationBindings.Count > 0)
            {
                var classifier = new DeepUpdateClassifier(api, model);
                await classifier.ClassifyAsync(updateItem, entitySet, isFullReplaceUpdate, cancellationToken)
                    .ConfigureAwait(false);
            }

            var changeSetProperty = HttpContext.GetChangeSet();
            if (changeSetProperty is null)
            {
                var changeSet = new ChangeSet();
                foreach (var item in updateItem.FlattenDepthFirst())
                {
                    changeSet.Entries.Enqueue(item);
                }

                try
                {
                    var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsRelationshipConstraintViolation(ex))
                {
                    return BadRequest($"A relationship constraint was violated: {ex.GetBaseException().Message}");
                }
            }
            else
            {
                foreach (var item in updateItem.FlattenDepthFirst())
                {
                    changeSetProperty.ChangeSet.Entries.Enqueue(item);
                }

                await changeSetProperty.OnChangeSetCompleted().ConfigureAwait(false);
            }

            // Same response expansion as Post() — expand nested nav props in the 200/204 response.
            var selectExpandClause = DeepOperationResponseBuilder.BuildSelectExpandClause(updateItem, model, entitySet);
            if (selectExpandClause is not null)
            {
                HttpContext.ODataFeature().SelectExpandClause = selectExpandClause;
            }

            return CreateUpdatedODataResult(updateItem.Resource);
        }

        private async Task<IActionResult> CreateQueryResponse(IQueryable query, IEdmType edmType, ETag etag, ODataPath path, CancellationToken cancellationToken)
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
                    // Check if parent entity doesn't exist (404) vs property is null (204)
                    if (path.OfType<KeySegment>().Any())
                    {
                        var parentExists = await ParentEntityExistsAsync(path, cancellationToken).ConfigureAwait(false);
                        if (!parentExists)
                        {
                            return NotFound(Resources.ResourceNotFound);
                        }
                    }

                    // Per specification, If the property is single-valued and has the null value,
                    // the service responds with 204 No Content.
                    return NoContent();
                }

                return response;
            }

            // Opt-in OData v4 §11.2.6 strictness: when a collection-valued nav segment
            // sits below a key segment whose parent does not exist, the addressed
            // resource doesn't exist, so 404 is required by the spec. Off by default —
            // see RestierConformanceOptions.StrictMissingParentForCollections.
            if (typeReference.IsCollection() && path.OfType<KeySegment>().Any())
            {
                var conformance = HttpContext.Request.GetRouteServices()
                    .GetService<RestierConformanceOptions>();
                if (conformance?.StrictMissingParentForCollections == true)
                {
                    var parentExists = await ParentEntityExistsAsync(path, cancellationToken)
                        .ConfigureAwait(false);
                    if (!parentExists)
                    {
                        return NotFound(Resources.ResourceNotFound);
                    }
                }
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
                var lastSegment = path.LastOrDefault();
                var isKeyRequest = lastSegment is KeySegment
                    || (lastSegment is TypeSegment && path.Count >= 2 && path[path.Count - 2] is KeySegment);

                if (isKeyRequest)
                {
                    return NotFound(Resources.ResourceNotFound);
                }

                // Parent entity might not exist — check before returning 204
                if (path.OfType<KeySegment>().Any())
                {
                    var parentExists = await ParentEntityExistsAsync(path, cancellationToken).ConfigureAwait(false);
                    if (!parentExists)
                    {
                        return NotFound(Resources.ResourceNotFound);
                    }
                }

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

        private async Task<bool> ParentEntityExistsAsync(ODataPath fullPath, CancellationToken cancellationToken)
        {
            // Build a path through the last KeySegment (not the first). For nested paths
            // like /Publishers('P1')/Books(<missing-id>)/Title, the immediate keyed parent
            // is Books(<missing-id>), not Publishers('P1').
            var parentSegments = new List<ODataPathSegment>();
            var lastKeyIndex = -1;
            var index = 0;
            foreach (var segment in fullPath)
            {
                parentSegments.Add(segment);
                if (segment is KeySegment)
                {
                    lastKeyIndex = index;
                }

                index++;
            }

            if (lastKeyIndex >= 0)
            {
                parentSegments = parentSegments.GetRange(0, lastKeyIndex + 1);
            }

            var parentPath = new ODataPath(parentSegments);
            var filterBinder = HttpContext.Request.GetRouteServices().GetService<IFilterBinder>();
            var parentQuery = new RestierQueryBuilder(api, parentPath, querySettings, filterBinder).BuildQuery();
            if (parentQuery is null)
            {
                return false;
            }

            var queryRequest = new QueryRequest(parentQuery);
            var result = await api.QueryAsync(queryRequest, cancellationToken).ConfigureAwait(false);
            return result.Results.Cast<object>().Any();
        }

        private IQueryable GetQuery(ODataPath path)
        {
            var filterBinder = HttpContext.Request.GetRouteServices().GetService<IFilterBinder>();
            var builder = new RestierQueryBuilder(api, path, querySettings, filterBinder);
            var queryable = builder.BuildQuery();
            shouldReturnCount = builder.IsCountPathSegmentPresent;
            shouldWriteRawValue = builder.IsValuePathSegmentPresent;

            return queryable;
        }

        private ETag ApplyQueryOptions(QueryRequest queryRequest, ODataPath path, bool applyCount)
        {
            ETag etag = null;

            if (shouldWriteRawValue)
            {
                // Query options don't apply to $value.
                return null;
            }

            var feature = HttpContext.ODataFeature();
            var model = api.Model;
            var queryContext = new ODataQueryContext(model, queryRequest.Query.ElementType, path);
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
                queryRequest.Query = queryOptions.ApplyTo(queryRequest.Query, querySettings, AllowedQueryOptions.All ^ AllowedQueryOptions.Filter);
                return etag;
            }

            if (queryOptions.Count is not null && !applyCount)
            {
                queryRequest.IncludeTotalCount = queryOptions.Count.Value;
                queryRequest.SetTotalCount = value => feature.TotalCount = value;
            }

            // Validate query before apply, and query setting like MaxExpansionDepth can be customized here
            queryOptions.Validate(validationSettings);

            // Entity count can NOT be evaluated at this point of time because the source
            // expression is just a placeholder to be replaced by the expression sourcer.
            if (!applyCount)
            {
                queryRequest.Query = queryOptions.ApplyTo(queryRequest.Query, querySettings, AllowedQueryOptions.Count);
            }
            else
            {
                queryRequest.Query = queryOptions.ApplyTo(queryRequest.Query, querySettings);
            }

            return etag;
        }

        private async Task<IQueryable> ExecuteQuery(QueryRequest queryRequest, CancellationToken cancellationToken)
        {
            var queryResult = await api.QueryAsync(queryRequest, cancellationToken).ConfigureAwait(false);
            var result = queryResult.Results.AsQueryable();
            return result;
        }

        private ODataPath GetPath()
        {
            var properties = HttpContext.ODataFeature() ??
                throw new InvalidOperationException(Resources.InvalidODataInfoInRequest);

            return properties.Path ?? 
                throw new InvalidOperationException(Resources.InvalidEmptyPathInRequest);
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
            return operationExecutor.ExecuteOperationAsync(context, cancellationToken);
        }

        private IReadOnlyDictionary<string, object> GetOriginalValues(IEdmEntitySet entitySet)
        {
            var originalValues = new Dictionary<string, object>();

            if (Request.Headers.TryGetValue("If-Match", out var ifMatchValues)
                || Request.Headers.TryGetValue("IfMatch", out ifMatchValues))
            {
                var etagHeaderValue = EntityTagHeaderValue.Parse(ifMatchValues.SingleOrDefault());

                // Wildcard ETag (*) means "any version" — satisfy the precondition requirement
                // but skip concurrency validation downstream.
                if (etagHeaderValue == EntityTagHeaderValue.Any)
                {
                    return originalValues;
                }

                var etag = Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(IfMatchKey, etagHeaderValue.Tag);
                return NormalizePropertyNames(originalValues, entitySet.EntityType, api.Model);
            }

            if (Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatchValues)
                || Request.Headers.TryGetValue("IfNoneMatch", out ifNoneMatchValues))
            {
                var etagHeaderValue = EntityTagHeaderValue.Parse(ifNoneMatchValues.SingleOrDefault());
                var etag = Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(IfNoneMatchKey, etagHeaderValue.Tag);
                return NormalizePropertyNames(originalValues, entitySet.EntityType, api.Model);
            }

            // return 428(Precondition Required) if entity requires concurrency check.
            var model = api.Model;
            if (model.IsConcurrencyCheckEnabled(entitySet))
            {
                return null;
            }

            return originalValues;
        }

        private static IReadOnlyDictionary<string, object> NormalizePropertyNames(
            Dictionary<string, object> values, IEdmStructuredType edmType, IEdmModel model)
        {
            var normalized = new Dictionary<string, object>(values.Count);
            foreach (var kvp in values)
            {
                if (kvp.Key.StartsWith("@", StringComparison.Ordinal))
                {
                    // Preserve internal keys like @IfMatchKey, @IfNoneMatchKey
                    normalized.Add(kvp.Key, kvp.Value);
                    continue;
                }

                var edmProperty = edmType.FindProperty(kvp.Key);
                var clrName = edmProperty is not null
                    ? EdmClrPropertyMapper.GetClrPropertyName(edmProperty, model)
                    : kvp.Key;
                normalized.Add(clrName, kvp.Value);
            }

            return normalized;
        }

        private static IActionResult CreateCreatedODataResult(object entity) => CreateResult(typeof(CreatedODataResult<>), entity);

        private static IActionResult CreateUpdatedODataResult(object entity) => CreateResult(typeof(UpdatedODataResult<>), entity);

        private static IActionResult CreateResult(Type resultType, object result)
        {
            var genericResultType = resultType.MakeGenericType(result.GetType());

            return (IActionResult)Activator.CreateInstance(genericResultType, result);
        }

        private static bool IsRelationshipConstraintViolation(Exception ex)
        {
            // Walk the exception chain to find constraint violation indicators
            var current = ex;
            while (current is not null)
            {
                var message = current.Message;
                if (message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("referential integrity", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
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
                            item.Value.Errors[0].Exception?.Message)).ToList();

                throw new ODataException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.ModelStateIsNotValid,
                        string.Join(";", errorList)));
            }
        }

        private void EnsureInitialized()
        {
            var container = HttpContext.Request.GetRouteServices();
            api = container.GetRequiredService<ApiBase>();
            querySettings = container.GetRequiredService<ODataQuerySettings>();
            validationSettings = container.GetRequiredService<ODataValidationSettings>();
            operationExecutor = container.GetRequiredService<IOperationExecutor>();
        }
    }
}
