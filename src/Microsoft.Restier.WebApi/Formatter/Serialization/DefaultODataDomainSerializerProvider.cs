// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Edm;
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi.Formatter.Serialization
{
    public class DefaultODataDomainSerializerProvider : DefaultODataSerializerProvider
    {
        private ODataDomainFeedSerializer feedSerializer;
        private ODataDomainEntityTypeSerializer entityTypeSerializer;

        public DefaultODataDomainSerializerProvider()
        {
            this.feedSerializer = new ODataDomainFeedSerializer(this);
            this.entityTypeSerializer = new ODataDomainEntityTypeSerializer(this);
        }

        public override ODataSerializer GetODataPayloadSerializer(IEdmModel model, Type type, HttpRequestMessage request)
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
            }

            return serializer;
        }

        public override ODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType.IsEntity())
            {
                return this.entityTypeSerializer;
            }
            else
            {
                return base.GetEdmTypeSerializer(edmType);
            }
        }
    }
}
