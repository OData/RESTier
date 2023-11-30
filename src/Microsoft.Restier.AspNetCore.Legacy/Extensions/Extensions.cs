// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.Edm.Vocabularies.V1;
#if NET6_0_OR_GREATER
using Microsoft.Restier.AspNetCore.Model;
#else
using Microsoft.Restier.AspNet.Model;
#endif
using Microsoft.Restier.Core;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore
#else
namespace Microsoft.Restier.AspNet
#endif
{
    /// <summary>
    /// An assortment of Extension methods. This might need some refactoring.
    /// </summary>
    internal static class Extensions
    {
        private const string PropertyNameOfConcurrencyProperties = "ConcurrencyProperties";

        private static readonly PropertyInfo EtagConcurrencyPropertiesProperty = typeof(ETag).GetProperty(
            PropertyNameOfConcurrencyProperties, BindingFlags.NonPublic | BindingFlags.Instance);

        // TODO GithubIssue#485 considering move to API class DI instance
        private static readonly ConcurrentDictionary<IEdmEntitySet, bool> ConcurrencyCheckFlags
            = new ConcurrentDictionary<IEdmEntitySet, bool>();

        // TODO GithubIssue#485 considering move to API class DI instance
        private static readonly ConcurrentDictionary<IEdmStructuredType, IDictionary<string, PropertyAttributes>>
            TypePropertiesAttributes
            = new ConcurrentDictionary<IEdmStructuredType, IDictionary<string, PropertyAttributes>>();

        /// <summary>
        /// Adds the Etag value to a dictionary of property values.
        /// </summary>
        /// <param name="etag">The <see cref="ETag"/> value.</param>
        /// <param name="propertyValues">A dictionary of property values.</param>
        public static void ApplyTo(this ETag etag, IDictionary<string, object> propertyValues)
        {
            if (etag is not null)
            {
                var concurrencyProperties = (IDictionary<string, object>)EtagConcurrencyPropertiesProperty.GetValue(etag);
                foreach (var item in concurrencyProperties)
                {
                    propertyValues.Add(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// See whether concurrency checks are enabled for this Entity Set.
        /// </summary>
        /// <param name="model">The Edm Model.</param>
        /// <param name="entitySet">The entity set.</param>
        /// <returns>A boolean indicating whether concurrency checks are enabled.</returns>
        public static bool IsConcurrencyCheckEnabled(this IEdmModel model, IEdmEntitySet entitySet)
        {
            if (ConcurrencyCheckFlags.TryGetValue(entitySet, out var needCurrencyCheck))
            {
                return needCurrencyCheck;
            }

            needCurrencyCheck = false;
            var annotations = model.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(
                entitySet, CoreVocabularyModel.ConcurrencyTerm);
            var annotation = annotations.FirstOrDefault();
            if (annotation is not null)
            {
                needCurrencyCheck = true;
            }

            ConcurrencyCheckFlags[entitySet] = needCurrencyCheck;
            return needCurrencyCheck;
        }

        /// <summary>
        /// Creates a dictionary of changed property values.
        /// </summary>
        /// <param name="entity">The entity inside the delta.</param>
        /// <param name="edmType">The Edm Type.</param>
        /// <param name="api">The api.</param>
        /// <param name="isCreation">Whether this is entity creation or update.</param>
        /// <returns>A dictionary of changed property values.</returns>
        public static IReadOnlyDictionary<string, object> CreatePropertyDictionary(
            this Delta entity, IEdmStructuredType edmType, ApiBase api, bool isCreation)
        {
            var propertiesAttributes = RetrievePropertiesAttributes(edmType, api);

            var propertyValues = new Dictionary<string, object>();
            foreach (var propertyName in entity.GetChangedPropertyNames())
            {
                if (propertiesAttributes is not null && propertiesAttributes.TryGetValue(propertyName, out var attributes))
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

                    // RWM: Other entities are not allowed in the payload until we support Delta payloads.
                    if (value is EdmEntityObject entityObj)
                    {
                        //RWM: This doesn't work because it adds multiple instances of the same tracked entity.
                        //value = CreatePropertyDictionary(entityObj, entityObj.ActualEdmType, api, isCreation);

                        // TODO: RWM: Turn this message into a language resource.
                        throw new StatusCodeException(HttpStatusCode.BadRequest, "Navigation Properties were also present in the payload. Please remove related entities from your request and try again.");
                    }

                    propertyValues.Add(propertyName, value);
                }
            }

            return propertyValues;
        }

        /// <summary>
        /// Gets all the attributes for all properties on an Edm type.
        /// </summary>
        /// <param name="edmType">The Edm type.</param>
        /// <param name="api">The api.</param>
        /// <returns>A dictionary of property attributes.</returns>
        public static IDictionary<string, PropertyAttributes> RetrievePropertiesAttributes(
            IEdmStructuredType edmType, ApiBase api)
        {
            if (TypePropertiesAttributes.TryGetValue(edmType, out var propertiesAttributes))
            {
                return propertiesAttributes;
            }

            var model = api.GetModel();
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
                    if (propertiesAttributes is null)
                    {
                        propertiesAttributes = new Dictionary<string, PropertyAttributes>();
                        TypePropertiesAttributes[edmType] = propertiesAttributes;
                    }

                    propertiesAttributes.Add(property.Name, attributes);
                }
            }

            return propertiesAttributes;
        }

        /// <summary>
        /// Gets the type reference for the return type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="model">The model.</param>
        /// <returns>An <see cref="IEdmTypeReference"/> implementation.</returns>
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

        /// <summary>
        /// Check whether the source term and target term are the same.
        /// </summary>
        /// <param name="sourceTerm">The source term.</param>
        /// <param name="targetTerm">The target term.</param>
        /// <returns>A value indicating whether the two terms are the same.</returns>
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
