// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.OData;
using System.Web.OData.Formatter;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Annotations;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Library.Annotations;
using Microsoft.OData.Edm.Library.Values;
using Microsoft.OData.Edm.Vocabularies.V1;
using Microsoft.Restier.Core;
using Microsoft.Restier.Publishers.OData.Model;

namespace Microsoft.Restier.Publishers.OData
{
    internal static class Extensions
    {
        private const string PropertyNameOfConcurrencyProperties = "ConcurrencyProperties";

        private static PropertyInfo etagConcurrencyPropertiesProperty = typeof(ETag).GetProperty(
            PropertyNameOfConcurrencyProperties, BindingFlags.NonPublic | BindingFlags.Instance);

        private static ConcurrentDictionary<IEdmEntitySet, bool> concurrencyCheckFlags
            = new ConcurrentDictionary<IEdmEntitySet, bool>();

        private static ConcurrentDictionary<IEdmStructuredType, IDictionary<string, PropertyAttributes>>
            typePropertiesAttributes
            = new ConcurrentDictionary<IEdmStructuredType, IDictionary<string, PropertyAttributes>>();

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

        public static bool IsConcurrencyCheckEnabled(this IEdmModel model, IEdmEntitySet entitySet)
        {
            bool needCurrencyCheck;
            if (concurrencyCheckFlags.TryGetValue(entitySet, out needCurrencyCheck))
            {
                return needCurrencyCheck;
            }

            needCurrencyCheck = false;
            var annotations = model.FindVocabularyAnnotations<IEdmValueAnnotation>(
                entitySet, CoreVocabularyModel.ConcurrencyTerm);
            IEdmValueAnnotation annotation = annotations.FirstOrDefault();
            if (annotation != null)
            {
                needCurrencyCheck = true;
            }

            concurrencyCheckFlags[entitySet] = needCurrencyCheck;
            return needCurrencyCheck;
        }

        public static IReadOnlyDictionary<string, object> CreatePropertyDictionary(
            this Delta entity, IEdmStructuredType edmType, ApiBase api, bool isCreation)
        {
            var propertiesAttributes = RetrievePropertiesAttributes(edmType, api);

            Dictionary<string, object> propertyValues = new Dictionary<string, object>();
            foreach (string propertyName in entity.GetChangedPropertyNames())
            {
                PropertyAttributes attributes;
                if (propertiesAttributes != null && propertiesAttributes.TryGetValue(propertyName, out attributes))
                {
                    if ((isCreation && attributes.IgnoreForCreation) || (!isCreation && attributes.IgnoreForUpdate))
                    {
                        // Will not get the properties for update or creation
                        continue;
                    }
                }

                object value;
                if (entity.TryGetPropertyValue(propertyName, out value))
                {
                    var complexObj = value as EdmComplexObject;
                    if (complexObj != null)
                    {
                        value = CreatePropertyDictionary(complexObj, complexObj.ActualEdmType, api, isCreation);
                    }

                    propertyValues.Add(propertyName, value);
                }
            }

            return propertyValues;
        }

        public static IDictionary<string, PropertyAttributes> RetrievePropertiesAttributes(
            IEdmStructuredType edmType, ApiBase api)
        {
            IDictionary<string, PropertyAttributes> propertiesAttributes;
            if (typePropertiesAttributes.TryGetValue(edmType, out propertiesAttributes))
            {
                return propertiesAttributes;
            }

            var model = api.Context.GetModelAsync().Result;
            foreach (var property in edmType.DeclaredProperties)
            {
                var annotations = model.FindVocabularyAnnotations(property);
                PropertyAttributes attributes = null;
                foreach (var annotation in annotations)
                {
                    var valueAnnotation = annotation as EdmAnnotation;
                    if (valueAnnotation == null)
                    {
                        continue;
                    }

                    if (valueAnnotation.Term.Namespace == CoreVocabularyModel.ImmutableTerm.Namespace
                        && valueAnnotation.Term.Name == CoreVocabularyModel.ImmutableTerm.Name)
                    {
                        var value = valueAnnotation.Value as EdmBooleanConstant;
                        if (value != null && value.Value)
                        {
                            if (attributes == null)
                            {
                                attributes = new PropertyAttributes();
                            }

                            attributes.IgnoreForUpdate = true;
                        }
                    }

                    if (valueAnnotation.Term.Namespace == CoreVocabularyModel.ComputedTerm.Namespace
                        && valueAnnotation.Term.Name == CoreVocabularyModel.ComputedTerm.Name)
                    {
                        var value = valueAnnotation.Value as EdmBooleanConstant;
                        if (value != null && value.Value)
                        {
                            if (attributes == null)
                            {
                                attributes = new PropertyAttributes();
                            }

                            attributes.IgnoreForUpdate = true;
                            attributes.IgnoreForCreation = true;
                        }
                    }

                    // TODO add permission annotation check
                    // CoreVocabularyModel has no permission yet, will add with #480
                }

                // Add property attributes to the dictionary
                if (attributes != null)
                {
                    if (propertiesAttributes == null)
                    {
                        propertiesAttributes = new Dictionary<string, PropertyAttributes>();
                        typePropertiesAttributes[edmType] = propertiesAttributes;
                    }

                    propertiesAttributes.Add(property.Name, attributes);
                }
            }

            return propertiesAttributes;
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

        public static IEdmTypeReference GetReturnTypeReference(this Type type, IEdmModel model)
        {
            // In case it is a nullable type, get the underlying type
            type = TypeHelper.GetUnderlyingTypeOrSelf(type);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // if the action returns a Task<T>, map that to just be returning a T
                type = type.GetGenericArguments()[0];
            }
            else if (type == typeof(Task))
            {
                // if the action returns a concrete Task, map that to being a void return type.
                type = typeof(void);
            }

            return GetTypeReference(type, model);
        }

        public static IEdmTypeReference GetTypeReference(this Type type, IEdmModel model)
        {
            Type elementType;
            if (type.TryGetElementType(out elementType))
            {
                return EdmCoreModel.GetCollection(GetTypeReference(elementType, model));
            }

            var edmType = model.FindDeclaredType(type.FullName);

            var enumType = edmType as IEdmEnumType;
            if (enumType != null)
            {
                return new EdmEnumTypeReference(enumType, true);
            }

            var complexType = edmType as IEdmComplexType;
            if (complexType != null)
            {
                return new EdmComplexTypeReference(complexType, true);
            }

            var entityType = edmType as IEdmEntityType;
            if (entityType != null)
            {
                return new EdmEntityTypeReference(entityType, true);
            }

            return type.GetPrimitiveTypeReference();
        }
    }
}
