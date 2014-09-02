using System.Collections.Generic;
using System.Web.OData.Domain.Formatter.Serialization;
using System.Web.OData.Formatter;
using System.Web.OData.Formatter.Deserialization;

namespace System.Web.OData.Domain
{
    public class ODataDomainFormattingAttribute : ODataFormattingAttribute
    {
        public override IList<ODataMediaTypeFormatter> CreateODataFormatters()
        {
            return ODataMediaTypeFormatters.Create(
                new DefaultODataDomainSerializerProvider(),
                new DefaultODataDeserializerProvider());
        }
    }
}
