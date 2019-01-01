using System.Web.Http;

namespace Microsoft.Restier.Samples.Northwind.AspNet
{
#pragma warning disable CA1716 // Identifiers should not match keywords
    public class Global : System.Web.HttpApplication
#pragma warning restore CA1716 // Identifiers should not match keywords
    {

#pragma warning disable CA1707 // Identifiers should not contain underscores
        protected void Application_Start()
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            //AreaRegistration.RegisterAllAreas();
            //AuthorizationConfig.Configure();
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

    }
}