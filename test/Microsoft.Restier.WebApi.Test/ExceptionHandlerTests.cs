using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Xunit;

namespace Microsoft.Restier.WebApi.Test
{
    public class ExceptionHandlerTests
    {
        private HttpClient client;

        public ExceptionHandlerTests()
        {
            var configuration = new HttpConfiguration();
            configuration.MapODataDomainRoute<ExcDomain>("Exc", "Exc").Wait();
            client = new HttpClient(new HttpServer(configuration));
        }

        [Fact]
        public async Task ShouldReturn403HandlerThrowsSecurityException()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/Exc/Products");
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private class ExcDomain : StoreDomain
        {
            protected override DomainConfiguration CreateDomainConfiguration()
            {
                return base.CreateDomainConfiguration()
                    .AddHookHandler<IQueryExpressionSourcer>(new FakeSourcer());
            }
        }

        private class FakeSourcer : IQueryExpressionSourcer
        {
            public Expression Source(QueryExpressionContext context, bool embedded)
            {
                throw new SecurityException();
            }
        }
    }
}
