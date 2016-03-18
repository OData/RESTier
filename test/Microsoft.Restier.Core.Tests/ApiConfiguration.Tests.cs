// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ApiConfigurationTests
    {
        [Fact]
        public void CachedConfigurationIsCachedCorrectly()
        {
            ApiBase api = new TestApi();
            var configuration = api.Context.Configuration;

            ApiBase anotherApi = new TestApi();
            var cached = anotherApi.Context.Configuration;
            Assert.Same(configuration, cached);
        }

        [Fact]
        public void ConfigurationRegistersHookPointsCorrectly()
        {
            IServiceCollection services = new ServiceCollection();
            var configuration = services.BuildApiConfiguration();

            Assert.Null(configuration.GetApiService<IHookA>());
            Assert.Null(configuration.GetApiService<IHookB>());

            var singletonHookPoint = new HookA();
            services.CutoffPrevious<IHookA>(singletonHookPoint);
            configuration = services.BuildApiConfiguration();
            Assert.Same(singletonHookPoint, configuration.GetApiService<IHookA>());
            Assert.Null(configuration.GetApiService<IHookB>());

            var multiCastHookPoint1 = new HookB();
            services.CutoffPrevious<IHookB>(multiCastHookPoint1);
            configuration = services.BuildApiConfiguration();
            Assert.Same(singletonHookPoint, configuration.GetApiService<IHookA>());
            Assert.Equal(multiCastHookPoint1, configuration.GetApiService<IHookB>());

            services = new ServiceCollection()
                .CutoffPrevious<IHookB>(multiCastHookPoint1)
                .ChainPrevious<IHookB, HookB>()
                .AddInstance(new HookB());
            configuration = services.BuildApiConfiguration();
            var multiCastHookPoint2 = configuration.GetApiService<HookB>();
            var handler = configuration.GetApiService<IHookB>();
            Assert.Equal(multiCastHookPoint2, handler);

            var delegateHandler = handler as HookB;
            Assert.NotNull(delegateHandler);
            Assert.Equal(multiCastHookPoint1, delegateHandler.InnerHandler);
        }

        [Fact]
        public void hookHandlerChainTest()
        {
            var q1 = new HookB("q1Pre", "q1Post");
            var q2 = new HookB("q2Pre", "q2Post");
            var configuration = new ServiceCollection()
                .CutoffPrevious<IHookB>(q1)
                .ChainPrevious<IHookB>(next =>
                {
                    q2.InnerHandler = next;
                    return q2;
                }).BuildApiConfiguration();

            var handler = configuration.GetApiService<IHookB>();
            Assert.Equal("q2Pre_q1Pre_q1Post_q2Post_", handler.GetStr());
        }

        private class TestApi : ApiBase
        {
        }

        private class TestModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private interface IHookA
        {
        }

        private class HookA : IHookA
        {
        }

        private interface IHookB
        {
            string GetStr();
        }

        private class HookB : IHookB
        {
            public IHookB InnerHandler { get; set; }

            private readonly string preStr;

            private readonly string postStr;

            public HookB(string preStr = "DefaultPre", string postStr = "DefaultPost")
            {
                this.preStr = preStr;
                this.postStr = postStr;
            }

            public string GetStr()
            {
                var services = new StringBuilder();
                services.Append(this.preStr);
                services.Append("_");

                if (this.InnerHandler != null)
                {
                    services.Append(this.InnerHandler.GetStr());
                }

                services.Append(this.postStr);
                services.Append("_");
                return services.ToString();
            }
        }
    }
}
