// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.Restier.Core.Tests
{
    public class ApiBaseTests
    {
        private class TestApi : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                return base.ConfigureApi(services)
                    .MakeScoped<IService>()
                    .CutoffPrevious<IService, Service>();
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

            public override void ConfigureApi(IServiceCollection services, Type type)
            {
                base.ConfigureApi(services, type);
                Assert.Same(typeof(TestApiWithParticipants), type);
                services.ChainPrevious<Dictionary<string, object>>((sp, next) =>
                {
                    if (next == null)
                    {
                        next = new Dictionary<string, object>();
                    }

                    next.Add(this.Value, true);
                    return next;
                })
                .MakeScoped<Dictionary<string, object>>();
            }

            public override void Initialize(
                ApiContext context,
                Type type, object instance)
            {
                base.Initialize(context, type, instance);
                Assert.Same(typeof(TestApiWithParticipants), type);
                context.GetApiService<Dictionary<string, object>>()
                    .Add(this.Value + ".Self", instance);
            }

            public override void Dispose(
                ApiContext context,
                Type type, object instance)
            {
                Assert.Same(typeof(TestApiWithParticipants), type);
                context.GetApiService<Dictionary<string, object>>()
                    .Remove(this.Value);
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
            Assert.True((bool)configuration.GetApiService<Dictionary<string, object>>()["Test1"]);
            Assert.True((bool)configuration.GetApiService<Dictionary<string, object>>()["Test2"]);

            var context = api.Context;
            var dict = context.GetApiService<Dictionary<string, object>>();
            Assert.NotSame(dict, configuration.GetApiService<Dictionary<string, object>>());
            Assert.True((bool)dict["Test1"]);
            Assert.Same(api, dict["Test1.Self"]);
            Assert.True((bool)dict["Test2"]);
            Assert.Same(api, dict["Test2.Self"]);

            (api as IDisposable).Dispose();
            Assert.False(dict.ContainsKey("Test2"));
            Assert.False(dict.ContainsKey("Test1"));
        }
    }
}
