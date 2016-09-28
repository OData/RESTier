// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
extern alias Net;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Formatter;
using System.Web.OData.Query;
using System.Web.OData.Results;
using System.Web.OData.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Publishers.OData.Batch;
using Microsoft.Restier.Publishers.OData.Model;
using Microsoft.Restier.Publishers.OData.Properties;
using Microsoft.Restier.Publishers.OData.Query;

// This is a must for creating response with correct extension method
using Net::System.Net.Http;
using ODataPath = System.Web.OData.Routing.ODataPath;

namespace Microsoft.Restier.Publishers.OData
{
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
        private bool shouldReturnCount;
        private bool shouldWriteRawValue;

        /// <summary>
        /// Gets the API associated with this controller.
        /// </summary>
        private ApiBase Api
        {
            get
            {
                if (this.api == null)
                {
                    var provider = Request.GetRequestContainer();
                    this.api = provider.GetService<ApiBase>();
                }

                return this.api;
            }
        }

        /// <summary>
        /// Handles a GET request to query entities.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the response message.</returns>
        public async Task<HttpResponseMessage> Get(
            CancellationToken cancellationToken)
        {
            ODataPath path = this.GetPath();
            ODataPathSegment lastSegment = path.Segments.LastOrDefault();
            if (lastSegment == null)
            {
                throw new InvalidOperationException(Resources.ControllerRequiresPath);
            }

            IQueryable result = null;

            // Get queryable path builder to builder
            IQueryable queryable = this.GetQuery(path);
            ETag etag;

            // TODO #365 Do not support additional path segment after function call now
            if (lastSegment is OperationImportSegment)
            {
                var unboundSegment = (OperationImportSegment)lastSegment;
                var operation = unboundSegment.OperationImports.FirstOrDefault();
                Func<string, object> getParaValueFunc = p => unboundSegment.GetParameterValue(p);
                result = await ExecuteOperationAsync(
                    getParaValueFunc, operation.Name, true, null, cancellationToken);
                result = ApplyQueryOptions(result, path, true, out etag);
            }
            else
            {
                if (queryable == null)
                {
                    throw new HttpResponseException(
                        this.Request.CreateErrorResponse(
                            HttpStatusCode.NotFound,
                            Resources.ResourceNotFound));
                }

                if (lastSegment is OperationSegment)
                {
                    result = await ExecuteQuery(queryable, cancellationToken);

                    var boundSeg = (OperationSegment)lastSegment;
                    var operation = boundSeg.Operations.FirstOrDefault();
                    Func<string, object> getParaValueFunc = p => boundSeg.GetParameterValue(p);
                    result = await ExecuteOperationAsync(
                        getParaValueFunc, operation.Name, true, result, cancellationToken);

                    result = ApplyQueryOptions(result, path, true, out etag);
                }
                else
                {
                    queryable = ApplyQueryOptions(queryable, path, false, out etag);
                    result = await ExecuteQuery(queryable, cancellationToken);
                }
            }

            return this.CreateQueryResponse(result, path.EdmType, etag);
        }

        /// <summary>
        /// Handles a POST request to create an entity.
        /// </summary>
        /// <param name="edmEntityObject">The entity object to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the creation result.</returns>
        public async Task<IHttpActionResult> Post(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            CheckModelState();
            ODataPath path = this.GetPath();
            IEdmEntitySet entitySet = path.NavigationSource as IEdmEntitySet;
            if (entitySet == null)
            {
                throw new NotImplementedException(Resources.InsertOnlySupportedOnEntitySet);
            }

            // In case of type inheritance, the actual type will be different from entity type
            var expectedEntityType = path.EdmType;
            var actualEntityType = path.EdmType as IEdmStructuredType;
            if (edmEntityObject.ActualEdmType != null)
            {
                expectedEntityType = edmEntityObject.ExpectedEdmType;
                actualEntityType = edmEntityObject.ActualEdmType;
            }

            DataModificationItem postItem = new DataModificationItem(
                entitySet.Name,
                expectedEntityType.GetClrType(Api.ServiceProvider),
                actualEntityType.GetClrType(Api.ServiceProvider),
                DataModificationItemAction.Insert,
                null,
                null,
                edmEntityObject.CreatePropertyDictionary(actualEntityType, api, true));

            RestierChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(postItem);

                SubmitResult result = await Api.SubmitAsync(changeSet, cancellationToken);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(postItem);

                await changeSetProperty.OnChangeSetCompleted(this.Request);
            }

            return this.CreateCreatedODataResult(postItem.Resource);
        }

        /// <summary>
        /// Handles a PUT request to fully update an entity.
        /// </summary>
        /// <param name="edmEntityObject">The entity object to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the updated result.</returns>
        public async Task<IHttpActionResult> Put(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            return await this.Update(edmEntityObject, true, cancellationToken);
        }

        /// <summary>
        /// Handles a PATCH request to partially update an entity.
        /// </summary>
        /// <param name="edmEntityObject">The entity object to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the updated result.</returns>
        public async Task<IHttpActionResult> Patch(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            return await this.Update(edmEntityObject, false, cancellationToken);
        }

        /// <summary>
        /// Handles a DELETE request to delete an entity.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the deletion result.</returns>
        public async Task<IHttpActionResult> Delete(CancellationToken cancellationToken)
        {
            ODataPath path = this.GetPath();
            IEdmEntitySet entitySet = path.NavigationSource as IEdmEntitySet;
            if (entitySet == null)
            {
                throw new NotImplementedException(Resources.DeleteOnlySupportedOnEntitySet);
            }

            var propertiesInEtag = await this.GetOriginalValues(entitySet);
            if (propertiesInEtag == null)
            {
                throw new PreconditionRequiredException(Resources.PreconditionRequired);
            }

            DataModificationItem deleteItem = new DataModificationItem(
                entitySet.Name,
                path.EdmType.GetClrType(Api.ServiceProvider),
                null,
                DataModificationItemAction.Remove,
                RestierQueryBuilder.GetPathKeyValues(path),
                propertiesInEtag,
                null);

            RestierChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(deleteItem);

                SubmitResult result = await Api.SubmitAsync(changeSet, cancellationToken);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(deleteItem);

                await changeSetProperty.OnChangeSetCompleted(this.Request);
            }

            return this.StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Handles a POST request to an action.
        /// </summary>
        /// <param name="parameters">Parameters from action request content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the action result.</returns>
        public async Task<HttpResponseMessage> PostAction(
            ODataActionParameters parameters, CancellationToken cancellationToken)
        {
            ODataPath path = this.GetPath();

            ODataPathSegment lastSegment = path.Segments.LastOrDefault();
            if (lastSegment == null)
            {
                throw new InvalidOperationException(Resources.ControllerRequiresPath);
            }

            IQueryable result = null;
            Func<string, object> getParaValueFunc = p =>
            {
                if (parameters == null)
                {
                    return null;
                }

                return parameters[p];
            };

            var segment = lastSegment as OperationImportSegment;
            if (segment != null)
            {
                var unboundSegment = segment;
                var operation = unboundSegment.OperationImports.FirstOrDefault();
                result = await ExecuteOperationAsync(
                    getParaValueFunc, operation.Name, false, null, cancellationToken);
            }
            else
            {
                // Get queryable path builder to builder
                IQueryable queryable = this.GetQuery(path);
                if (queryable == null)
                {
                    throw new HttpResponseException(
                        this.Request.CreateErrorResponse(
                            HttpStatusCode.NotFound,
                            Resources.ResourceNotFound));
                }

                if (lastSegment is OperationSegment)
                {
                    var operationSegment = lastSegment as OperationSegment;
                    var operation = operationSegment.Operations.FirstOrDefault();
                    var queryResult = await ExecuteQuery(queryable, cancellationToken);
                    result = await ExecuteOperationAsync(
                        getParaValueFunc, operation.Name, false, queryResult, cancellationToken);
                }
            }

            if (path.EdmType == null)
            {
                // This is a void action, return 204 directly
                return this.Request.CreateResponse(HttpStatusCode.NoContent);
            }

            return this.CreateQueryResponse(result, path.EdmType, null);
        }

        private static IEdmTypeReference GetTypeReference(IEdmType edmType)
        {
            Ensure.NotNull(edmType, "edmType");

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
                    throw Error.NotSupported(Resources.EdmTypeNotSupported, edmType.ToTraceString());
            }
        }

        private async Task<IHttpActionResult> Update(
            EdmEntityObject edmEntityObject,
            bool isFullReplaceUpdate,
            CancellationToken cancellationToken)
        {
            CheckModelState();
            ODataPath path = this.GetPath();
            IEdmEntitySet entitySet = path.NavigationSource as IEdmEntitySet;
            if (entitySet == null)
            {
                throw new NotImplementedException(Resources.UpdateOnlySupportedOnEntitySet);
            }

            var propertiesInEtag = await this.GetOriginalValues(entitySet);
            if (propertiesInEtag == null)
            {
                throw new PreconditionRequiredException(Resources.PreconditionRequired);
            }

            // In case of type inheritance, the actual type will be different from entity type
            // This is only needed for put case, and does not need for patch case
            // For put request, it will create a new, blank instance of the entity.
            // copy over the key values and set any updated values from the client on the new instance.
            // Then apply all the properties of the new instance to the instance to be updated.
            // This will set any unspecified properties to their default value.
            var expectedEntityType = path.EdmType;
            var actualEntityType = path.EdmType as IEdmStructuredType;
            if (edmEntityObject.ActualEdmType != null)
            {
                expectedEntityType = edmEntityObject.ExpectedEdmType;
                actualEntityType = edmEntityObject.ActualEdmType;
            }

            DataModificationItem updateItem = new DataModificationItem(
                entitySet.Name,
                expectedEntityType.GetClrType(Api.ServiceProvider),
                actualEntityType.GetClrType(Api.ServiceProvider),
                DataModificationItemAction.Update,
                RestierQueryBuilder.GetPathKeyValues(path),
                propertiesInEtag,
                edmEntityObject.CreatePropertyDictionary(actualEntityType, api, false));
            updateItem.IsFullReplaceUpdateRequest = isFullReplaceUpdate;

            RestierChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(updateItem);

                SubmitResult result = await Api.SubmitAsync(changeSet, cancellationToken);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(updateItem);

                await changeSetProperty.OnChangeSetCompleted(this.Request);
            }

            return this.CreateUpdatedODataResult(updateItem.Resource);
        }

        private HttpResponseMessage CreateQueryResponse(
            IQueryable query, IEdmType edmType, ETag etag)
        {
            IEdmTypeReference typeReference = GetTypeReference(edmType);
            BaseSingleResult singleResult = null;
            HttpResponseMessage response = null;

            if (typeReference.IsPrimitive())
            {
                if (this.shouldReturnCount || this.shouldWriteRawValue)
                {
                    var rawResult = new RawResult(query, typeReference);
                    singleResult = rawResult;
                    response = this.Request.CreateResponse(HttpStatusCode.OK, rawResult);
                }
                else
                {
                    var primitiveResult = new PrimitiveResult(query, typeReference);
                    singleResult = primitiveResult;
                    response = this.Request.CreateResponse(HttpStatusCode.OK, primitiveResult);
                }
            }

            if (typeReference.IsComplex())
            {
                var complexResult = new ComplexResult(query, typeReference);
                singleResult = complexResult;
                response = this.Request.CreateResponse(HttpStatusCode.OK, complexResult);
            }

            if (typeReference.IsEnum())
            {
                if (this.shouldWriteRawValue)
                {
                    var rawResult = new RawResult(query, typeReference);
                    singleResult = rawResult;
                    response = this.Request.CreateResponse(HttpStatusCode.OK, rawResult);
                }
                else
                {
                    var enumResult = new EnumResult(query, typeReference);
                    singleResult = enumResult;
                    response = this.Request.CreateResponse(HttpStatusCode.OK, enumResult);
                }
            }

            if (singleResult != null)
            {
                if (singleResult.Result == null)
                {
                    // Per specification, If the property is single-valued and has the null value,
                    // the service responds with 204 No Content.
                    return this.Request.CreateResponse(HttpStatusCode.NoContent);
                }

                return response;
            }

            if (typeReference.IsCollection())
            {
                var elementType = typeReference.AsCollection().ElementType();
                if (elementType.IsPrimitive() || elementType.IsEnum())
                {
                    return this.Request.CreateResponse(
                        HttpStatusCode.OK, new NonResourceCollectionResult(query, typeReference));
                }

                return this.Request.CreateResponse(
                    HttpStatusCode.OK, new ResourceSetResult(query, typeReference));
            }

            var entityResult = query.SingleOrDefault();
            if (entityResult == null)
            {
                return this.Request.CreateResponse(HttpStatusCode.NoContent);
            }

            // Check the ETag here
            if (etag != null)
            {
                // request with If-Match header, if match, then should return whole content
                // request with If-Match header, if not match, then should return 412
                // request with If-None-Match header, if match, then should return 304
                // request with If-None-Match header, if not match, then should return whole content
                etag.EntityType = query.ElementType;
                query = etag.ApplyTo(query);
                entityResult = query.SingleOrDefault();
                if (entityResult == null && !etag.IsIfNoneMatch)
                {
                    return this.Request.CreateResponse(HttpStatusCode.PreconditionFailed);
                }
                else if (entityResult == null)
                {
                    return this.Request.CreateResponse(HttpStatusCode.NotModified);
                }
            }

            // Using reflection to create response for single entity so passed in parameter is not object type,
            // but will be type of real entity type, then EtagMessageHandler can be used to set ETAG header
            // when response is single entity.
            // There are three HttpRequestMessageExtensions class defined in different assembles
            var genericMethod = typeof(System.Net.Http.HttpRequestMessageExtensions).GetMethods()
                .Where(m => m.Name == "CreateResponse" && m.GetParameters().Length == 3);
            var method = genericMethod.FirstOrDefault().MakeGenericMethod(query.ElementType);
            response = method.Invoke(null, new object[] { this.Request, HttpStatusCode.OK, entityResult })
                as HttpResponseMessage;
            return response;
        }

        private IQueryable GetQuery(ODataPath path)
        {
            RestierQueryBuilder builder = new RestierQueryBuilder(this.Api, path);
            IQueryable queryable = builder.BuildQuery();
            this.shouldReturnCount = builder.IsCountPathSegmentPresent;
            this.shouldWriteRawValue = builder.IsValuePathSegmentPresent;

            return queryable;
        }

        private IQueryable ApplyQueryOptions(
            IQueryable queryable, ODataPath path, bool applyCount, out ETag etag)
        {
            etag = null;

            if (this.shouldWriteRawValue)
            {
                // Query options don't apply to $value.
                return queryable;
            }

            HttpRequestMessageProperties properties = this.Request.ODataProperties();
            var model = Api.GetModelAsync().Result;
            ODataQueryContext queryContext =
                new ODataQueryContext(model, queryable.ElementType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, this.Request);

            // Get etag for query request
            if (queryOptions.IfMatch != null)
            {
                etag = queryOptions.IfMatch;
            }
            else if (queryOptions.IfNoneMatch != null)
            {
                etag = queryOptions.IfNoneMatch;
            }

            // TODO GitHubIssue#41 : Ensure stable ordering for query
            ODataQuerySettings settings = Api.GetApiService<ODataQuerySettings>();

            if (this.shouldReturnCount)
            {
                // Query options other than $filter and $search don't apply to $count.
                queryable = queryOptions.ApplyTo(
                    queryable, settings, AllowedQueryOptions.All ^ AllowedQueryOptions.Filter);
                return queryable;
            }

            if (queryOptions.Count != null && !applyCount)
            {
                RestierQueryExecutorOptions queryExecutorOptions =
                    Api.GetApiService<RestierQueryExecutorOptions>();
                queryExecutorOptions.IncludeTotalCount = queryOptions.Count.Value;
                queryExecutorOptions.SetTotalCount = value => properties.TotalCount = value;
            }

            // Validate query before apply, and query setting like MaxExpansionDepth can be customized here
            ODataValidationSettings validationSettings = Api.GetApiService<ODataValidationSettings>();
            queryOptions.Validate(validationSettings);

            // Entity count can NOT be evaluated at this point of time because the source
            // expression is just a placeholder to be replaced by the expression sourcer.
            if (!applyCount)
            {
                queryable = queryOptions.ApplyTo(queryable, settings, AllowedQueryOptions.Count);
            }
            else
            {
                queryable = queryOptions.ApplyTo(queryable, settings);
            }

            return queryable;
        }

        private async Task<IQueryable> ExecuteQuery(IQueryable queryable, CancellationToken cancellationToken)
        {
            QueryRequest queryRequest = new QueryRequest(queryable)
            {
                ShouldReturnCount = this.shouldReturnCount
            };

            QueryResult queryResult = await Api.QueryAsync(queryRequest, cancellationToken);
            var result = queryResult.Results.AsQueryable();
            return result;
        }

        private ODataPath GetPath()
        {
            HttpRequestMessageProperties properties = this.Request.ODataProperties();
            if (properties == null)
            {
                throw new InvalidOperationException(Resources.InvalidODataInfoInRequest);
            }

            ODataPath path = properties.Path;
            if (path == null)
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
            var executor = Api.GetApiService<IOperationExecutor>();

            var context = new OperationContext(
                getParaValueFunc,
                operationName,
                Api,
                isFunction,
                bindingParameterValue,
                Request.GetRequestContainer());

            context.Request = Request;
            var result = executor.ExecuteOperationAsync(context, cancellationToken);
            return result;
        }

        private async Task<IReadOnlyDictionary<string, object>> GetOriginalValues(IEdmEntitySet entitySet)
        {
            Dictionary<string, object> originalValues = new Dictionary<string, object>();

            EntityTagHeaderValue etagHeaderValue = this.Request.Headers.IfMatch.SingleOrDefault();
            if (etagHeaderValue != null)
            {
                ETag etag = this.Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(IfMatchKey, etagHeaderValue.Tag);
                return originalValues;
            }

            etagHeaderValue = this.Request.Headers.IfNoneMatch.SingleOrDefault();
            if (etagHeaderValue != null)
            {
                ETag etag = this.Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(IfNoneMatchKey, etagHeaderValue.Tag);
                return originalValues;
            }

            // return 428(Precondition Required) if entity requires concurrency check.
            var model = await this.Api.GetModelAsync();
            bool needEtag = model.IsConcurrencyCheckEnabled(entitySet);
            if (needEtag)
            {
                return null;
            }

            return originalValues;
        }

        private IHttpActionResult CreateCreatedODataResult(object entity)
        {
            return this.CreateResult(typeof(CreatedODataResult<>), entity);
        }

        private IHttpActionResult CreateUpdatedODataResult(object entity)
        {
            return this.CreateResult(typeof(UpdatedODataResult<>), entity);
        }

        private IHttpActionResult CreateResult(Type resultType, object result)
        {
            Type genericResultType = resultType.MakeGenericType(result.GetType());

            return (IHttpActionResult)Activator.CreateInstance(genericResultType, result, this);
        }

        private void CheckModelState()
        {
            if (!this.ModelState.IsValid)
            {
                var errorList = (
                    from item in this.ModelState
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
    }
}
