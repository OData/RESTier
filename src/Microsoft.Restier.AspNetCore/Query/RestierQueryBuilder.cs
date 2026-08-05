// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using AspNetResources = Microsoft.Restier.AspNetCore.Resources;

namespace Microsoft.Restier.AspNetCore.Query
{
    /// <summary>
    /// Restier Query Builder. Builds a Linq Query based on the received path.
    /// </summary>
    internal class RestierQueryBuilder
    {
        private const string DefaultNameOfParameterExpression = "currentValue";

        private readonly ApiBase api;
        private readonly ODataPath path;
        private readonly ODataQuerySettings querySettings;
        private readonly IFilterBinder filterBinder;
        private readonly IDictionary<Type, Action<ODataPathSegment>> handlers = new Dictionary<Type, Action<ODataPathSegment>>();
        private readonly IEdmModel edmModel;

        private IQueryable queryable;
        private Type currentType;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierQueryBuilder"/> class.
        /// </summary>
        /// <param name="api">The Api to use.</param>
        /// <param name="path">The path to process.</param>
        /// <param name="querySettings">
        /// The per-route <see cref="ODataQuerySettings"/> resolved from DI. Used when binding
        /// <see cref="FilterSegment"/> expressions so that <see cref="TimeZoneInfo"/>-aware
        /// DateTime literal conversion matches the rest of the filter pipeline (issue #704).
        /// </param>
        /// <param name="filterBinder">
        /// Optional <see cref="IFilterBinder"/> used by path-segment $filter handling. When null,
        /// <see cref="HandleFilterPathSegment"/> falls back to a fresh <c>FilterBinder</c>
        /// — observationally identical to the historical behavior.
        /// </param>
        public RestierQueryBuilder(ApiBase api, ODataPath path, ODataQuerySettings querySettings, IFilterBinder filterBinder = null)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(path, nameof(path));
            Ensure.NotNull(querySettings, nameof(querySettings));
            this.api = api;
            this.path = path;
            this.querySettings = querySettings;
            this.filterBinder = filterBinder;

            edmModel = this.api.Model;

            handlers[typeof(EntitySetSegment)] = HandleEntitySetPathSegment;
            handlers[typeof(SingletonSegment)] = HandleSingletonPathSegment;
            handlers[typeof(OperationSegment)] = EmptyHandler;
            handlers[typeof(OperationImportSegment)] = EmptyHandler;
            handlers[typeof(CountSegment)] = HandleCountPathSegment;
            handlers[typeof(ValueSegment)] = HandleValuePathSegment;
            handlers[typeof(KeySegment)] = HandleKeyValuePathSegment;
            handlers[typeof(NavigationPropertySegment)] = HandleNavigationPathSegment;
            handlers[typeof(PropertySegment)] = HandlePropertyAccessPathSegment;
            handlers[typeof(TypeSegment)] = HandleEntityTypeSegment;
            handlers[typeof(FilterSegment)] = HandleFilterPathSegment;

            // Complex cast is not supported by EF, and is not supported here
            // this.handlers[ODataSegmentKinds.ComplexCast] = null;
        }

        /// <summary>
        /// Gets a value indicating whether a Count path segment is present.
        /// </summary>
        public bool IsCountPathSegmentPresent { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a value path segment is present.
        /// </summary>
        public bool IsValuePathSegmentPresent { get; private set; }

        /// <summary>
        /// Builds an <see cref="IQueryable"/> based on the path.
        /// </summary>
        /// <returns>An <see cref="IQueryable"/> instance.</returns>
        public IQueryable BuildQuery()
        {
            queryable = null;

            foreach (var segment in path)
            {
                if (!handlers.TryGetValue(segment.GetType(), out var handler))
                {
                    throw new NotImplementedException(
                        string.Format(CultureInfo.InvariantCulture, AspNetResources.PathSegmentNotSupported, segment));
                }

                handler(segment);
            }

            return queryable;
        }

        internal static IReadOnlyDictionary<string, object> GetPathKeyValues(ODataPath path, IEdmModel model)
        {
            var segments = path.ToList();

            if (segments.Count == 2 && segments[0] is EntitySetSegment && segments[1] is KeySegment keySegment)
            {
                return GetPathKeyValues(keySegment, model);
            }
            else if (segments.Count == 3 && segments[0] is EntitySetSegment && segments[1] is KeySegment keySegment2 && segments[2] is TypeSegment)
            {
                return GetPathKeyValues(keySegment2, model);
            }
            else if (segments.Count == 3 && segments[0] is EntitySetSegment && segments[1] is TypeSegment && segments[2] is KeySegment keySegment3)
            {
                return GetPathKeyValues(keySegment3, model);
            }
            else
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    AspNetResources.InvalidPathTemplateInRequest,
                    "~/entityset/key"));
            }
        }

        private static IReadOnlyDictionary<string, object> GetPathKeyValues(
            KeySegment keySegment, IEdmModel model)
        {
            var result = new Dictionary<string, object>();
            var entityType = keySegment.EdmType as IEdmEntityType;

            // TODO GitHubIssue#42 : Improve key parsing logic
            // this parsing implementation does not allow key values to contain commas
            // Depending on the WebAPI to make KeyValuePathSegment.Values collection public
            // (or have the parsing logic public).
            var keyValuePairs = keySegment.Keys;

            foreach (var keyValuePair in keyValuePairs)
            {
                var edmProperty = entityType?.FindProperty(keyValuePair.Key);
                var clrName = edmProperty is not null
                    ? EdmClrPropertyMapper.GetClrPropertyName(edmProperty, model)
                    : keyValuePair.Key;
                result.Add(clrName, keyValuePair.Value);
            }

            return result;
        }

        private static BinaryExpression CreateEqualsExpression(
            ParameterExpression parameterExpression,
            string propertyName,
            object propertyValue)
        {
            var property = Expression.Property(parameterExpression, propertyName);
            var constant = Expression.Constant(
                TypeConverter.ChangeType(propertyValue, property.Type, CultureInfo.InvariantCulture));
            return Expression.Equal(property, constant);
        }

        private static LambdaExpression CreateNotEqualsNullExpression(
            Expression propertyExpression, ParameterExpression parameterExpression)
        {
            var nullConstant = Expression.Constant(null);
            var nullFilterExpression = Expression.NotEqual(propertyExpression, nullConstant);
            var whereExpression = Expression.Lambda(nullFilterExpression, parameterExpression);

            return whereExpression;
        }

        private void HandleEntitySetPathSegment(ODataPathSegment segment)
        {
            var entitySetPathSegment = (EntitySetSegment)segment;
            var entitySet = entitySetPathSegment.EntitySet;
            queryable = api.GetQueryableSource(entitySet.Name, (object[])null);
            currentType = queryable.ElementType;
        }

        private void HandleSingletonPathSegment(ODataPathSegment segment)
        {
            var singletonPathSegment = (SingletonSegment)segment;
            var singleton = singletonPathSegment.Singleton;
            queryable = api.GetQueryableSource(singleton.Name, (object[])null);
            currentType = queryable.ElementType;
        }

        private void EmptyHandler(ODataPathSegment segment)
        {
            // Nothing will be done
        }

        private void HandleCountPathSegment(ODataPathSegment segment) => IsCountPathSegmentPresent = true;

        private void HandleValuePathSegment(ODataPathSegment segment) => IsValuePathSegmentPresent = true;

        private void HandleKeyValuePathSegment(ODataPathSegment segment)
        {
            var keySegment = (KeySegment)segment;

            var parameterExpression = Expression.Parameter(currentType, DefaultNameOfParameterExpression);
            var keyValues = GetPathKeyValues(keySegment, edmModel);

            BinaryExpression keyFilter = null;
            foreach (var keyValuePair in keyValues)
            {
                var equalsExpression =
                    CreateEqualsExpression(parameterExpression, keyValuePair.Key, keyValuePair.Value);
                keyFilter = keyFilter is null ? equalsExpression : Expression.And(keyFilter, equalsExpression);
            }

            var whereExpression = Expression.Lambda(keyFilter, parameterExpression);
            queryable = ExpressionHelpers.Where(queryable, whereExpression, currentType);
        }

        private void HandleNavigationPathSegment(ODataPathSegment segment)
        {
            var navigationSegment = (NavigationPropertySegment)segment;
            var entityParameterExpression = Expression.Parameter(currentType);
            var navigationClrName = EdmClrPropertyMapper.GetClrPropertyName(navigationSegment.NavigationProperty, edmModel);
            var navigationPropertyExpression =
                Expression.Property(entityParameterExpression, navigationClrName);

            if (navigationSegment.NavigationProperty.TargetMultiplicity() == EdmMultiplicity.Many)
            {
                // get the element type of the target
                // (the type should be an EntityCollection<T> for navigation queries).
                currentType = navigationPropertyExpression.Type.GetEnumerableItemType();

                // need to explicitly define the delegate type as IEnumerable<T>
                var delegateType = typeof(Func<,>).MakeGenericType(
                    queryable.ElementType,
                    typeof(IEnumerable<>).MakeGenericType(currentType));
                var selectBody =
                    Expression.Lambda(delegateType, navigationPropertyExpression, entityParameterExpression);

                queryable = ExpressionHelpers.SelectMany(queryable, selectBody, currentType);
            }
            else
            {
                // Check whether property is null or not before further selection
                // RWM: Removed from the outer loop because I don't believe it is necessary for Collection properties.
                var whereExpression = CreateNotEqualsNullExpression(navigationPropertyExpression, entityParameterExpression);
                queryable = ExpressionHelpers.Where(queryable, whereExpression, currentType);

                currentType = navigationPropertyExpression.Type;
                var selectBody =
                    Expression.Lambda(navigationPropertyExpression, entityParameterExpression);
                queryable = ExpressionHelpers.Select(queryable, selectBody);
            }
        }

        private void HandlePropertyAccessPathSegment(ODataPathSegment segment)
        {
            var propertySegment = (PropertySegment)segment;
            var entityParameterExpression = Expression.Parameter(currentType);
            var propertyClrName = EdmClrPropertyMapper.GetClrPropertyName(propertySegment.Property, edmModel);
            var structuralPropertyExpression =
                Expression.Property(entityParameterExpression, propertyClrName);

            // Check whether property is null or not before further selection
            if (propertySegment.Property.Type.IsNullable && !propertySegment.Property.Type.IsPrimitive())
            {
                var whereExpression =
                    CreateNotEqualsNullExpression(structuralPropertyExpression, entityParameterExpression);
                queryable = ExpressionHelpers.Where(queryable, whereExpression, currentType);
            }

            if (propertySegment.Property.Type.IsCollection())
            {
                // Produces new query like 'queryable.SelectMany(param => param.PropertyName)'.
                // Suppose 'param.PropertyName' is of type 'IEnumerable<T>', the type of the
                // resulting query would be 'IEnumerable<T>' too.
                currentType = structuralPropertyExpression.Type.GetEnumerableItemType();
                var delegateType = typeof(Func<,>).MakeGenericType(
                    queryable.ElementType,
                    typeof(IEnumerable<>).MakeGenericType(currentType));
                var selectBody =
                    Expression.Lambda(delegateType, structuralPropertyExpression, entityParameterExpression);
                queryable = ExpressionHelpers.SelectMany(queryable, selectBody, currentType);
            }
            else
            {
                // Produces new query like 'queryable.Select(param => param.PropertyName)'.
                currentType = structuralPropertyExpression.Type;
                var selectBody =
                    Expression.Lambda(structuralPropertyExpression, entityParameterExpression);
                queryable = ExpressionHelpers.Select(queryable, selectBody);
            }
        }

        // This only covers entity type cast
        // complex type cast uses ComplexCastPathSegment and is not supported by EF now
        // CLR type is got from model annotation, which means model must include that annotation.
        private void HandleEntityTypeSegment(ODataPathSegment segment)
        {
            var typeSegment = (TypeSegment)segment;
            var edmType = typeSegment.EdmType;

            if (typeSegment.EdmType.TypeKind == EdmTypeKind.Collection)
            {
                edmType = ((IEdmCollectionType)typeSegment.EdmType).ElementType.Definition;
            }

            if (edmType.TypeKind == EdmTypeKind.Entity)
            {
                currentType = edmType.GetClrType(edmModel);
                queryable = ExpressionHelpers.OfType(queryable, currentType);
            }
        }

        private void HandleFilterPathSegment(ODataPathSegment segment)
        {
            var filterSegment = (FilterSegment)segment;

            // Wrap the segment's expression in a FilterClause so we can reuse
            // the ASP.NET Core OData FilterBinder infrastructure.
            var filterClause = new FilterClause(filterSegment.Expression, filterSegment.RangeVariable);

            var binder = this.filterBinder ?? new FilterBinder();
            var context = new QueryBinderContext(edmModel, querySettings, currentType);

            queryable = binder.ApplyBind(queryable, filterClause, context);
        }
    }
}
