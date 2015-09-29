// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
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
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi
{
    /// <summary>
    /// The all-in-one controller class to handle domain requests.
    /// </summary>
    [ODataDomainFormatting]
    [ODataDomainExceptionFilter]
    public class ODataDomainController : ODataController
    {
        private const string ETagGetterKey = "ETagGetter";
        private const string ETagHeaderKey = "@etag";

        private IDomain domain;
        private bool shouldWriteRawValue;

        /// <summary>
        /// Gets the domain associated with this controller.
        /// </summary>
        public IDomain Domain
        {
            get
            {
                if (this.domain == null)
                {
                    this.domain = this.Request.GetDomainFactory().Invoke();
                }

                return this.domain;
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
            var result = await Domain.QueryAsync(new QueryRequest(queryable), cancellationToken);

            this.Request.Properties[ETagGetterKey] = this.Domain.Context.GetProperty(ETagGetterKey);

            return this.CreateQueryResponse(result.Results.AsQueryable(), path.EdmType);
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

            ODataDomainChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(postEntry);

                SubmitResult result = await Domain.SubmitAsync(changeSet, cancellationToken);
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
                ODataDomainQueryBuilder.GetPathKeyValues(path),
                this.GetOriginalValues(),
                null);

            ODataDomainChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(deleteEntry);

                SubmitResult result = await Domain.SubmitAsync(changeSet, cancellationToken);
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
                throw new NotSupportedException();
            }

            ActionInvocationEntry entry = new ActionInvocationEntry(actionPathSegment.ActionName, null);

            ODataDomainChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(entry);

                SubmitResult result = await Domain.SubmitAsync(changeSet, cancellationToken);
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
        /// Disposes the domain and the controller.
        /// </summary>
        /// <param name="disposing">Indicates whether disposing is happening.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.domain != null)
                {
                    this.domain.Dispose();
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
                ODataDomainQueryBuilder.GetPathKeyValues(path),
                this.GetOriginalValues(),
                edmEntityObject.CreatePropertyDictionary());
            updateEntry.IsFullReplaceUpdate = isFullReplaceUpdate;

            ODataDomainChangeSetProperty changeSetProperty = this.Request.GetChangeSet();
            if (changeSetProperty == null)
            {
                ChangeSet changeSet = new ChangeSet();
                changeSet.Entries.Add(updateEntry);

                SubmitResult result = await Domain.SubmitAsync(changeSet, cancellationToken);
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

            if (typeReference.IsPrimitive())
            {
                if (this.shouldWriteRawValue)
                {
                    return this.Request.CreateResponse(
                        HttpStatusCode.OK, new RawResult(query, typeReference, this.Domain.Context));
                }

                return this.Request.CreateResponse(
                    HttpStatusCode.OK, new PrimitiveResult(query, typeReference, this.Domain.Context));
            }

            if (typeReference.IsComplex())
            {
                return this.Request.CreateResponse(
                    HttpStatusCode.OK, new ComplexResult(query, typeReference, this.Domain.Context));
            }

            if (typeReference.IsEnum())
            {
                if (this.shouldWriteRawValue)
                {
                    return this.Request.CreateResponse(
                        HttpStatusCode.OK, new RawResult(query, typeReference, this.Domain.Context));
                }

                return this.Request.CreateResponse(
                    HttpStatusCode.OK, new EnumResult(query, typeReference, this.Domain.Context));
            }

            if (typeReference.IsCollection())
            {
                var elementType = typeReference.AsCollection().ElementType();
                if (elementType.IsPrimitive() || elementType.IsComplex() || elementType.IsEnum())
                {
                    return this.Request.CreateResponse(
                        HttpStatusCode.OK, new NonEntityCollectionResult(query, typeReference, this.Domain.Context));
                }

                return this.Request.CreateResponse(
                    HttpStatusCode.OK, new EntityCollectionResult(query, typeReference, this.Domain.Context));
            }

            var entityResult = new EntityResult(query, typeReference, this.Domain.Context);
            if (entityResult.Result == null)
            {
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

            ODataDomainQueryBuilder builder = new ODataDomainQueryBuilder(this.Domain, path);
            IQueryable queryable = builder.BuildQuery();
            this.shouldWriteRawValue = builder.IsValuePathSegmentPresent;
            if (queryable == null)
            {
                throw new HttpResponseException(
                    this.Request.CreateErrorResponse(
                        HttpStatusCode.NotFound,
                        Resources.ResourceNotFound));
            }

            ODataQueryContext queryContext =
                new ODataQueryContext(this.Request.ODataProperties().Model, queryable.ElementType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, this.Request);

            // TODO GitHubIssue#41 : Ensure stable ordering for query
            ODataQuerySettings settings = new ODataQuerySettings()
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                EnsureStableOrdering = true,
                EnableConstantParameterization = false,
                PageSize = null,  // no support for server enforced PageSize, yet
            };

            queryable = queryOptions.ApplyTo(queryable, settings);

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

    internal static class Extensions
    {
        private const string PropertyNameOfConcurrencyProperties = "ConcurrencyProperties";

        private static PropertyInfo etagConcurrencyPropertiesProperty = typeof(ETag).GetProperty(
            PropertyNameOfConcurrencyProperties, BindingFlags.NonPublic | BindingFlags.Instance);

        public static void ApplyTo(this ETag etag, IDictionary<string, object> propertyValues)
        {
            if (etag != null)
            {
                IDictionary<string, object> concurrencyProperties =
                    (IDictionary<string, object>)etagConcurrencyPropertiesProperty.GetValue(etag);
                foreach (KeyValuePair<string, object> item in concurrencyProperties)
                {
                    propertyValues.Add(item.Key, item.Value);
                }
            }
        }

        public static IReadOnlyDictionary<string, object> CreatePropertyDictionary(this Delta entity)
        {
            Dictionary<string, object> propertyValues = new Dictionary<string, object>();
            foreach (string propertyName in entity.GetChangedPropertyNames())
            {
                object value;
                if (entity.TryGetPropertyValue(propertyName, out value))
                {
                    var complexObj = value as EdmComplexObject;
                    if (complexObj != null)
                    {
                        value = CreatePropertyDictionary(complexObj);
                    }

                    propertyValues.Add(propertyName, value);
                }
            }

            return propertyValues;
        }
    }
}
