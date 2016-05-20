using System.Web.Http;

namespace Microsoft.OData.Service.Sample.TrippinInMemory
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
