// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Web.OData;
using System.Web.OData.Formatter;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Publishers.OData.Properties;

namespace Microsoft.Restier.Publishers.OData
{
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

        public static Type GetClrType(this IEdmType edmType, ApiBase api)
        {
            IEdmModel edmModel = api.GetModelAsync().Result;

            ClrTypeAnnotation annotation = edmModel.GetAnnotationValue<ClrTypeAnnotation>(edmType);
            if (annotation != null)
            {
                return annotation.ClrType;
            }
            
            throw new NotSupportedException(string.Format(
                CultureInfo.InvariantCulture,
                Resources.ElementTypeNotFound,
                edmType.FullTypeName()));
        }
    }
}
