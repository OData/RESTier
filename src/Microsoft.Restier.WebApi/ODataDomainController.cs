// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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
using Microsoft.OData.Core;
using Microsoft.OData.Core.UriParser;
using Microsoft.OData.Edm;
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
    /// The base class for all domain controllers.
    /// </summary>
    [ODataDomainFormatting]
    [ODataDomainExceptionFilter]
    public abstract class ODataDomainController : ODataController
    {
        private IDomain domain;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataDomainController" /> class.
        /// </summary>
        protected ODataDomainController()
        {
        }

        /// <summary>
        /// Gets the domain instance associated with the controller.
        /// </summary>
        public IDomain Domain
        {
            get
            {
                if (this.domain == null)
                {
                    this.domain = this.CreateDomain();
                }

                return this.domain;
            }
        }

        /// <summary>
        /// Creates a domain instance.
        /// </summary>
        /// <returns>The domain instance created.</returns>
        protected abstract IDomain CreateDomain();

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
                    this.domain = null;
                }
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// The base class for all domain controllers with domain specified.
    /// </summary>
    /// <typeparam name="T">The specified domain class.</typeparam>
    public class ODataDomainController<T> : ODataDomainController
        where T : class, IDomain
    {
        /// <summary>
        /// Gets the domain class of the <see cref="ODataDomainController"/>.
        /// </summary>
        public new T Domain
        {
            get
            {
                return base.Domain as T;
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

            IQueryable queryable = this.GetQuery(odataProperties);
            var result = await Domain.QueryAsync(new QueryRequest(queryable), cancellationToken);

            this.Request.Properties["ETagGetter"] = this.Domain.Context.GetProperty("ETagGetter");

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
                GetPathKeyValues(path),
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
        /// Creates a domain instance of type T.
        /// </summary>
        /// <returns>The domain instance created.</returns>
        protected override IDomain CreateDomain()
        {
            return Activator.CreateInstance<T>();
        }

        private static IQueryable ApplyKeys(
            IQueryable queryable,
            KeyValuePathSegment keySegment,
            IEdmEntityType entityType,
            Type type)
        {
            BinaryExpression keyFilter = null;

            ParameterExpression parameterExpression = Expression.Parameter(type, "currentValue");
            IReadOnlyDictionary<string, object> keyValues = GetPathKeyValues(keySegment, entityType);

            foreach (KeyValuePair<string, object> keyValuePair in keyValues)
            {
                BinaryExpression equalsExpression =
                    CreateEqualsExpression(parameterExpression, keyValuePair.Key, keyValuePair.Value);
                keyFilter = keyFilter == null ? equalsExpression : Expression.And(keyFilter, equalsExpression);
            }

            LambdaExpression whereExpression = Expression.Lambda(keyFilter, parameterExpression);
            return ExpressionHelpers.Where(queryable, whereExpression, type);
        }

        private static IReadOnlyDictionary<string, object> GetPathKeyValues(
            KeyValuePathSegment keySegment,
            IEdmEntityType entityType)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            IEnumerable<IEdmStructuralProperty> keys = entityType.Key();

            // TODO GitHubIssue#42 : Improve key parsing logic
            // this parsing implementation does not allow key values to contain commas
            // Depending on the WebAPI to make KeyValuePathSegment.Values collection public
            // (or have the parsing logic public).
            string[] values = keySegment.Value.Split(',');
            if (values.Length > 1)
            {
                foreach (string value in values)
                {
                    // Split key name and key value
                    string[] keyValues = value.Split('=');
                    if (keyValues.Length != 2)
                    {
                        throw new InvalidOperationException(Resources.IncorrectKeyFormat);
                    }

                    // Validate the key name
                    if (!keys.Select(k => k.Name).Contains(keyValues[0]))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.KeyNotValidForEntityType,
                            keyValues[0],
                            entityType.Name));
                    }

                    result.Add(keyValues[0], ODataUriUtils.ConvertFromUriLiteral(keyValues[1], ODataVersion.V4));
                }
            }
            else
            {
                // We just have the single key value
                // Validate it has exactly one key
                if (keys.Count() > 1)
                {
                    throw new InvalidOperationException(Resources.MultiKeyValuesExpected);
                }

                string keyName = keys.First().Name;
                result.Add(keyName, ODataUriUtils.ConvertFromUriLiteral(keySegment.Value, ODataVersion.V4));
            }

            return result;
        }

        private static BinaryExpression CreateEqualsExpression(
            ParameterExpression parameterExpression,
            string propertyName,
            object propertyValue)
        {
            MemberExpression property = Expression.Property(parameterExpression, propertyName);
            ConstantExpression constant = Expression.Constant(propertyValue);

            return Expression.Equal(property, constant);
        }

        private static IQueryable ApplyNavigation(
            IQueryable queryable,
            NavigationPathSegment navigationSegment,
            ref IEdmEntityType currentEntityType,
            ref Type currentType)
        {
            ParameterExpression entityParameterExpression = Expression.Parameter(currentType);
            Expression navigationPropertyExpression =
                Expression.Property(entityParameterExpression, navigationSegment.NavigationPropertyName);

            currentEntityType = navigationSegment.NavigationProperty.ToEntityType();

            if (navigationSegment.NavigationProperty.TargetMultiplicity() == EdmMultiplicity.Many)
            {
                // get the element type of the target
                // (the type should be an EntityCollection<T> for navigation queries).
                currentType = navigationPropertyExpression.Type.GetEnumerableItemType();

                // need to explicitly define the delegate type as IEnumerable<T>
                Type delegateType = typeof(Func<,>).MakeGenericType(
                    queryable.ElementType,
                    typeof(IEnumerable<>).MakeGenericType(currentType));
                LambdaExpression selectBody =
                    Expression.Lambda(delegateType, navigationPropertyExpression, entityParameterExpression);

                return ExpressionHelpers.SelectMany(queryable, selectBody, currentType);
            }
            else
            {
                currentType = navigationPropertyExpression.Type;
                LambdaExpression selectBody =
                    Expression.Lambda(navigationPropertyExpression, entityParameterExpression);
                return ExpressionHelpers.Select(queryable, selectBody);
            }
        }

        private static IEdmTypeReference GetTypeReference(IEdmType type)
        {
            return type.GetEdmTypeReference(isNullable: false);
        }

        private static IReadOnlyDictionary<string, object> GetPathKeyValues(ODataPath path)
        {
            if (path.PathTemplate == "~/entityset/key" ||
                path.PathTemplate == "~/entityset/key/cast")
            {
                KeyValuePathSegment keySegment = (KeyValuePathSegment)path.Segments[1];
                return GetPathKeyValues(keySegment, (IEdmEntityType)path.EdmType);
            }
            else
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture, Resources.InvalidPathTemplateInRequest, "~/entityset/key"));
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
                GetPathKeyValues(path),
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
            if (typeReference.IsCollection())
            {
                return this.Request.CreateResponse(
                    HttpStatusCode.OK, new EntityCollectionResult(query, typeReference, this.Domain.Context));
            }
            else
            {
                // TODO GitHubIssue#43 : support non-Entity ($select/$value) queries
                return this.Request.CreateResponse(
                    HttpStatusCode.OK, new EntityResult(query, typeReference, this.Domain.Context));
            }
        }

        private IQueryable GetQuery(HttpRequestMessageProperties odataProperties)
        {
            ODataPath path = this.GetPath();

            IEdmEntityType currentEntityType;
            IQueryable queryable = this.GetSource(path, out currentEntityType);

            // Apply segments to queryable
            Type currentType = queryable.ElementType;
            foreach (ODataPathSegment segment in path.Segments.Skip(1))
            {
                KeyValuePathSegment keySegment = segment as KeyValuePathSegment;
                if (keySegment != null)
                {
                    queryable = ApplyKeys(queryable, keySegment, currentEntityType, currentType);
                }
                else
                {
                    NavigationPathSegment navigationSegment = segment as NavigationPathSegment;
                    if (navigationSegment != null)
                    {
                        queryable = ApplyNavigation(
                            queryable,
                            navigationSegment,
                            ref currentEntityType,
                            ref currentType);
                    }
                    else
                    {
                        throw new HttpResponseException(
                            this.Request.CreateErrorResponse(
                                HttpStatusCode.NotFound,
                                "Path segment not supported: " + segment));
                    }
                }
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

        private IQueryable GetSource(ODataPath path, out IEdmEntityType rootEntityType)
        {
            IEdmNamedElement querySource = null;
            object[] queryArgs = null;
            ODataPathSegment firstPathSegment = path.Segments.FirstOrDefault();
            rootEntityType = null;
            var entitySetPathSegment = firstPathSegment as EntitySetPathSegment;
            if (entitySetPathSegment != null)
            {
                IEdmEntitySetBase entitySet = entitySetPathSegment.EntitySetBase;
                querySource = entitySet;
                rootEntityType = entitySet.EntityType();
            }
            else
            {
                var unboundFunctionPathSegment = firstPathSegment as UnboundFunctionPathSegment;
                if (unboundFunctionPathSegment != null)
                {
                    UnboundFunctionPathSegment functionSegment = unboundFunctionPathSegment;
                    IEdmFunctionImport functionImport = functionSegment.Function;
                    querySource = functionImport;
                    IEdmEntityTypeReference entityTypeRef = functionImport.Function.ReturnType.AsEntity();
                    rootEntityType = entityTypeRef == null ? null : entityTypeRef.EntityDefinition();

                    if (functionImport.Function.Parameters.Any())
                    {
                        queryArgs = functionImport.Function.Parameters.Select(
                            p => functionSegment.GetParameterValue(p.Name)).ToArray();
                    }
                }
                else
                {
                    throw new HttpResponseException(
                        this.Request.CreateErrorResponse(
                            HttpStatusCode.NotFound,
                            Resources.UnknownResourceRequested));
                }
            }

            return Domain.Source(querySource.Name, queryArgs);
        }

        private IReadOnlyDictionary<string, object> GetOriginalValues()
        {
            Dictionary<string, object> originalValues = new Dictionary<string, object>();

            EntityTagHeaderValue etagHeaderValue = this.Request.Headers.IfMatch.SingleOrDefault();
            if (etagHeaderValue != null)
            {
                ETag etag = this.Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add("@etag", etagHeaderValue.Tag);
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
        private static PropertyInfo etagConcurrencyPropertiesProperty =
            typeof(ETag).GetProperty("ConcurrencyProperties", BindingFlags.NonPublic | BindingFlags.Instance);

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
