// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData.Formatter.Deserialization;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Publishers.OData.Formatter
{
    /// <summary>
    /// The default deserializer provider.
    /// </summary>
    public class DefaultRestierDeserializerProvider : DefaultODataDeserializerProvider
    {
        private RestierEnumDeserializer enumDeserializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRestierDeserializerProvider" /> class.
        /// </summary>
        /// <param name="rootContainer">The container to get the service</param>
        public DefaultRestierDeserializerProvider(IServiceProvider rootContainer) : base(rootContainer)
        {
            this.enumDeserializer = new RestierEnumDeserializer();
        }

        /// <inheritdoc />
        public override ODataEdmTypeDeserializer GetEdmTypeDeserializer(IEdmTypeReference edmType)
        {
            if (edmType.IsEnum())
            {
                return this.enumDeserializer;
            }

            return base.GetEdmTypeDeserializer(edmType);
        }
    }
}
