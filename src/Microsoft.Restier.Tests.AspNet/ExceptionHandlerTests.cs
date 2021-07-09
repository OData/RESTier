using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET5_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore
#else
namespace Microsoft.Restier.Tests.AspNet
#endif
{

    [TestClass]
    public class ExceptionHandlerTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <StoreApi>
#endif
    {

        [TestMethod]
        public async Task ShouldReturn403HandlerThrowsSecurityException()
        {
            void di(IServiceCollection services)
            {
                services
                    .AddTestStoreApiServices()
                    .AddChainedService<IQueryExpressionSourcer>((sp, next) => new FakeSourcer());
            }
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products", serviceCollection: di);
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
