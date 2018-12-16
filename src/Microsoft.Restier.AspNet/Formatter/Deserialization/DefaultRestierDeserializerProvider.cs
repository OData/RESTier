// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Formatter.Deserialization;
using Microsoft.OData.Edm;
using System;

namespace Microsoft.Restier.AspNet.Formatter
{
    /// <summary>
    /// The default deserializer provider.
    /// </summary>
    public class DefaultRestierDeserializerProvider : DefaultODataDeserializerProvider
    {
        private readonly RestierEnumDeserializer enumDeserializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRestierDeserializerProvider" /> class.
        /// </summary>
        /// <param name="rootContainer">The container to get the service</param>
        public DefaultRestierDeserializerProvider(IServiceProvider rootContainer) : base(rootContainer) => enumDeserializer = new RestierEnumDeserializer();

        /// <inheritdoc />
        public override ODataEdmTypeDeserializer GetEdmTypeDeserializer(IEdmTypeReference edmType)
        {
            if (edmType.IsEnum())
            {
                return enumDeserializer;
            }

            return base.GetEdmTypeDeserializer(edmType);
        }
    }
}
