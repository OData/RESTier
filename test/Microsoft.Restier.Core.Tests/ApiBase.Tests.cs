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
                var context = api.Context;
                var svc = context.GetApiService<IService>();

                Assert.Same(svc.Api, api);
                Assert.Same(svc.Context, context);

                api.Dispose();
                Assert.Throws<ObjectDisposedException>(() => api.Context);
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        private class TestApiConfiguratorAttribute :
            ApiConfiguratorAttribute
        {
            public TestApiConfiguratorAttribute(string value)
            {
                this.Value = value;
            }

            public string Value { get; private set; }

            public override void UpdateApiConfiguration(
                ApiConfiguration configuration,
                Type type)
            {
                base.UpdateApiConfiguration(configuration, type);
                Assert.Same(typeof(TestApiWithParticipants), type);
                configuration.SetProperty(this.Value, true);
            }

            public override void UpdateApiContext(
                ApiContext context,
                Type type, object instance)
            {
                base.UpdateApiContext(context, type, instance);
                Assert.Same(typeof(TestApiWithParticipants), type);
                context.SetProperty(this.Value + ".Self", instance);
                context.SetProperty(this.Value, true);
            }

            public override void Dispose(
                ApiContext context,
                Type type, object instance)
            {
                Assert.Same(typeof(TestApiWithParticipants), type);
                context.SetProperty(this.Value, false);
                base.Dispose(context, type, instance);
            }
        }

        [TestApiConfigurator("Test1")]
        [TestApiConfigurator("Test2")]
        private class TestApiWithParticipants : ApiBase
        {
        }

        [Fact]
        public void TestApiAppliesApiParticipantsCorrectly()
        {
            ApiBase api = new TestApiWithParticipants();

            var configuration = api.Context.Configuration;
            Assert.True(configuration.GetProperty<bool>("Test1"));
            Assert.True(configuration.GetProperty<bool>("Test2"));

            var context = api.Context;
            Assert.True(context.GetProperty<bool>("Test1"));
            Assert.Same(api, context.GetProperty("Test1.Self"));
            Assert.True(context.GetProperty<bool>("Test2"));
            Assert.Same(api, context.GetProperty("Test2.Self"));

            (api as IDisposable).Dispose();
            Assert.False(context.GetProperty<bool>("Test2"));
            Assert.False(context.GetProperty<bool>("Test1"));
        }
    }
}
