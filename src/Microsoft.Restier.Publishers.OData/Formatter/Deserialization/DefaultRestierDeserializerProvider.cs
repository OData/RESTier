﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Web.OData.Formatter.Deserialization;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Publishers.OData.Formatter.Deserialization
{
    /// <summary>
    /// The default deserializer provider.
    /// </summary>
    public class DefaultRestierDeserializerProvider : DefaultODataDeserializerProvider
    {
        private static readonly DefaultRestierDeserializerProvider SingletonInstanceField
            = new DefaultRestierDeserializerProvider();

        private RestierEnumDeserializer enumDeserializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRestierDeserializerProvider" /> class.
        /// </summary>
        public DefaultRestierDeserializerProvider()
        {
            this.enumDeserializer = new RestierEnumDeserializer();
        }

        /// <summary>
        /// Gets the default instance of the <see cref="DefaultRestierDeserializerProvider"/>.
        /// </summary>
        internal static DefaultRestierDeserializerProvider SingletonInstance
        {
            get
            {
                return SingletonInstanceField;
            }
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
