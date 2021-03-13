// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.Edm.Vocabularies.V1;
using Microsoft.Restier.Core;
using Microsoft.Restier.AspNet.Model;
using System.Net;

namespace Microsoft.Restier.AspNet
{
    internal static class Extensions
    {
        private const string PropertyNameOfConcurrencyProperties = "ConcurrencyProperties";

        private static PropertyInfo etagConcurrencyPropertiesProperty = typeof(ETag).GetProperty(
            PropertyNameOfConcurrencyProperties, BindingFlags.NonPublic | BindingFlags.Instance);

        // TODO GithubIssue#485 considering move to API class DI instance
        private static readonly ConcurrentDictionary<IEdmEntitySet, bool> concurrencyCheckFlags
            = new ConcurrentDictionary<IEdmEntitySet, bool>();

        // TODO GithubIssue#485 considering move to API class DI instance
        private static readonly ConcurrentDictionary<IEdmStructuredType, IDictionary<string, PropertyAttributes>>
            typePropertiesAttributes
            = new ConcurrentDictionary<IEdmStructuredType, IDictionary<string, PropertyAttributes>>();

        public static void ApplyTo(this ETag etag, IDictionary<string, object> propertyValues)
        {
            if (etag != null)
            {
                var concurrencyProperties = (IDictionary<string, object>)etagConcurrencyPropertiesProperty.GetValue(etag);
                foreach (var item in concurrencyProperties)
                {
                    propertyValues.Add(item.Key, item.Value);
                }
            }
        }

        public static bool IsConcurrencyCheckEnabled(this IEdmModel model, IEdmEntitySet entitySet)
        {
            if (concurrencyCheckFlags.TryGetValue(entitySet, out var needCurrencyCheck))
            {
                return needCurrencyCheck;
            }

            needCurrencyCheck = false;
            var annotations = model.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(
                entitySet, CoreVocabularyModel.ConcurrencyTerm);
            var annotation = annotations.FirstOrDefault();
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

            var propertyValues = new Dictionary<string, object>();
            foreach (var propertyName in entity.GetChangedPropertyNames())
            {
                if (propertiesAttributes != null && propertiesAttributes.TryGetValue(propertyName, out var attributes))
                {
                    if ((isCreation && (attributes & PropertyAttributes.IgnoreForCreation) != PropertyAttributes.None)
                      || (!isCreation && (attributes & PropertyAttributes.IgnoreForUpdate) != PropertyAttributes.None))
                    {
                        // Will not get the properties for update or creation
                        continue;
                    }
                }

                if (entity.TryGetPropertyValue(propertyName, out var value))
                {
                    if (value is EdmComplexObject complexObj)
                    {
                        value = CreatePropertyDictionary(complexObj, complexObj.ActualEdmType, api, isCreation);
                    }

                    //RWM: Other entities are not allowed in the payload until we support Delta payloads.
                    if (value is EdmEntityObject entityObj)
                    {
                        // TODO: RWM: Turn this message into a language resource.
                        throw new StatusCodeException(HttpStatusCode.BadRequest, "Navigation Properties were also present in the payload. Please remove related entities from your request and try again.");
                    }

                    propertyValues.Add(propertyName, value);
                }
            }

            return propertyValues;
        }

        public static IDictionary<string, PropertyAttributes> RetrievePropertiesAttributes(
            IEdmStructuredType edmType, ApiBase api)
        {
            if (typePropertiesAttributes.TryGetValue(edmType, out var propertiesAttributes))
            {
                return propertiesAttributes;
            }

            var model = api.GetModelAsync().Result;
            foreach (var property in edmType.DeclaredProperties)
            {
                var annotations = model.FindVocabularyAnnotations(property);
                var attributes = PropertyAttributes.None;
                foreach (var annotation in annotations)
                {
                    if (!(annotation is EdmVocabularyAnnotation valueAnnotation))
                    {
                        continue;
                    }

                    if (valueAnnotation.Term.IsSameTerm(CoreVocabularyModel.ImmutableTerm))
                    {
                        if (valueAnnotation.Value is EdmBooleanConstant value && value.Value)
                        {
                            attributes |= PropertyAttributes.IgnoreForUpdate;
                        }
                    }

                    if (valueAnnotation.Term.IsSameTerm(CoreVocabularyModel.ComputedTerm))
                    {
                        if (valueAnnotation.Value is EdmBooleanConstant value && value.Value)
                        {
                            attributes |= PropertyAttributes.IgnoreForUpdate;
                            attributes |= PropertyAttributes.IgnoreForCreation;
                        }
                    }

                    // TODO add permission annotation check
                    // CoreVocabularyModel has no permission yet, will add with #480
                }

                // Add property attributes to the dictionary
                if (attributes != PropertyAttributes.None)
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

            return EdmHelpers.GetTypeReference(type, model);
        }

        public static bool IsSameTerm(this IEdmTerm sourceTerm, IEdmTerm targetTerm)
        {
            if (sourceTerm.Namespace == targetTerm.Namespace && sourceTerm.Name == targetTerm.Name)
            {
                return true;
            }

            return false;
        }
    }
}
