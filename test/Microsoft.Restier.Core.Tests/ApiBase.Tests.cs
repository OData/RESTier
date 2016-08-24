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
        }

        interface IService
        {
            ApiBase Api { get; }

            ApiContext Context { get; }
        }

        class Service : IService
        {
            public ApiBase Api { get; set; }

            public ApiContext Context { get; set; }

            public Service(ApiBase api, ApiContext context)
            {
                Api = api;
                Context = context;
            }
        }

        [Fact]
        public void DefaultApiBaseCanBeCreatedAndDisposed()
        {
            using (var api = new TestApi())
            {
                api.Dispose();
            }
        }

        [Fact]
        public void ApiAndApiContextCanBeInjectedByDI()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            api.ServiceProvider = provider;

            // TODO, this will create a new scope and a new provider....
            var context = api.Context;
            var svc = context.GetApiService<IService>();

            Assert.Same(svc.Api, api);
            Assert.Same(svc.Context, context);

            api.Dispose();
            Assert.Throws<ObjectDisposedException>(() => api.Context);
        }
    }
}
