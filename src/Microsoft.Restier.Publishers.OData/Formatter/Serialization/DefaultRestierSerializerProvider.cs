// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Web.OData;
using System.Web.OData.Formatter.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Publishers.OData.Formatter
{
    /// <summary>
    /// The default serializer provider.
    /// </summary>
    public class DefaultRestierSerializerProvider : DefaultODataSerializerProvider
    {
        private RestierResourceSetSerializer resourceSetSerializer;
        private RestierPrimitiveSerializer primitiveSerializer;
        private RestierRawSerializer rawSerializer;
        private RestierResourceSerializer resourceSerializer;
        private RestierCollectionSerializer collectionSerializer;
        private RestierEnumSerializer enumSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRestierSerializerProvider" /> class.
        /// </summary>
        /// <param name="rootContainer">The container to get the service</param>
        public DefaultRestierSerializerProvider(IServiceProvider rootContainer) : base(rootContainer)
        {
            this.resourceSetSerializer = new RestierResourceSetSerializer(this);
            this.primitiveSerializer = new RestierPrimitiveSerializer();
            this.rawSerializer = new RestierRawSerializer();
            this.resourceSerializer = new RestierResourceSerializer(this);
            this.collectionSerializer = new RestierCollectionSerializer(this);
            this.enumSerializer = new RestierEnumSerializer(this);
        }

        /// <summary>
        /// Gets the serializer for the given result type.
        /// </summary>
        /// <param name="type">The type of result to serialize.</param>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The serializer instance.</returns>
        public override ODataSerializer GetODataPayloadSerializer(
            Type type,
            HttpRequestMessage request)
        {
            ODataSerializer serializer = null;
            if (type == typeof(ResourceSetResult))
            {
                serializer = this.resourceSetSerializer;
            }
            else if (type == typeof(PrimitiveResult))
            {
                serializer = this.primitiveSerializer;
            }
            else if (type == typeof(RawResult))
            {
                serializer = this.rawSerializer;
            }
            else if (type == typeof(ComplexResult))
            {
                serializer = this.resourceSerializer;
            }
            else if (type == typeof(NonResourceCollectionResult))
            {
                serializer = this.collectionSerializer;
            }
            else if (type == typeof(EnumResult))
            {
                serializer = this.enumSerializer;
            }
            else
            {
                serializer = base.GetODataPayloadSerializer(type, request);
            }

            return serializer;
        }

        /// <summary>
        /// Gets the serializer for the given EDM type reference.
        /// </summary>
        /// <param name="edmType">The EDM type reference involved in the serializer.</param>
        /// <returns>The serializer instance.</returns>
        public override ODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType.IsComplex())
            {
                return this.resourceSerializer;
            }

            if (edmType.IsPrimitive())
            {
                return this.primitiveSerializer;
            }

            if (edmType.IsEnum())
            {
                return this.enumSerializer;
            }

            if (edmType.IsCollection())
            {
                var collectionType = edmType.AsCollection();
                if (collectionType.Definition.IsDeltaFeed())
                {
                    return base.GetEdmTypeSerializer(edmType);
                }
                else if (collectionType.ElementType().IsEntity() || collectionType.ElementType().IsComplex())
                {
                    return this.resourceSetSerializer;
                }

                return this.collectionSerializer;
            }

            return base.GetEdmTypeSerializer(edmType);
        }
    }
}
