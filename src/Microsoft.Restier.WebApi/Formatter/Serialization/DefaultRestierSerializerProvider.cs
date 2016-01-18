// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Edm;
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi.Formatter.Serialization
{
    /// <summary>
    /// The default serializer provider.
    /// </summary>
    internal class DefaultRestierSerializerProvider : DefaultODataSerializerProvider
    {
        private RestierFeedSerializer feedSerializer;
        private RestierEntityTypeSerializer entityTypeSerializer;
        private RestierPrimitiveSerializer primitiveSerializer;
        private RestierRawSerializer rawSerializer;
        private RestierComplexTypeSerializer complexTypeSerializer;
        private RestierCollectionSerializer collectionSerializer;
        private RestierEnumSerializer enumSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRestierSerializerProvider" /> class.
        /// </summary>
        public DefaultRestierSerializerProvider()
        {
            this.feedSerializer = new RestierFeedSerializer(this);
            this.entityTypeSerializer = new RestierEntityTypeSerializer(this);
            this.primitiveSerializer = new RestierPrimitiveSerializer();
            this.rawSerializer = new RestierRawSerializer();
            this.complexTypeSerializer = new RestierComplexTypeSerializer(this);
            this.collectionSerializer = new RestierCollectionSerializer(this);
            this.enumSerializer = new RestierEnumSerializer();
        }

        /// <summary>
        /// Gets the serializer for the given result type.
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
            ODataSerializer serializer = base.GetODataPayloadSerializer(model, type, request);

            if (serializer == null)
            {
                if (type == typeof(EntityCollectionResult))
                {
                    serializer = this.feedSerializer;
                }
                else if (type == typeof(EntityResult))
                {
                    serializer = this.entityTypeSerializer;
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
                    serializer = this.complexTypeSerializer;
                }
                else if (type == typeof(NonEntityCollectionResult))
                {
                    serializer = this.collectionSerializer;
                }
                else if (type == typeof(EnumResult))
                {
                    serializer = this.enumSerializer;
                }
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
            if (edmType.IsEntity())
            {
                return this.entityTypeSerializer;
            }

            if (edmType.IsComplex())
            {
                return this.complexTypeSerializer;
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
                if (edmType.AsCollection().ElementType().IsEntity())
                {
                    return this.feedSerializer;
                }

                return this.collectionSerializer;
            }

            return base.GetEdmTypeSerializer(edmType);
        }
    }
}
