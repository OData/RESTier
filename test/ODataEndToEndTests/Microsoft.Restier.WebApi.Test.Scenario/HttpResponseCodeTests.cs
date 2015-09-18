using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OData.Client;
using Xunit;

namespace Microsoft.Restier.WebApi.Test.Scenario
{
    public class HttpResponseCodeTests : TrippinE2ETestBase
    {
        [Fact]
        public void ShouldReturn400WhenQueryContainsInvalidOption()
        {
            HttpWebRequest request = WebRequest.CreateHttp(
               new Uri(this.ServiceBaseUri, "People%283%29?$filter=Fi%20eq%20%27er%27"));

            WebResponse response;

            try
            {
                response = request.GetResponse();
            }
            catch (WebException ex)
            {
                response = ex.Response;
            }

            var hr = response as HttpWebResponse;
            Assert.NotNull(hr);
            Assert.Equal(HttpStatusCode.BadRequest, hr.StatusCode);
        }


    }
}
