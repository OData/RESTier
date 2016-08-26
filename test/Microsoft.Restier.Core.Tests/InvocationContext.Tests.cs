// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IServiceA>((sp, next) => ApiService);

                return services;
            }

            public TestApi(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        [Fact]
        public void NewInvocationContextIsConfiguredCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            var context = new InvocationContext(provider);
            Assert.Same(api, context.GetApiService<ApiBase>());
        }

        [Fact]
        public void InvocationContextGetsApiServicesCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var context = new InvocationContext(provider);
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
