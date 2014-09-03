using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.OData.Domain.Formatter.Serialization;
using System.Web.OData.Formatter;
using System.Web.OData.Formatter.Deserialization;

namespace System.Web.OData.Domain
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ODataDomainFormattingAttribute : Attribute, IControllerConfiguration
    {
        public void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
        {
            var controllerFormatters = controllerSettings.Formatters;
            IList<ODataMediaTypeFormatter> odataFormatters =
                controllerFormatters.OfType<ODataMediaTypeFormatter>().ToList();
            if (!odataFormatters.Any())
            {
                foreach (var formatter in odataFormatters)
                {
                    controllerFormatters.Remove(formatter);
                }
            }

            odataFormatters = ODataMediaTypeFormatters.Create(
                new DefaultODataDomainSerializerProvider(),
                new DefaultODataDeserializerProvider());
            controllerFormatters.InsertRange(0, odataFormatters);
        }
    }
}
