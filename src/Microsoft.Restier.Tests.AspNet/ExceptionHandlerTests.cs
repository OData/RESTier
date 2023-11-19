// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore
#else
namespace Microsoft.Restier.Tests.AspNet
#endif
{

    [TestClass]
    public class ExceptionHandlerTests : RestierTestBase
#if NET6_0_OR_GREATER
        <StoreApi>
#endif
    {

        private const string conflictMessage = "Record could not be saved.";
        private const string innerExceptionMessage = "More details about what happened.";
        private const string securityError = "Security error.";
        private const string somethingHappened = "Something happened.";

        [TestMethod]
        public async Task ODataException_Returns403()
        {
            static void di(IServiceCollection services)
            {
                services
                    .AddTestStoreApiServices()
                    .AddChainedService<IQueryExpressionSourcer>((sp, next) => new ODataExceptionSourcer());
            }

            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products", serviceCollection: di);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var result = await response.DeserializeResponseAsync<Product, ODataV4ErrorResponse>();
            result.Should().NotBeNull();
            result.Response.Should().BeNull();
            result.ErrorContent.Should().NotBeNull();
            result.ErrorContent.Error.Message.Should().Be(somethingHappened);
        }

        [TestMethod]
        public async Task ShouldReturn403HandlerThrowsSecurityException()
        {
            static void di(IServiceCollection services)
            {
                services
                    .AddTestStoreApiServices()
                    .AddChainedService<IQueryExpressionSourcer>((sp, next) => new SecurityExceptionSourcer());
            }

            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products", serviceCollection: di);
            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var result = await response.DeserializeResponseAsync<Product, ODataV4ErrorResponse>();
            result.Should().NotBeNull();
            result.Response.Should().BeNull();
            result.ErrorContent.Should().NotBeNull();
            result.ErrorContent.Error.Message.Should().Be(securityError);
        }

        [TestMethod]
        public async Task NullReferenceException_ReturnsProperPayload()
        {
            static void di(IServiceCollection services)
            {
                services
                    .AddTestStoreApiServices()
                    .AddChainedService<IQueryExpressionSourcer>((sp, next) => new NullReferenceExceptionSourcer());
            }

            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products", serviceCollection: di);
            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var result = await response.DeserializeResponseAsync<Product, ODataV4ErrorResponse>();
            result.Should().NotBeNull();
            result.Response.Should().BeNull();
            result.ErrorContent.Should().NotBeNull();
            result.ErrorContent.Error.Message.Should().Contain("magic word");
        }

        #region Test Resources

        /// <summary>
        /// Throws an <see cref="ODataException"/> without an InnerException.
        /// </summary>
        private class ODataExceptionSourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                throw new ODataException(somethingHappened);
            }
        }

        /// <summary>
        /// Throws an <see cref="ODataException"/> with an InnerException.
        /// </summary>
        private class ODataInnerExceptionSourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                throw new ODataException(somethingHappened, new Exception(innerExceptionMessage));
            }
        }

        /// <summary>
        /// Throws a <see cref="SecurityException"/> without any parameters.
        /// </summary>
        private class NullReferenceExceptionSourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                throw new NullReferenceException("Ah ah ah, you didn't say the magic word!");
            }
        }

        /// <summary>
        /// Throws a <see cref="SecurityException"/> without any parameters.
        /// </summary>
        private class SecurityExceptionSourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                throw new SecurityException();
            }
        }

        /// <summary>
        /// Throws a <see cref="SecurityException"/> without any parameters.
        /// </summary>
        private class SecurityExceptionMessageSourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                throw new SecurityException(somethingHappened);
            }
        }

        private class StatusCodeExceptionSourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                throw new StatusCodeException(HttpStatusCode.Conflict, conflictMessage);
            }
        }

        private class StatusCodeInnerExceptionSourcer : IQueryExpressionSourcer
        {
            public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
            {
                throw new StatusCodeException(HttpStatusCode.Conflict, conflictMessage, 
                    new Exception(innerExceptionMessage));
            }
        }

        #endregion


    }
}
