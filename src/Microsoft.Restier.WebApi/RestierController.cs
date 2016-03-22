// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Formatter;
using System.Web.OData.Query;
using System.Web.OData.Results;
using System.Web.OData.Routing;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.WebApi.Batch;
using Microsoft.Restier.WebApi.Filters;
using Microsoft.Restier.WebApi.Properties;
using Microsoft.Restier.WebApi.Query;
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi
{
    /// <summary>
    /// The all-in-one controller class to handle API requests.
    /// </summary>
    [RestierFormatting]
    [RestierExceptionFilter]
    public class RestierController : ODataController
    {
        private const string ETagGetterKey = "ETagGetter";
        private const string ETagHeaderKey = "@etag";

        private ApiBase api;
        private bool shouldReturnCount;
        private bool shouldWriteRawValue;

        /// <summary>
        /// Gets the API associated with this controller.
        /// </summary>
        public ApiBase Api
        {
            get
            {
                if (this.api == null)
                {
                    this.api = this.Request.GetApiFactory().Invoke();
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
            HttpRequestMessageProperties odataProperties = this.Request.ODataProperties();
            ODataPath path = odataProperties.Path;
            if (path == null)
            {
                throw new InvalidOperationException(Resources.ControllerRequiresPath);
            }

            IQueryable queryable = this.GetQuery();
            QueryRequest queryRequest = new QueryRequest(queryable)
            {
                ShouldReturnCount = this.shouldReturnCount
            };
            QueryResult queryResult = await Api.QueryAsync(queryRequest, cancellationToken);

            this.Request.Properties[ETagGetterKey] = this.Api.Context.GetProperty(ETagGetterKey);

            return this.CreateQueryResponse(queryResult.Results.AsQueryable(), path.EdmType);
        }

        /// <summary>
        /// Handles a POST request to create an entity.
        /// </summary>
        /// <param name="edmEntityObject">The entity object to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the creation result.</returns>
        public async Task<IHttpActionResult> Post(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return BadRequest(this.ModelState);
            }

            ODataPath path = this.GetPath();
            IEdmEntitySet entitySet = path.NavigationSource as IEdmEntitySet;
            if (entitySet == null)
            {
                throw new NotImplementedException(Resources.InsertOnlySupportedOnEntitySet);
            }

            DataModificationEntry postEntry = new DataModificationEntry(
                entitySet.Name,
                path.EdmType.FullTypeName(),
                null,
                null,
                edmEntityObject.CreatePropertyDictionary());

            RestierChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(postEntry);

                SubmitResult result = await Api.SubmitAsync(changeSet, cancellationToken);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(postEntry);

                await changeSetProperty.OnChangeSetCompleted();
            }

            return this.CreateCreatedODataResult(postEntry.Entity);
        }

        /// <summary>
        /// Handles a PUT request to fully update an entity.
        /// </summary>
        /// <param name="edmEntityObject">The entity object to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the updated result.</returns>
        public async Task<IHttpActionResult> Put(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                return BadRequest(this.ModelState);
            }

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
            if (!this.ModelState.IsValid)
            {
                return BadRequest(this.ModelState);
            }

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

            DataModificationEntry deleteEntry = new DataModificationEntry(
                entitySet.Name,
                path.EdmType.FullTypeName(),
                RestierQueryBuilder.GetPathKeyValues(path),
                this.GetOriginalValues(),
                null);

            RestierChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(deleteEntry);

                SubmitResult result = await Api.SubmitAsync(changeSet, cancellationToken);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(deleteEntry);

                await changeSetProperty.OnChangeSetCompleted();
            }

            return this.StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Handles a POST request to an action.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the action result.</returns>
        public async Task<IHttpActionResult> PostAction(CancellationToken cancellationToken)
        {
            ODataPath path = this.GetPath();
            UnboundActionPathSegment actionPathSegment = path.Segments.Last() as UnboundActionPathSegment;
            if (actionPathSegment == null)
            {
                throw new NotSupportedException(Resources.PostToUnboundActionNotSupported);
            }

            ActionInvocationEntry entry = new ActionInvocationEntry(actionPathSegment.ActionName, null);

            RestierChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(entry);

                SubmitResult result = await Api.SubmitAsync(changeSet, cancellationToken);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(entry);

                await changeSetProperty.OnChangeSetCompleted();
            }

            if (entry.Result != null)
            {
                return this.CreateOKResult(entry.Result);
            }
            else
            {
                // TODO: Should also be able to handle 204.
                return this.StatusCode(HttpStatusCode.NotImplemented);
            }
        }

        /// <summary>
        /// Disposes the API and the controller.
        /// </summary>
        /// <param name="disposing">Indicates whether disposing is happening.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.api != null)
                {
                    this.api.Dispose();
                }
            }

            base.Dispose(disposing);
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
            ODataPath path = this.GetPath();
            IEdmEntitySet entitySet = path.NavigationSource as IEdmEntitySet;
            if (entitySet == null)
            {
                throw new NotImplementedException(Resources.UpdateOnlySupportedOnEntitySet);
            }

            DataModificationEntry updateEntry = new DataModificationEntry(
                entitySet.Name,
                path.EdmType.FullTypeName(),
                RestierQueryBuilder.GetPathKeyValues(path),
                this.GetOriginalValues(),
                edmEntityObject.CreatePropertyDictionary());
            updateEntry.IsFullReplaceUpdate = isFullReplaceUpdate;

            RestierChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(updateEntry);

                SubmitResult result = await Api.SubmitAsync(changeSet, cancellationToken);
            }
            else
            {
                changeSetProperty.ChangeSet.Entries.Add(updateEntry);

                await changeSetProperty.OnChangeSetCompleted();
            }

            return this.CreateUpdatedODataResult(updateEntry.Entity);
        }

        private HttpResponseMessage CreateQueryResponse(IQueryable query, IEdmType edmType)
        {
            IEdmTypeReference typeReference = GetTypeReference(edmType);

            // TODO, GitHubIssue#328 : 404 should be returned when requesting property of non-exist entity
            BaseSingleResult singleResult = null;
            HttpResponseMessage response = null;

            if (typeReference.IsPrimitive())
            {
                if (this.shouldReturnCount || this.shouldWriteRawValue)
                {
                    var rawResult = new RawResult(query, typeReference, this.Api.Context);
                    singleResult = rawResult;
                    response = this.Request.CreateResponse(HttpStatusCode.OK, rawResult);
                }
                else
                {
                    var primitiveResult = new PrimitiveResult(query, typeReference, this.Api.Context);
                    singleResult = primitiveResult;
                    response = this.Request.CreateResponse(HttpStatusCode.OK, primitiveResult);
                }
            }

            if (typeReference.IsComplex())
            {
                var complexResult = new ComplexResult(query, typeReference, this.Api.Context);
                singleResult = complexResult;
                response = this.Request.CreateResponse(HttpStatusCode.OK, complexResult);
            }

            if (typeReference.IsEnum())
            {
                if (this.shouldWriteRawValue)
                {
                    var rawResult = new RawResult(query, typeReference, this.Api.Context);
                    singleResult = rawResult;
                    response = this.Request.CreateResponse(HttpStatusCode.OK, rawResult);
                }
                else
                {
                    var enumResult = new EnumResult(query, typeReference, this.Api.Context);
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
                if (elementType.IsPrimitive() || elementType.IsComplex() || elementType.IsEnum())
                {
                    return this.Request.CreateResponse(
                        HttpStatusCode.OK, new NonEntityCollectionResult(query, typeReference, this.Api.Context));
                }

                return this.Request.CreateResponse(
                    HttpStatusCode.OK, new EntityCollectionResult(query, typeReference, this.Api.Context));
            }

            var entityResult = new EntityResult(query, typeReference, this.Api.Context);
            if (entityResult.Result == null)
            {
                // TODO GitHubIssue#288: 204 expected when requesting single nav propery which has null value
                // ~/People(nonexistkey) and ~/People(nonexistkey)/BestFriend, expected 404
                // ~/People(key)/BestFriend, abd BestFriend is null, expected 204
                throw new HttpResponseException(
                    this.Request.CreateErrorResponse(
                        HttpStatusCode.NotFound,
                        Resources.ResourceNotFound));
            }

            // TODO GitHubIssue#43 : support non-Entity ($select/$value) queries
            return this.Request.CreateResponse(HttpStatusCode.OK, entityResult);
        }

        private IQueryable GetQuery()
        {
            ODataPath path = this.GetPath();

            RestierQueryBuilder builder = new RestierQueryBuilder(this.Api, path);
            IQueryable queryable = builder.BuildQuery();
            this.shouldReturnCount = builder.IsCountPathSegmentPresent;
            this.shouldWriteRawValue = builder.IsValuePathSegmentPresent;
            if (queryable == null)
            {
                throw new HttpResponseException(
                    this.Request.CreateErrorResponse(
                        HttpStatusCode.NotFound,
                        Resources.ResourceNotFound));
            }

            if (this.shouldWriteRawValue)
            {
                // Query options don't apply to $value.
                return queryable;
            }

            HttpRequestMessageProperties properties = this.Request.ODataProperties();
            ODataQueryContext queryContext =
                new ODataQueryContext(properties.Model, queryable.ElementType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, this.Request);

            // TODO GitHubIssue#41 : Ensure stable ordering for query
            ODataQuerySettings settings = new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                PageSize = null,  // no support for server enforced PageSize, yet
            };

            if (this.shouldReturnCount)
            {
                // Query options other than $filter and $search don't apply to $count.
                queryable = queryOptions.ApplyTo(
                    queryable, settings, AllowedQueryOptions.All ^ AllowedQueryOptions.Filter);
                return queryable;
            }

            if (queryOptions.Count != null)
            {
                ODataCountOption context = Api.Context.GetApiService<ODataCountOption>();
                context.IncludeTotalCount = queryOptions.Count.Value;
                context.SetTotalCount = value => properties.TotalCount = value;
            }

            // Entity count can NOT be evaluated at this point of time because the source
            // expression is just a placeholder to be replaced by the expression sourcer.
            queryable = queryOptions.ApplyTo(queryable, settings, AllowedQueryOptions.Count);

            return queryable;
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

        private IReadOnlyDictionary<string, object> GetOriginalValues()
        {
            Dictionary<string, object> originalValues = new Dictionary<string, object>();

            EntityTagHeaderValue etagHeaderValue = this.Request.Headers.IfMatch.SingleOrDefault();
            if (etagHeaderValue != null)
            {
                ETag etag = this.Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(ETagHeaderKey, etagHeaderValue.Tag);
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

        private IHttpActionResult CreateOKResult(object result)
        {
            return this.CreateResult(typeof(OkNegotiatedContentResult<>), result);
        }

        private IHttpActionResult CreateResult(Type resultType, object result)
        {
            Type genericResultType = resultType.MakeGenericType(result.GetType());

            return (IHttpActionResult)Activator.CreateInstance(genericResultType, result, this);
        }
    }
}
