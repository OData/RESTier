// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ApiBaseTests
    {
        private class TestApi : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                return ApiBase.ConfigureApi(apiType, services)
                    .MakeScoped<IService>()
                    .AddService<IService, Service>();
            }

            public TestApi(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        interface IService
        {
            ApiBase Api { get; }
        }

        class Service : IService
        {
            public ApiBase Api { get; set; }

            public Service(ApiBase api)
            {
                Api = api;
            }
        }

        [Fact]
        public void DefaultApiBaseCanBeCreatedAndDisposed()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            api.Dispose();
        }

        [Fact]
        public void ApiAndApiContextCanBeInjectedByDI()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var svc = api.GetApiService<IService>();

            Assert.Same(svc.Api, api);
        }
    }
}
