using System.Data.Entity;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet
{

    [TestClass]
    public class ExceptionHandlerTests : RestierTestBase
    {

        [TestMethod]
        public async Task ShouldReturn403HandlerThrowsSecurityException()
        {
            void di(IServiceCollection services)
            {
                services.AddTestStoreApiServices()
                    .AddChainedService<IQueryExpressionSourcer>((sp, next) => new FakeSourcer());
            }
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi, DbContext>(HttpMethod.Get, resource: "/Products", serviceCollection: di);
            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #region Test Resources

        private class FakeSourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                throw new SecurityException();
            }
        }

        #endregion

    }
}
