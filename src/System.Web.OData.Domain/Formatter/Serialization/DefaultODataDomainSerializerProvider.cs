using System.Net.Http;
using System.Web.OData.Domain.Results;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Edm;

namespace System.Web.OData.Domain.Formatter.Serialization
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
            if (edmType.Definition.TypeKind == EdmTypeKind.Entity)
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
