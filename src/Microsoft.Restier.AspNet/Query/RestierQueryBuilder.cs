// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.Core;
using ODataPath = Microsoft.AspNet.OData.Routing.ODataPath;

namespace Microsoft.Restier.AspNet.Query
{
    internal class RestierQueryBuilder
    {
        private const string DefaultNameOfParameterExpression = "currentValue";

        private readonly ApiBase api;
        private readonly ODataPath path;
        private readonly IDictionary<Type, Action<ODataPathSegment>> handlers = new Dictionary<Type, Action<ODataPathSegment>>();
        private readonly IEdmModel edmModel;

        private IQueryable queryable;
        private Type currentType;


        public RestierQueryBuilder(ApiBase api, ODataPath path)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(path, nameof(path));
            this.api = api;
            this.path = path;

            // TODO: JWS: At best a hack to avoid a deadlock, because the only place to get the model is in a synchronous method or
            // constructor. See https://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
            this.edmModel = Task.Run(() => this.api.GetModelAsync()).Result;

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

            // Complex cast is not supported by EF, and is not supported here
            // this.handlers[ODataSegmentKinds.ComplexCast] = null;
        }

        public bool IsCountPathSegmentPresent { get; private set; }

        public bool IsValuePathSegmentPresent { get; private set; }

        public IQueryable BuildQuery()
        {
            queryable = null;

            foreach (var segment in path.Segments)
            {
                if (!handlers.TryGetValue(segment.GetType(), out var handler))
                {
                    throw new NotImplementedException(
                        string.Format(CultureInfo.InvariantCulture, Resources.PathSegmentNotSupported, segment));
                }

                handler(segment);
            }

            return queryable;
        }

        #region Helper Methods
        internal static IReadOnlyDictionary<string, object> GetPathKeyValues(ODataPath path)
        {
            if (path.PathTemplate == "~/entityset/key" ||
                path.PathTemplate == "~/entityset/key/cast")
            {
                var keySegment = (KeySegment)path.Segments[1];
                return GetPathKeyValues(keySegment);
            }
            else if (path.PathTemplate == "~/entityset/cast/key")
            {
                var keySegment = (KeySegment)path.Segments[2];
                return GetPathKeyValues(keySegment);
            }
            else
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.InvalidPathTemplateInRequest,
                    "~/entityset/key"));
            }
        }

        private static IReadOnlyDictionary<string, object> GetPathKeyValues(
            KeySegment keySegment)
        {
            var result = new Dictionary<string, object>();

            // TODO GitHubIssue#42 : Improve key parsing logic
            // this parsing implementation does not allow key values to contain commas
            // Depending on the WebAPI to make KeyValuePathSegment.Values collection public
            // (or have the parsing logic public).
            var keyValuePairs = keySegment.Keys;

            foreach (var keyValuePair in keyValuePairs)
            {
                result.Add(keyValuePair.Key, keyValuePair.Value);
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
        #endregion

        #region Handler Methods
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
            var keyValues = GetPathKeyValues(keySegment);

            BinaryExpression keyFilter = null;
            foreach (var keyValuePair in keyValues)
            {
                var equalsExpression =
                    CreateEqualsExpression(parameterExpression, keyValuePair.Key, keyValuePair.Value);
                keyFilter = keyFilter == null ? equalsExpression : Expression.And(keyFilter, equalsExpression);
            }

            var whereExpression = Expression.Lambda(keyFilter, parameterExpression);
            queryable = ExpressionHelpers.Where(queryable, whereExpression, currentType);
        }

        private void HandleNavigationPathSegment(ODataPathSegment segment)
        {
            var navigationSegment = (NavigationPropertySegment)segment;
            var entityParameterExpression = Expression.Parameter(currentType);
            var navigationPropertyExpression =
                Expression.Property(entityParameterExpression, navigationSegment.NavigationProperty.Name);

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
            var structuralPropertyExpression =
                Expression.Property(entityParameterExpression, propertySegment.Property.Name);

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
                currentType = edmType.GetClrType(this.edmModel);
                queryable = ExpressionHelpers.OfType(queryable, currentType);
            }
        }
        #endregion
    }
}
