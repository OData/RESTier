// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Results;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.AspNet.Query;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
// This is a must for creating response with correct extension method
using ODataPath = Microsoft.AspNet.OData.Routing.ODataPath;


namespace Microsoft.Restier.AspNet
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

        private  ApiBase api;
        private ODataValidationSettings validationSettings;
        private IOperationExecutor operationExecutor;
        private ODataQuerySettings querySettings;

        private bool shouldReturnCount;
        private bool shouldWriteRawValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierController"/> class.
        /// </summary>
        /// <remarks>Please note that this controller needs a few dependencies
        /// to work correctly. The second constructor with arguments specifies those
        /// dependencies. When using the constructor without arguments, a DI container
        /// is requested from the HttpRequestMessage and the dependencies are
        /// resolved at run time. 
        /// It is better to use a DI framework and register RestierController yourself
        /// to allow the DI container to explicitly resolve dependencies at the start
        /// of your application.
        /// It is possible that the default constructor will be removed in the future.
        /// </remarks>
        public RestierController()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierController"/> class.
        /// </summary>
        /// <param name="api">A reference to an <see cref="ApiBase"/> class to use for filtering, authorization and querying.</param>
        /// <param name="querySettings">OData Query settings for queries.</param>
        /// <param name="validationSettings">OData validation settings for validation.</param>
        /// <param name="operationExecutor">An Operation Executer to execute operations.</param>
        public RestierController(ApiBase api, ODataQuerySettings querySettings, ODataValidationSettings validationSettings, IOperationExecutor operationExecutor)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(querySettings, nameof(querySettings));
            Ensure.NotNull(validationSettings, nameof(validationSettings));
            Ensure.NotNull(operationExecutor, nameof(operationExecutor));

            this.api = api;
            this.querySettings = querySettings;
            this.validationSettings = validationSettings;
            this.operationExecutor = operationExecutor;
        }

        /// <summary>
        /// Initializes the <see cref="ApiController"/> instance with the specified controllerContext.
        /// </summary>
        /// <remarks>
        /// Resolves the api, query settings, validation settings, operation executor from
        /// the Request container associated with the HttpRequestMessage.
        /// </remarks>
        /// <param name="controllerContext"> 
        /// The <see cref="HttpControllerContext"/> object that is used for the initialization.
        /// </param>
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);

            if (api != null && querySettings != null && validationSettings != null && operationExecutor != null)
            {
                return;
            }

            // TODO: JWS Either properly inject RestierController into the DI Container.
            // or provide sensible defaults for these dependencies to reduce DI dependency.
#pragma warning disable CA1062 // Validate arguments of public methods
            var provider = controllerContext.Request.GetRequestContainer();
#pragma warning restore CA1062 // Validate arguments of public methods

            if (api == null)
            {
                api = provider.GetService(typeof(ApiBase)) as ApiBase;
            }
            if (querySettings == null)
            {
                querySettings = provider.GetService(typeof(ODataQuerySettings)) as ODataQuerySettings;
            }
            if (validationSettings == null)
            {
                validationSettings = provider.GetService(typeof(ODataValidationSettings)) as ODataValidationSettings;
            }
            if (operationExecutor == null)
            {
                operationExecutor = provider.GetService(typeof(IOperationExecutor)) as IOperationExecutor;
            }

        }

        /// <summary>
        /// Handles a GET request to query entities.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the response message.</returns>
        public async Task<HttpResponseMessage> Get(CancellationToken cancellationToken)
        {
            var path = GetPath();
            var lastSegment = path.Segments.LastOrDefault();
            if (lastSegment == null)
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

                var applied = await ApplyQueryOptionsAsync(result, path, true).ConfigureAwait(false);
                result = applied.Queryable;
                etag = applied.Etag;
            }
            else
            {
                if (queryable == null)
                {
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, Resources.ResourceNotFound));
                }

                if (lastSegment is OperationSegment)
                {
                    result = await ExecuteQuery(queryable, cancellationToken).ConfigureAwait(false);

                    var boundSeg = (OperationSegment)lastSegment;
                    var operation = boundSeg.Operations.FirstOrDefault();
                    Func<string, object> getParaValueFunc = p => boundSeg.Parameters.FirstOrDefault(c => c.Name == p).Value;
                    result = await ExecuteOperationAsync(getParaValueFunc, operation.Name, true, result, cancellationToken).ConfigureAwait(false);

                    var applied = await ApplyQueryOptionsAsync(result, path, true).ConfigureAwait(false);
                    result = applied.Queryable;
                    etag = applied.Etag;

                }
                else
                {
                    var applied = await ApplyQueryOptionsAsync(queryable, path, false).ConfigureAwait(false);
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
        public async Task<IHttpActionResult> Post(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            if (edmEntityObject == null)
            {
                throw new ArgumentNullException(nameof(edmEntityObject));
            }

            CheckModelState();
            var path = GetPath();
            if (!(path.NavigationSource is IEdmEntitySet entitySet))
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

            var model = await api.GetModelAsync().ConfigureAwait(false);

            var postItem = new DataModificationItem(
                entitySet.Name,
                expectedEntityType.GetClrType(model),
                actualEntityType.GetClrType(model),
                RestierEntitySetOperation.Insert,
                null,
                null,
                edmEntityObject.CreatePropertyDictionary(actualEntityType, api, true));

            var changeSetProperty = Request.GetChangeSet();
            if (changeSetProperty == null)
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
#pragma warning disable CA1062 // Validate public arguments
        public async Task<IHttpActionResult> Put(EdmEntityObject edmEntityObject, CancellationToken cancellationToken) => await Update(edmEntityObject, true, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Handles a PATCH request to partially update an entity.
        /// </summary>
        /// <param name="edmEntityObject">The entity object to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the updated result.</returns>
        public async Task<IHttpActionResult> Patch(EdmEntityObject edmEntityObject, CancellationToken cancellationToken) => await Update(edmEntityObject, false, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1062 // Validate public arguments

        /// <summary>
        /// Handles a DELETE request to delete an entity.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the deletion result.</returns>
        public async Task<IHttpActionResult> Delete(CancellationToken cancellationToken)
        {
            var path = GetPath();
            if (!(path.NavigationSource is IEdmEntitySet entitySet))
            {
                throw new NotImplementedException(Resources.DeleteOnlySupportedOnEntitySet);
            }

            var propertiesInEtag = await GetOriginalValues(entitySet).ConfigureAwait(false);
            if (propertiesInEtag == null)
            {
                throw new StatusCodeException((HttpStatusCode)428, Resources.PreconditionRequired);
            }

            var model = await api.GetModelAsync().ConfigureAwait(false);

            var deleteItem = new DataModificationItem(
                entitySet.Name,
                path.EdmType.GetClrType(model),
                null,
                RestierEntitySetOperation.Delete,
                RestierQueryBuilder.GetPathKeyValues(path),
                propertiesInEtag,
                null);

            var changeSetProperty = Request.GetChangeSet();
            if (changeSetProperty == null)
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

            return StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Handles a POST request to an action.
        /// </summary>
        /// <param name="parameters">Parameters from action request content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the action result.</returns>
        public async Task<HttpResponseMessage> PostAction(ODataActionParameters parameters, CancellationToken cancellationToken)
        {
            CheckModelState();
            var path = GetPath();

            var lastSegment = path.Segments.LastOrDefault();
            if (lastSegment == null)
            {
                throw new InvalidOperationException(Resources.ControllerRequiresPath);
            }

            IQueryable result = null;
            object getParaValueFunc(string p)
            {
                if (parameters == null)
                {
                    return null;
                }

                return parameters[p];
            }

            if (lastSegment is OperationImportSegment segment)
            {
                var unboundSegment = segment;
                var operation = unboundSegment.OperationImports.FirstOrDefault();
                result = await ExecuteOperationAsync(getParaValueFunc, operation.Name, false, null, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Get queryable path builder to builder
                var queryable = GetQuery(path);
                if (queryable == null)
                {
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, Resources.ResourceNotFound));
                }

                if (lastSegment is OperationSegment)
                {
                    var operationSegment = lastSegment as OperationSegment;
                    var operation = operationSegment.Operations.FirstOrDefault();
                    var queryResult = await ExecuteQuery(queryable, cancellationToken).ConfigureAwait(false);
                    result = await ExecuteOperationAsync(getParaValueFunc, operation.Name, false, queryResult, cancellationToken).ConfigureAwait(false);
                }
            }

            if (path.EdmType == null)
            {
                // This is a void action, return 204 directly
                return Request.CreateResponse(HttpStatusCode.NoContent);
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
                    throw Error.NotSupported(Resources.EdmTypeNotSupported, edmType.ToTraceString());
            }
        }

        private async Task<IHttpActionResult> Update(
            EdmEntityObject edmEntityObject,
            bool isFullReplaceUpdate,
            CancellationToken cancellationToken)
        {
            CheckModelState();
            var path = GetPath();
            var entitySet = path.NavigationSource as IEdmEntitySet;
            if (entitySet == null)
            {
                throw new NotImplementedException(Resources.UpdateOnlySupportedOnEntitySet);
            }

            var propertiesInEtag = await GetOriginalValues(entitySet).ConfigureAwait(false);
            if (propertiesInEtag == null)
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
            if (edmEntityObject.ActualEdmType != null)
            {
                expectedEntityType = edmEntityObject.ExpectedEdmType;
                actualEntityType = edmEntityObject.ActualEdmType;
            }

            var model = await api.GetModelAsync().ConfigureAwait(false);

            var updateItem = new DataModificationItem(
                entitySet.Name,
                expectedEntityType.GetClrType(model),
                actualEntityType.GetClrType(model),
                RestierEntitySetOperation.Update,
                RestierQueryBuilder.GetPathKeyValues(path),
                propertiesInEtag,
                edmEntityObject.CreatePropertyDictionary(actualEntityType, api, false))
            {
                IsFullReplaceUpdateRequest = isFullReplaceUpdate
            };

            var changeSetProperty = Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                var changeSet = new ChangeSet();
                changeSet.Entries.Add(updateItem);

                //RWM: Seems like we should be using the result here. For something else.
                var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(updateItem);

                await changeSetProperty.OnChangeSetCompleted().ConfigureAwait(false);
            }

            return CreateUpdatedODataResult(updateItem.Resource);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private HttpResponseMessage CreateQueryResponse(IQueryable query, IEdmType edmType, ETag etag)
        {
            var typeReference = GetTypeReference(edmType);
            BaseSingleResult singleResult = null;
            HttpResponseMessage response = null;

            if (typeReference.IsPrimitive())
            {
                if (shouldReturnCount || shouldWriteRawValue)
                {
                    var rawResult = new RawResult(query, typeReference);
                    singleResult = rawResult;
                    response = Request.CreateResponse(HttpStatusCode.OK, rawResult);
                }
                else
                {
                    var primitiveResult = new PrimitiveResult(query, typeReference);
                    singleResult = primitiveResult;
                    response = Request.CreateResponse(HttpStatusCode.OK, primitiveResult);
                }
            }

            if (typeReference.IsComplex())
            {
                var complexResult = new ComplexResult(query, typeReference);
                singleResult = complexResult;
                response = Request.CreateResponse(HttpStatusCode.OK, complexResult);
            }

            if (typeReference.IsEnum())
            {
                if (shouldWriteRawValue)
                {
                    var rawResult = new RawResult(query, typeReference);
                    singleResult = rawResult;
                    response = Request.CreateResponse(HttpStatusCode.OK, rawResult);
                }
                else
                {
                    var enumResult = new EnumResult(query, typeReference);
                    singleResult = enumResult;
                    response = Request.CreateResponse(HttpStatusCode.OK, enumResult);
                }
            }

            if (singleResult != null)
            {
                if (singleResult.Result == null)
                {
                    // Per specification, If the property is single-valued and has the null value,
                    // the service responds with 204 No Content.
                    return Request.CreateResponse(HttpStatusCode.NoContent);
                }

                return response;
            }

            if (typeReference.IsCollection())
            {
                var elementType = typeReference.AsCollection().ElementType();
                if (elementType.IsPrimitive() || elementType.IsEnum())
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new NonResourceCollectionResult(query, typeReference));
                }

                return Request.CreateResponse(HttpStatusCode.OK, new ResourceSetResult(query, typeReference));
            }

            var entityResult = query.SingleOrDefault();
            if (entityResult == null)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent);
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
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed);
                }
                else if (entityResult == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotModified);
                }
            }

            // Using reflection to create response for single entity so passed in parameter is not object type,
            // but will be type of real entity type, then EtagMessageHandler can be used to set ETAG header
            // when response is single entity.
            // There are three HttpRequestMessageExtensions class defined in different assembles

            // Fix by @xuzhg in PR #609.
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(AcceptVerbsAttribute));
            var type = assembly.GetType("System.Net.Http.HttpRequestMessageExtensions");
            var genericMethod = type.GetMethods()
                .Where(m => m.Name == "CreateResponse" && m.GetParameters().Length == 3);
            var method = genericMethod.FirstOrDefault().MakeGenericMethod(query.ElementType);
            response = method.Invoke(null, new object[] { Request, HttpStatusCode.OK, entityResult }) as HttpResponseMessage;
            return response;
        }

        private IQueryable GetQuery(ODataPath path)
        {
            var builder = new RestierQueryBuilder(api, path);
            var queryable = builder.BuildQuery();
            shouldReturnCount = builder.IsCountPathSegmentPresent;
            shouldWriteRawValue = builder.IsValuePathSegmentPresent;

            return queryable;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="path"></param>
        /// <param name="applyCount"></param>
        /// <returns></returns>
        private async Task<(IQueryable Queryable, ETag Etag)> ApplyQueryOptionsAsync(IQueryable queryable, ODataPath path, bool applyCount)
        {
            ETag etag = null;

            if (shouldWriteRawValue)
            {
                // Query options don't apply to $value.
                return (queryable, null);
            }

            var properties = Request.ODataProperties();
            var model = await api.GetModelAsync().ConfigureAwait(false);
            var queryContext = new ODataQueryContext(model, queryable.ElementType, path);
            var queryOptions = new ODataQueryOptions(queryContext, Request);

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
            
            if (shouldReturnCount)
            {
                // Query options other than $filter and $search don't apply to $count.
                queryable = queryOptions.ApplyTo(queryable, querySettings, AllowedQueryOptions.All ^ AllowedQueryOptions.Filter);
                return (queryable, etag);
            }

            if (queryOptions.Count != null && !applyCount)
            {
                var queryExecutorOptions = api.GetApiService<RestierQueryExecutorOptions>();
                queryExecutorOptions.IncludeTotalCount = queryOptions.Count.Value;
                queryExecutorOptions.SetTotalCount = value => properties.TotalCount = value;
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
                ShouldReturnCount = shouldReturnCount
            };

            var queryResult = await api.QueryAsync(queryRequest, cancellationToken).ConfigureAwait(false);
            var result = queryResult.Results.AsQueryable();
            return result;
        }

        private ODataPath GetPath()
        {
            var properties = Request.ODataProperties();
            if (properties == null)
            {
                throw new InvalidOperationException(Resources.InvalidODataInfoInRequest);
            }

            var path = properties.Path;
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

            var context = new OperationContext(api,
                getParaValueFunc,
                operationName,
                isFunction,
                bindingParameterValue)
            {
                Request = Request
            };
            var result = operationExecutor.ExecuteOperationAsync(context, cancellationToken);
            return result;
        }

        private async Task<IReadOnlyDictionary<string, object>> GetOriginalValues(IEdmEntitySet entitySet)
        {
            var originalValues = new Dictionary<string, object>();

            var etagHeaderValue = Request.Headers.IfMatch.SingleOrDefault();
            if (etagHeaderValue != null)
            {
                var etag = Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(IfMatchKey, etagHeaderValue.Tag);
                return originalValues;
            }

            etagHeaderValue = Request.Headers.IfNoneMatch.SingleOrDefault();
            if (etagHeaderValue != null)
            {
                var etag = Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(IfNoneMatchKey, etagHeaderValue.Tag);
                return originalValues;
            }

            // return 428(Precondition Required) if entity requires concurrency check.
            var model = await api.GetModelAsync().ConfigureAwait(false);
            var needEtag = model.IsConcurrencyCheckEnabled(entitySet);
            if (needEtag)
            {
                return null;
            }

            return originalValues;
        }

        private IHttpActionResult CreateCreatedODataResult(object entity) => CreateResult(typeof(CreatedODataResult<>), entity);

        private IHttpActionResult CreateUpdatedODataResult(object entity) => CreateResult(typeof(UpdatedODataResult<>), entity);

        private IHttpActionResult CreateResult(Type resultType, object result)
        {
            var genericResultType = resultType.MakeGenericType(result.GetType());

            return (IHttpActionResult)Activator.CreateInstance(genericResultType, result, this);
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
    }
}
