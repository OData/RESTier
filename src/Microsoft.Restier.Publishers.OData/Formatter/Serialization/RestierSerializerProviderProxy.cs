// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Publishers.OData.Formatter.Serialization
{
    /// <summary>
    /// The serializer provider proxy which get real provider to implement the logic.
    /// </summary>
    internal class RestierSerializerProviderProxy : ODataSerializerProvider
    {
        private ApiBase api;

        /// <summary>
        /// Gets the serializer for the given result type.
        /// The proxy provider will get the real provider to return the serializer.
        /// </summary>
        /// <param name="model">The EDM model.</param>
        /// <param name="type">The type of result to serialize.</param>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The serializer instance.</returns>
        public override ODataSerializer GetODataPayloadSerializer(
            IEdmModel model,
            Type type,
            HttpRequestMessage request)
        {
            this.api  = request.GetApiInstance();

            if (this.api != null)
            {
                ODataSerializerProvider provider = api.Context.GetApiService<ODataSerializerProvider>();
                if (provider != null)
                {
                    return provider.GetODataPayloadSerializer(model, type, request);
                }
            }

            // In case user uses his own controller or NonFound error for request
            return DefaultRestierSerializerProvider.SingletonInstance.GetODataPayloadSerializer(model, type, request);
        }

        /// <summary>
        /// Gets the serializer for the given EDM type reference.
        /// The proxy provider will get the real provider to return the serializer.
        /// </summary>
        /// <param name="edmType">The EDM type reference involved in the serializer.</param>
        /// <returns>The serializer instance.</returns>
        public override ODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (this.api != null)
            {
                ODataSerializerProvider provider = api.Context.GetApiService<ODataSerializerProvider>();
                if (provider != null)
                {
                    return provider.GetEdmTypeSerializer(edmType);
                }
            }

            // In case user uses his own controller or NonFound error for request
            return DefaultRestierSerializerProvider.SingletonInstance.GetEdmTypeSerializer(edmType);
        }
    }
}
