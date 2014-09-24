using Microsoft.OData.Client;

namespace System.Web.OData.Domain.Test.Scenario
{
    /// <summary>
    /// Summary description for E2ETestBase
    /// </summary>
    public class E2ETestBase<TDSC> where TDSC : DataServiceContext
    {
        protected Uri ServiceBaseUri { get; set; }
        public TDSC TestClientContext;
        public E2ETestBase(Uri serviceBaseUri)
        {
            this.ServiceBaseUri = serviceBaseUri;
            TestClientContext = Activator.CreateInstance(typeof(TDSC), this.ServiceBaseUri) as TDSC;
        }

        protected void ResetDataSource()
        {
            this.TestClientContext.Execute(new Uri("/ResetDataSource", UriKind.Relative), "POST");
        }
    }
}
