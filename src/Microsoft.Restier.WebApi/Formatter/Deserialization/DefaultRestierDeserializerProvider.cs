﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Edm;
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi.Formatter.Deserialization
{
    /// <summary>
    /// The default deserializer provider.
    /// </summary>
    public class DefaultRestierDeserializerProvider : DefaultODataDeserializerProvider
    {
        private RestierEnumDeserializer enumDeserializer;
        private static readonly DefaultRestierDeserializerProvider _instance = new DefaultRestierDeserializerProvider();

        /// <summary>
        /// Gets the default instance of the <see cref="DefaultRestierDeserializerProvider"/>.
        /// </summary>
        internal static DefaultRestierDeserializerProvider SingletonInstance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRestierDeserializerProvider" /> class.
        /// </summary>
        public DefaultRestierDeserializerProvider()
        {
            this.enumDeserializer = new RestierEnumDeserializer();
        }

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
