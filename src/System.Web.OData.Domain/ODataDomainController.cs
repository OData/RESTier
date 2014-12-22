// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi
{
    [ODataDomainFormatting]
    [ODataDomainExceptionFilter]
    public abstract class ODataDomainController : ODataController
    {
        private IDomain _domain;

        protected ODataDomainController()
        {
        }

        public IDomain Domain
        {
            get
            {
                if (this._domain == null)
                {
                    this._domain = this.CreateDomain();
                }
                return this._domain;
            }
        }

        protected abstract IDomain CreateDomain();
    }

    public class ODataDomainController<T> : ODataDomainController
        where T : class, IDomain
    {
        public new T Domain
        {
            get
            {
                return base.Domain as T;
            }

        }

        public async Task<HttpResponseMessage> Get(
            CancellationToken cancellationToken)
        {
            HttpRequestMessageProperties odataProperties = this.Request.ODataProperties();
            ODataPath path = odataProperties.Path;
            if (path == null)
            {
                throw new InvalidOperationException("Controller cannot have null path");
            }

            IQueryable queryable = this.GetQuery(odataProperties);
            var result = await Domain.QueryAsync(new QueryRequest(queryable), cancellationToken);

            this.Request.Properties["ETagGetter"] = this.Domain.Context.GetProperty("ETagGetter");

            return this.CreateQueryResponse(result.Results.AsQueryable(), path.EdmType);
        }

        protected override IDomain CreateDomain()
        {
            return Activator.CreateInstance<T>();
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
                        queryable = ApplyNavigation(queryable, navigationSegment, ref currentEntityType, ref currentType);
                    }
                    else
                    {
                        throw new HttpResponseException(
                            this.Request.CreateErrorResponse(HttpStatusCode.NotFound, "Path segment not supported: " + segment));
                    }
                }
            }

            ODataQueryContext queryContext = new ODataQueryContext(this.Request.ODataProperties().Model, queryable.ElementType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, this.Request);

            // TODO: investigate stable ordering in reference to WCF data services
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
                throw new InvalidOperationException("Invalid request - No ODataProperties");
            }

            ODataPath path = properties.Path;
            if (path == null)
            {
                throw new InvalidOperationException("Invalid request - ODataPath is null.");
            }

            return path;
        }

        private IQueryable GetSource(ODataPath path, out IEdmEntityType rootEntityType)
        {
            IEdmNamedElement querySource = null;
            object[] queryArgs = null;
            ODataPathSegment firstPathSegment = path.Segments.FirstOrDefault();
            rootEntityType = null;
            if (firstPathSegment is EntitySetPathSegment)
            {
                IEdmEntitySetBase entitySet = ((EntitySetPathSegment)firstPathSegment).EntitySetBase;
                querySource = entitySet;
                rootEntityType = entitySet.EntityType();
            }
            else if (firstPathSegment is UnboundFunctionPathSegment)
            {
                UnboundFunctionPathSegment functionSegment = (UnboundFunctionPathSegment)firstPathSegment;
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

            return Domain.Source(querySource.Name, queryArgs);
        }

        private static IQueryable ApplyKeys(IQueryable queryable, KeyValuePathSegment keySegment, IEdmEntityType entityType, Type type)
        {
            BinaryExpression keyFilter = null;

            ParameterExpression parameterExpression = Expression.Parameter(type, "currentValue");
            IReadOnlyDictionary<string, object> keyValues = GetPathKeyValues(keySegment, entityType);

            foreach (KeyValuePair<string, object> keyValuePair in keyValues)
            {
                BinaryExpression equalsExpression = CreateEqualsExpression(parameterExpression, keyValuePair.Key, keyValuePair.Value);
                keyFilter = keyFilter == null ? equalsExpression : Expression.And(keyFilter, equalsExpression);
            }

            LambdaExpression whereExpression = Expression.Lambda(keyFilter, parameterExpression);
            return ExpressionHelpers.Where(queryable, whereExpression, type);
        }

        private static IReadOnlyDictionary<string, object> GetPathKeyValues(KeyValuePathSegment keySegment, IEdmEntityType entityType)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            IEnumerable<IEdmStructuralProperty> keys = entityType.Key();

            // TODO: this parsing implementation does not allow key values to contain commas
            // Get the WebAPI team to make KeyValuePathSegment.Values collection public (or have the parsing logic public)
            string[] values = keySegment.Value.Split(',');
            if (values.Length > 1)
            {
                foreach (string value in values)
                {
                    // Split key name and key value
                    string[] keyValues = value.Split('=');
                    if (keyValues.Length != 2)
                    {
                        throw new InvalidOperationException("Keys were not specified in the format of 'KeyName=KeyValue'");
                    }

                    // Validate the key name
                    if (!keys.Select(k => k.Name).Contains(keyValues[0]))
                    {
                        throw new InvalidOperationException(string.Format("Specified key '{0}' is not a valid property of entity '{1}'", keyValues[0], entityType.Name));
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
                    throw new InvalidOperationException("Only one key was specified, when multiple were expected");
                }

                string keyName = keys.First().Name;
                result.Add(keyName, ODataUriUtils.ConvertFromUriLiteral(keySegment.Value, ODataVersion.V4));
            }

            return result;
        }

        private static BinaryExpression CreateEqualsExpression(ParameterExpression parameterExpression, string propertyName, object propertyValue)
        {
            MemberExpression property = Expression.Property(parameterExpression, propertyName);
            ConstantExpression constant = Expression.Constant(propertyValue);

            return Expression.Equal(property, constant);
        }

        private IQueryable ApplyNavigation(IQueryable queryable, NavigationPathSegment navigationSegment, ref IEdmEntityType currentEntityType, ref Type currentType)
        {
            ParameterExpression entityParameterExpression = Expression.Parameter(currentType);
            Expression navigationPropertyExpression = Expression.Property(entityParameterExpression, navigationSegment.NavigationPropertyName);

            currentEntityType = navigationSegment.NavigationProperty.ToEntityType();

            if (navigationSegment.NavigationProperty.TargetMultiplicity() == EdmMultiplicity.Many)
            {
                // get the element type of the target (the type should be an EntityCollection<T> for navigation queries).
                currentType = navigationPropertyExpression.Type.GetEnumerableItemType();

                // need to explicitly define the delegate type as IEnumerable<T>
                Type delegateType = typeof(Func<,>).MakeGenericType(queryable.ElementType, typeof(IEnumerable<>).MakeGenericType(currentType));
                LambdaExpression selectBody = Expression.Lambda(delegateType, navigationPropertyExpression, entityParameterExpression);

                return ExpressionHelpers.SelectMany(queryable, selectBody, currentType);
            }
            else
            {
                currentType = navigationPropertyExpression.Type;
                LambdaExpression selectBody = Expression.Lambda(navigationPropertyExpression, entityParameterExpression);
                return ExpressionHelpers.Select(queryable, selectBody);
            }
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
                // TODO: support non-Entity ($select/$value) queries
                return this.Request.CreateResponse(
                    HttpStatusCode.OK, new EntityResult(query, typeReference, this.Domain.Context));
            }
        }

        private static IEdmTypeReference GetTypeReference(IEdmType type)
        {
            return type.GetEdmTypeReference(isNullable: false);
        }

        public async Task<IHttpActionResult> Post(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            ODataPath path = this.GetPath();
            IEdmEntitySet entitySet = path.NavigationSource as IEdmEntitySet;
            if (entitySet == null)
            {
                throw new NotImplementedException("Currently only EntitySets can be inserted into.");
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

        public Task<IHttpActionResult> Put(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            return this.Update(edmEntityObject, true, cancellationToken);
        }

        public Task<IHttpActionResult> Patch(EdmEntityObject edmEntityObject, CancellationToken cancellationToken)
        {
            return this.Update(edmEntityObject, false, cancellationToken);
        }

        private async Task<IHttpActionResult> Update(EdmEntityObject edmEntityObject, bool isFullReplaceUpdate, CancellationToken cancellationToken)
        {
            ODataPath path = this.GetPath();
            IEdmEntitySet entitySet = path.NavigationSource as IEdmEntitySet;
            if (entitySet == null)
            {
                throw new NotImplementedException("Currently only EntitySets can be updated.");
            }

            DataModificationEntry updateEntry = new DataModificationEntry(
                entitySet.Name,
                path.EdmType.FullTypeName(),
                this.GetPathKeyValues(path),
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

        public async Task<IHttpActionResult> Delete(CancellationToken cancellationToken)
        {
            ODataPath path = this.GetPath();
            IEdmEntitySet entitySet = path.NavigationSource as IEdmEntitySet;
            if (entitySet == null)
            {
                throw new NotImplementedException("Currently only EntitySets can be deleted from.");
            }

            DataModificationEntry deleteEntry = new DataModificationEntry(
                entitySet.Name,
                path.EdmType.FullTypeName(),
                this.GetPathKeyValues(path),
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
                return this.StatusCode(HttpStatusCode.NoContent);
            }
        }

        private IReadOnlyDictionary<string, object> GetPathKeyValues(ODataPath path)
        {
            if (path.PathTemplate == "~/entityset/key" ||
                path.PathTemplate == "~/entityset/key/cast")
            {
                KeyValuePathSegment keySegment = (KeyValuePathSegment)path.Segments[1];
                return GetPathKeyValues(keySegment, (IEdmEntityType)path.EdmType);
            }
            else
            {
                throw new InvalidOperationException("Invalid request - Expecting ~/entityset/key path template");
            }
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
        private static PropertyInfo etagConcurrencyPropertiesProperty = typeof(ETag).GetProperty("ConcurrencyProperties", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void ApplyTo(this ETag etag, IDictionary<string, object> propertyValues)
        {
            if (etag != null)
            {
                IDictionary<string, object> concurrencyProperties = (IDictionary<string, object>)etagConcurrencyPropertiesProperty.GetValue(etag);
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
                    propertyValues.Add(propertyName, value);
                }
            }
            return propertyValues;
        }
    }
}
