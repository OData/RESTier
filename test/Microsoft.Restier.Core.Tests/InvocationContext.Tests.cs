// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class InvocationContextTests
    {
        private class TestApi : ApiBase
        {
            private static ApiServiceA _service;

            public ApiServiceA ApiService
            {
                get
                {
                    if (_service == null)
                    {
                        _service = new ApiServiceA();
                    }
                    return _service;
                }
            }
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                services.AddService<IServiceA>((sp, next) => ApiService);

                return services;
            }
        }

        [Fact]
        public void NewInvocationContextIsConfiguredCorrectly()
        {
            var api = new TestApi();
            var apiContext = api.Context;
            var context = new InvocationContext(apiContext);
            Assert.Same(apiContext, context.ApiContext);
        }

        [Fact]
        public void InvocationContextGetsApiServicesCorrectly()
        {
            var api = new TestApi();
            var apiContext = api.Context;
            var context = new InvocationContext(apiContext);
            Assert.Same(api.ApiService, context.GetApiService<IServiceA>());
        }

        private interface IServiceA
        {
        }

        private class ApiServiceA : IServiceA
        {
        }
    }
}
