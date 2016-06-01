// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Web.OData;
using System.Web.OData.Routing;
using Microsoft.OData.Core;
using Microsoft.OData.Core.UriParser;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Publishers.OData.Properties;

namespace Microsoft.Restier.Publishers.OData.Query
{
    internal class RestierQueryBuilder
    {
        private const string DefaultNameOfParameterExpression = "currentValue";
        private const char EntityKeySeparator = ',';
        private const char EntityKeyNameValueSeparator = '=';

        private readonly ApiBase api;
        private readonly ODataPath path;
        private readonly IDictionary<string, Action<ODataPathSegment>> handlers =
            new Dictionary<string, Action<ODataPathSegment>>();

        private IQueryable queryable;
        private IEdmEntityType currentEntityType;
        private Type currentType;

        public RestierQueryBuilder(ApiBase api, ODataPath path)
        {
            Ensure.NotNull(api, "api");
            Ensure.NotNull(path, "path");
            this.api = api;
            this.path = path;

            this.handlers[ODataSegmentKinds.EntitySet] = this.HandleEntitySetPathSegment;
            this.handlers[ODataSegmentKinds.Singleton] = this.HandleSingletonPathSegment;
            this.handlers[ODataSegmentKinds.UnboundFunction] = this.HandleUnboundFunctionPathSegment;
            this.handlers[ODataSegmentKinds.Function] = this.HandleBoundFunctionPathSegment;
            this.handlers[ODataSegmentKinds.Count] = this.HandleCountPathSegment;
            this.handlers[ODataSegmentKinds.Value] = this.HandleValuePathSegment;
            this.handlers[ODataSegmentKinds.Key] = this.HandleKeyValuePathSegment;
            this.handlers[ODataSegmentKinds.Navigation] = this.HandleNavigationPathSegment;
            this.handlers[ODataSegmentKinds.Property] = this.HandlePropertyAccessPathSegment;
            this.handlers[ODataSegmentKinds.Cast] = this.HandleCastPathSegment;

            // Complex cast is not supported by EF, and is not supported here
            // this.handlers[ODataSegmentKinds.ComplexCast] = null;
        }

        public bool IsCountPathSegmentPresent { get; private set; }

        public bool IsValuePathSegmentPresent { get; private set; }

        public IQueryable BuildQuery()
        {
            this.queryable = null;

            foreach (var segment in this.path.Segments)
            {
                Action<ODataPathSegment> handler;
                if (!this.handlers.TryGetValue(segment.SegmentKind, out handler))
                {
                    throw new NotSupportedException(
                        string.Format(CultureInfo.InvariantCulture, Resources.PathSegmentNotSupported, segment));
                }

                handler(segment);
            }

            return this.queryable;
        }

        #region Helper Methods
        internal static IReadOnlyDictionary<string, object> GetPathKeyValues(ODataPath path)
        {
            if (path.PathTemplate == "~/entityset/key" ||
                path.PathTemplate == "~/entityset/key/cast")
            {
                KeyValuePathSegment keySegment = (KeyValuePathSegment)path.Segments[1];
                return GetPathKeyValues(keySegment, (IEdmEntityType)path.EdmType);
            }
            else if (path.PathTemplate == "~/entityset/cast/key")
            {
                KeyValuePathSegment keySegment = (KeyValuePathSegment)path.Segments[2];
                return GetPathKeyValues(keySegment, (IEdmEntityType)path.EdmType);
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
            KeyValuePathSegment keySegment,
            IEdmEntityType entityType)
        {
            var result = new Dictionary<string, object>();
            var keys = entityType.Key();

            // TODO GitHubIssue#42 : Improve key parsing logic
            // this parsing implementation does not allow key values to contain commas
            // Depending on the WebAPI to make KeyValuePathSegment.Values collection public
            // (or have the parsing logic public).
            string[] values = keySegment.Value.Split(EntityKeySeparator);
            if (values.Length > 1)
            {
                foreach (string value in values)
                {
                    // Split key name and key value
                    var keyValues = value.Split(EntityKeyNameValueSeparator);
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

                var keyName = keys.First().Name;
                result.Add(keyName, ODataUriUtils.ConvertFromUriLiteral(keySegment.Value, ODataVersion.V4));
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
                Convert.ChangeType(propertyValue, property.Type, CultureInfo.InvariantCulture));
            return Expression.Equal(property, constant);
        }

        private static LambdaExpression CreateNotEqualsNullExpression(
            Expression propertyExpression, ParameterExpression parameterExpression)
        {
            var nullConstant = Expression.Constant(null);
            BinaryExpression nullFilterExpression = Expression.NotEqual(propertyExpression, nullConstant);
            var whereExpression = Expression.Lambda(nullFilterExpression, parameterExpression);

            return whereExpression;
        }
        #endregion

        #region Handler Methods
        private void HandleEntitySetPathSegment(ODataPathSegment segment)
        {
            var entitySetPathSegment = (EntitySetPathSegment)segment;
            var entitySet = entitySetPathSegment.EntitySetBase;
            this.currentEntityType = entitySet.EntityType();
            this.queryable = this.api.GetQueryableSource(entitySet.Name, (object[])null);
            this.currentType = this.queryable.ElementType;
        }

        private void HandleSingletonPathSegment(ODataPathSegment segment)
        {
            var singletonPathSegment = (SingletonPathSegment)segment;
            var singleton = singletonPathSegment.Singleton;
            this.currentEntityType = singleton.EntityType();
            this.queryable = this.api.GetQueryableSource(singleton.Name, (object[])null);
            this.currentType = this.queryable.ElementType;
        }

        private void HandleUnboundFunctionPathSegment(ODataPathSegment segment)
        {
            // Nothing will be done
        }

        private void HandleBoundFunctionPathSegment(ODataPathSegment segment)
        {
            // Nothing will be done
        }

        private void HandleCountPathSegment(ODataPathSegment segment)
        {
            this.IsCountPathSegmentPresent = true;
        }

        private void HandleValuePathSegment(ODataPathSegment segment)
        {
            this.IsValuePathSegmentPresent = true;
        }

        private void HandleKeyValuePathSegment(ODataPathSegment segment)
        {
            var keySegment = (KeyValuePathSegment)segment;

            var parameterExpression = Expression.Parameter(this.currentType, DefaultNameOfParameterExpression);
            var keyValues = GetPathKeyValues(keySegment, this.currentEntityType);

            BinaryExpression keyFilter = null;
            foreach (KeyValuePair<string, object> keyValuePair in keyValues)
            {
                var equalsExpression =
                    CreateEqualsExpression(parameterExpression, keyValuePair.Key, keyValuePair.Value);
                keyFilter = keyFilter == null ? equalsExpression : Expression.And(keyFilter, equalsExpression);
            }

            var whereExpression = Expression.Lambda(keyFilter, parameterExpression);
            this.queryable = ExpressionHelpers.Where(this.queryable, whereExpression, this.currentType);
        }

        private void HandleNavigationPathSegment(ODataPathSegment segment)
        {
            var navigationSegment = (NavigationPathSegment)segment;
            var entityParameterExpression = Expression.Parameter(this.currentType);
            var navigationPropertyExpression =
                Expression.Property(entityParameterExpression, navigationSegment.NavigationPropertyName);

            this.currentEntityType = navigationSegment.NavigationProperty.ToEntityType();

            // Check whether property is null or not before futher selection
            var whereExpression =
                CreateNotEqualsNullExpression(navigationPropertyExpression, entityParameterExpression);
            this.queryable = ExpressionHelpers.Where(this.queryable, whereExpression, this.currentType);

            if (navigationSegment.NavigationProperty.TargetMultiplicity() == EdmMultiplicity.Many)
            {
                // get the element type of the target
                // (the type should be an EntityCollection<T> for navigation queries).
                this.currentType = navigationPropertyExpression.Type.GetEnumerableItemType();

                // need to explicitly define the delegate type as IEnumerable<T>
                Type delegateType = typeof(Func<,>).MakeGenericType(
                    queryable.ElementType,
                    typeof(IEnumerable<>).MakeGenericType(this.currentType));
                LambdaExpression selectBody =
                    Expression.Lambda(delegateType, navigationPropertyExpression, entityParameterExpression);

                this.queryable = ExpressionHelpers.SelectMany(this.queryable, selectBody, this.currentType);
            }
            else
            {
                this.currentType = navigationPropertyExpression.Type;
                LambdaExpression selectBody =
                    Expression.Lambda(navigationPropertyExpression, entityParameterExpression);
                this.queryable = ExpressionHelpers.Select(this.queryable, selectBody);
            }
        }

        private void HandlePropertyAccessPathSegment(ODataPathSegment segment)
        {
            var propertySegment = (PropertyAccessPathSegment)segment;
            var entityParameterExpression = Expression.Parameter(this.currentType);
            var structuralPropertyExpression =
                Expression.Property(entityParameterExpression, propertySegment.PropertyName);

            // Check whether property is null or not before futher selection
            if (propertySegment.Property.Type.IsNullable && !propertySegment.Property.Type.IsPrimitive())
            {
                var whereExpression =
                    CreateNotEqualsNullExpression(structuralPropertyExpression, entityParameterExpression);
                this.queryable = ExpressionHelpers.Where(this.queryable, whereExpression, this.currentType);
            }

            if (propertySegment.Property.Type.IsCollection())
            {
                // Produces new query like 'queryable.SelectMany(param => param.PropertyName)'.
                // Suppose 'param.PropertyName' is of type 'IEnumerable<T>', the type of the
                // resulting query would be 'IEnumerable<T>' too.
                this.currentType = structuralPropertyExpression.Type.GetEnumerableItemType();
                var delegateType = typeof(Func<,>).MakeGenericType(
                    this.queryable.ElementType,
                    typeof(IEnumerable<>).MakeGenericType(this.currentType));
                var selectBody =
                    Expression.Lambda(delegateType, structuralPropertyExpression, entityParameterExpression);
                this.queryable = ExpressionHelpers.SelectMany(this.queryable, selectBody, this.currentType);
            }
            else
            {
                // Produces new query like 'queryable.Select(param => param.PropertyName)'.
                this.currentType = structuralPropertyExpression.Type;
                LambdaExpression selectBody =
                    Expression.Lambda(structuralPropertyExpression, entityParameterExpression);
                this.queryable = ExpressionHelpers.Select(this.queryable, selectBody);
            }
        }

        // This only covers entity type cast
        // complex type cast uses ComplexCastPathSegment and is not supported by EF now
        // CLR type is got from model annotation, which means model must include that annotation.
        private void HandleCastPathSegment(ODataPathSegment segment)
        {
            var castSegment = (CastPathSegment)segment;
            var elementType = castSegment.CastType.GetClrType(api);
            this.currentEntityType = castSegment.CastType;
            this.currentType = elementType;
            this.queryable = ExpressionHelpers.OfType(this.queryable, elementType);
        }
        #endregion
    }
}
