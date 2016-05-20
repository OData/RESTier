// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Web.OData.Formatter.Deserialization;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Publisher.OData.Formatter.Deserialization
{
    /// <summary>
    /// The deserializer provider proxy which get real provider to implement the logic.
    /// </summary>
    internal class RestierDeserializerProviderProxy : ODataDeserializerProvider
    {
        private ApiBase api;

        /// <summary>
        /// Gets an <see cref="ODataDeserializer"/> for the given type.
        /// The proxy provider will get the real provider to return the serializer.
        /// </summary>
        /// <param name="model">The EDM model.</param>
        /// <param name="type">The CLR type.</param>
        /// <param name="request">The request being deserialized.</param>
        /// <returns>An <see cref="ODataDeserializer"/> that can deserialize the given type.</returns>
        public override ODataDeserializer GetODataDeserializer(IEdmModel model, Type type, HttpRequestMessage request)
        {
            this.api = request.GetApiInstance();

            if (this.api != null)
            {
                ODataDeserializerProvider provider = api.Context.GetApiService<ODataDeserializerProvider>();
                if (provider != null)
                {
                    return provider.GetODataDeserializer(model, type, request);
                }
            }

            // In case user uses his own controller or NonFound error for request
            return DefaultRestierDeserializerProvider.SingletonInstance.GetODataDeserializer(model, type, request);
        }

        /// <summary>
        /// Gets the <see cref="ODataEdmTypeDeserializer"/> for the given EDM type.
        /// The proxy provider will get the real provider to return the serializer.
        /// </summary>
        /// <param name="edmType">The EDM type.</param>
        /// <returns>An <see cref="ODataEdmTypeDeserializer"/> that can deserialize the given EDM type.</returns>
        public override ODataEdmTypeDeserializer GetEdmTypeDeserializer(IEdmTypeReference edmType)
        {
            if (this.api != null)
            {
                ODataDeserializerProvider provider = api.Context.GetApiService<ODataDeserializerProvider>();
                if (provider != null)
                {
                    return provider.GetEdmTypeDeserializer(edmType);
                }
            }

            // In case user uses his own controller or NonFound error for request
            return DefaultRestierDeserializerProvider.SingletonInstance.GetEdmTypeDeserializer(edmType);
        }
    }
}
