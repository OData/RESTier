using System.Web.Http;

namespace Microsoft.Restier.Samples.Northwind.AspNet
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start()
        {
            //AreaRegistration.RegisterAllAreas();
            //AuthorizationConfig.Configure();
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

    }
}