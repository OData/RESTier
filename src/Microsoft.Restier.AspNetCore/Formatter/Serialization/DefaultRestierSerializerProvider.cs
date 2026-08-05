// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.OData;
using Microsoft.OData.Edm;
using System;


namespace Microsoft.Restier.AspNetCore.Formatter
{
    /// <summary>
    /// The default serializer provider.
    /// </summary>
    public class DefaultRestierSerializerProvider : ODataSerializerProvider
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
        /// <param name="serviceProvider">The container to get the service.</param>
        /// <param name="payloadValueConverter">The OData payload value converter to use.</param>
        public DefaultRestierSerializerProvider(IServiceProvider serviceProvider, ODataPayloadValueConverter payloadValueConverter)
            : base(serviceProvider)
        {
            Ensure.NotNull(serviceProvider, nameof(serviceProvider));
            Ensure.NotNull(payloadValueConverter, nameof(payloadValueConverter));

            resourceSetSerializer = new RestierResourceSetSerializer(this);
            primitiveSerializer = new RestierPrimitiveSerializer(payloadValueConverter);
            rawSerializer = new RestierRawSerializer(payloadValueConverter);
            resourceSerializer = new RestierResourceSerializer(this);
            collectionSerializer = new RestierCollectionSerializer(this);
            enumSerializer = new RestierEnumSerializer(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRestierSerializerProvider" /> class.
        /// </summary>
        /// <param name="serviceProvider">The container to get the service.</param>
        public DefaultRestierSerializerProvider(IServiceProvider serviceProvider)
            : this(serviceProvider, new RestierPayloadValueConverter())
        {
        }

        /// <summary>
        /// Gets the serializer for the given result type.
        /// </summary>
        /// <param name="type">The type of result to serialize.</param>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The serializer instance.</returns>
        public override IODataSerializer GetODataPayloadSerializer(
            Type type,
            HttpRequest request)
        {
            IODataSerializer serializer = null;
            if (type == typeof(ResourceSetResult))
            {
                serializer = resourceSetSerializer;
            }
            else if (type == typeof(PrimitiveResult))
            {
                serializer = primitiveSerializer;
            }
            else if (type == typeof(RawResult))
            {
                serializer = rawSerializer;
            }
            else if (type == typeof(ComplexResult))
            {
                serializer = resourceSerializer;
            }
            else if (type == typeof(NonResourceCollectionResult))
            {
                serializer = collectionSerializer;
            }
            else if (type == typeof(EnumResult))
            {
                serializer = enumSerializer;
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
        public override IODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType.IsComplex())
            {
                return resourceSerializer;
            }

            if (edmType.IsPrimitive())
            {
                return primitiveSerializer;
            }

            if (edmType.IsEnum())
            {
                return enumSerializer;
            }

            if (edmType.IsCollection())
            {
                var collectionType = edmType.AsCollection();

                // TODO: Fix Deltafeed
                /* if (collectionType.Definition.IsDeltaFeed())
                {
                    return base.GetEdmTypeSerializer(edmType);
                }
                else*/ if (collectionType.ElementType().IsEntity() || collectionType.ElementType().IsComplex())
                {
                    return resourceSetSerializer;
                }

                return collectionSerializer;
            }

            return base.GetEdmTypeSerializer(edmType);
        }
    }
}
