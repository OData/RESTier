// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class InvocationContextTests
    {
        private class TestApi : ApiBase
        {
            private static ApiServiceA _service;

            public static ApiServiceA ApiService
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

            public new static IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                services.AddService<IServiceA>((sp, next) => ApiService);

                return services;
            }
        }

        [Fact]
        public void NewInvocationContextIsConfiguredCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            api.ServiceProvider = provider;
            var apiContext = api.Context;
            var context = new InvocationContext(apiContext);
            Assert.Same(apiContext, context.ApiContext);
        }

        [Fact]
        public void InvocationContextGetsApiServicesCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            api.ServiceProvider = provider;
            var apiContext = api.Context;
            var context = new InvocationContext(apiContext);
            Assert.Same(TestApi.ApiService, context.GetApiService<IServiceA>());
        }

        private interface IServiceA
        {
        }

        private class ApiServiceA : IServiceA
        {
        }
    }
}
