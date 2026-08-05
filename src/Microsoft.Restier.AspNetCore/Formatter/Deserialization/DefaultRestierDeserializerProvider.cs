// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.OData.Edm;
using System;

namespace Microsoft.Restier.AspNetCore.Formatter
{

    /// <summary>
    /// The default deserializer provider.
    /// </summary>
    public class DefaultRestierDeserializerProvider : ODataDeserializerProvider
    {
        private readonly RestierEnumDeserializer enumDeserializer;
        private readonly RestierResourceDeserializer resourceDeserializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRestierDeserializerProvider" /> class.
        /// </summary>
        /// <param name="rootContainer">The container to get the service</param>
        public DefaultRestierDeserializerProvider(IServiceProvider rootContainer) : base(rootContainer)
        {
            enumDeserializer = new RestierEnumDeserializer();
            resourceDeserializer = new RestierResourceDeserializer(this);
        }

        /// <inheritdoc />
        public override IODataEdmTypeDeserializer GetEdmTypeDeserializer(IEdmTypeReference edmType, bool isDelta = false)
        {
            if (edmType.IsEnum())
            {
                return enumDeserializer;
            }

            // Only use RestierResourceDeserializer for non-delta entity/complex types.
            // Delta payloads (PATCH with delta) use OData's built-in delta deserializer
            // which handles property tracking differently.
            if (!isDelta && (edmType.IsEntity() || edmType.IsComplex()))
            {
                return resourceDeserializer;
            }

            return base.GetEdmTypeDeserializer(edmType, isDelta);
        }

    }

}
