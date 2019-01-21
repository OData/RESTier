using System;
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
            var response = await RestierTestHelpers.ExecuteTestRequest<SecurityExceptionApi>(HttpMethod.Get, resource: "/Products");
            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #region Test Resources

        private class SecurityExceptionApi : StoreApi
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                return StoreApi.ConfigureApi(apiType, services)
                    .AddService<IQueryExpressionSourcer>((sp, next) => new FakeSourcer());
            }

            public SecurityExceptionApi(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

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
