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
            public override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                return base.ConfigureApi(services)
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
            using (var api = new TestApi())
            {
                var container = new RestierContainerBuilder(() => new TestApi());
                api.Configuration = new ApiConfiguration(container.BuildContainer());

                var context = api.Context;
                var svc = context.GetApiService<IService>();

                Assert.Same(svc.Api, api);
                Assert.Same(svc.Context, context);

                api.Dispose();
                Assert.Throws<ObjectDisposedException>(() => api.Context);
            }
        }
    }
}
