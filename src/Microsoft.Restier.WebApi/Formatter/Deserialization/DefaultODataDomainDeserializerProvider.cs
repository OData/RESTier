// Copyright (c) Microsoft Corporation.  All rights reserved.
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
    /// The default serializer provider.
    /// </summary>
    internal class DefaultODataDomainDeserializerProvider : DefaultODataDeserializerProvider
    {
        private ODataDomainEnumDeserializer enumDeserializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultODataDomainDeserializerProvider" /> class.
        /// </summary>
        public DefaultODataDomainDeserializerProvider()
        {
            this.enumDeserializer = new ODataDomainEnumDeserializer();
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
