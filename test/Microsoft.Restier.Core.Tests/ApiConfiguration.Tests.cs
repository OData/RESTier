// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ApiConfigurationTests
    {
        [Fact]
        public void EmptyConfigurationIsConfiguredCorrectly()
        {
            var configuration = new ApiConfiguration();
            Assert.False(configuration.IsCommitted);
        }

        [Fact]
        public void CachedConfigurationIsCachedCorrectly()
        {
            IApi api = new TestApi();
            var configuration = api.Context.Configuration;

            IApi anotherApi = new TestApi();
            var cached = anotherApi.Context.Configuration;
            Assert.Same(configuration, cached);
        }

        [Fact]
        public void CommittedConfigurationIsConfiguredCorrectly()
        {
            var configuration = new ApiConfiguration();

            configuration.EnsureCommitted();
            Assert.True(configuration.IsCommitted);

            configuration.EnsureCommitted();
            Assert.True(configuration.IsCommitted);
        }

        [Fact]
        public void CommittedConfigurationCannotAddHookHandler()
        {
            var configuration = new ApiConfiguration();
            configuration.EnsureCommitted();

            Assert.Throws<InvalidOperationException>(
                () => configuration.AddHookHandler<IHookHandler>(new TestModelBuilder()));
        }

        [Fact]
        public void ConfigurationCannotAddHookHandlerOfWrongType()
        {
            var configuration = new ApiConfiguration();
            Assert.Throws<InvalidOperationException>(
                () => configuration.AddHookHandler<TestModelBuilder>(new TestModelBuilder()));
        }

        [Fact]
        public void ConfigurationRegistersHookPointsCorrectly()
        {
            var configuration = new ApiConfiguration();
            configuration.EnsureCommitted();

            Assert.Null(configuration.GetHookHandler<IHookA>());
            Assert.Null(configuration.GetHookHandler<IHookB>());

            configuration = new ApiConfiguration();
            var singletonHookPoint = new HookA();
            configuration.AddHookHandler<IHookA>(singletonHookPoint);
            configuration.EnsureCommitted();

            Assert.Same(singletonHookPoint, configuration.GetHookHandler<IHookA>());
            Assert.Null(configuration.GetHookHandler<IHookB>());

            configuration = new ApiConfiguration();
            configuration.AddHookHandler<IHookA>(singletonHookPoint);
            var multiCastHookPoint1 = new HookB();
            configuration.AddHookHandler<IHookB>(multiCastHookPoint1);
            configuration.EnsureCommitted();

            Assert.Same(singletonHookPoint, configuration.GetHookHandler<IHookA>());
            Assert.Equal(multiCastHookPoint1, configuration.GetHookHandler<IHookB>());

            configuration = new ApiConfiguration();
            var multiCastHookPoint2 = new HookB();
            configuration.AddHookHandler<IHookB>(multiCastHookPoint1);
            configuration.AddHookHandler<IHookB>(multiCastHookPoint2);
            configuration.EnsureCommitted();

            var handler = configuration.GetHookHandler<IHookB>();
            Assert.Equal(multiCastHookPoint2, handler);

            var delegateHandler = handler as IDelegateHookHandler<IHookB>;
            Assert.NotNull(delegateHandler);
            Assert.Equal(multiCastHookPoint1, delegateHandler.InnerHandler);
        }

        [Fact]
        public void hookHandlerChainTest()
        {
            var q1 = new HookB("q1Pre", "q1Post");
            var q2 = new HookB("q2Pre", "q2Post");
            var configuration = new ApiConfiguration()
                .AddHookHandler<IHookB>(q1)
                .AddHookHandler<IHookB>(q2);
            configuration.EnsureCommitted();

            var handler = configuration.GetHookHandler<IHookB>();
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

        [Fact]
        public void ContributorsAreCalledCorrectly()
        {
            int i = 0;
            var configuration = new ApiConfiguration()
                .AddContributor<ISomeService>((sp, next) => new SomeService()
                {
                    Next = next(),
                    Value = i++,
                })
                .AddContributor<ISomeService>((sp, next) => new SomeService()
                {
                    Next = next(),
                    Value = i++,
                })
                .ChainPrevious<ISomeService>(next => new SomeService()
                {
                    Next = next,
                    Value = i++,
                })
                .ChainPrevious<ISomeService>((sp, next) => new SomeService()
                {
                    Next = next,
                    Value = i++,
                });

            configuration.EnsureCommitted();
            var value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("3210", value);
        }

        [Fact]
        public void LegacyHookHandlerChainContributor()
        {
            var configuration = new ApiConfiguration()
                .AddContributor<ISomeHook>((sp, next) => new SomeHook()
                {
                    Next = next(),
                    Value = 0,
                })
                .AddHookHandler<ISomeHook>(new SomeDelegateHook()
                {
                    Value = 1,
                });

            configuration.EnsureCommitted();
            var value = configuration.GetHookHandler<ISomeHook>().Call();
            Assert.Equal("10", value);
        }

        [Fact]
        public void ContributorChainLegacyHookHandler()
        {
            var configuration = new ApiConfiguration()
                .AddHookHandler<ISomeHook>(new SomeDelegateHook()
                {
                    Value = 1,
                })
                .AddContributor<ISomeHook>((sp, next) => new SomeHook()
                {
                    Next = next(),
                    Value = 0,
                });

            configuration.EnsureCommitted();
            var value = configuration.GetHookHandler<ISomeHook>().Call();
            Assert.Equal("01", value);
        }

        [Fact]
        public void SharedApiScopeWorksCorrectly()
        {
            var configuration = new ApiConfiguration()
                .MakeScoped<ISomeService>()
                .ChainPrevious<ISomeService>(next => new SomeService());

            configuration.EnsureCommitted();
            var service1 = configuration.GetHookHandler<ISomeService>();

            var context = new ApiContext(configuration);
            var service2 = context.GetApiService<ISomeService>();

            Assert.Equal(service1, service2);
        }

        [Fact]
        public void ContextApiScopeWorksCorrectly()
        {
            var configuration = new ApiConfiguration()
                .UseContextApiScope()
                .MakeScoped<ISomeService>()
                .ChainPrevious<ISomeService>(next => new SomeService());

            configuration.EnsureCommitted();
            var service1 = configuration.GetHookHandler<ISomeService>();

            var context = new ApiContext(configuration);
            var service2 = context.GetApiService<ISomeService>();

            Assert.NotEqual(service1, service2);

            var context3 = new ApiContext(configuration);
            var service3 = context3.GetApiService<ISomeService>();

            Assert.NotEqual(service3, service2);
        }

        interface ISomeService
        {
            string Call();
        }

        interface ISomeHook : ISomeService, IHookHandler
        {
        }

        private interface IHookA : IHookHandler
        {
        }

        class SomeService : ISomeService
        {
            public int Value
            {
                get; set;
            }

            public ISomeService Next
            {
                get; set;
            }

            public string Call()
            {
                if (Next == null)
                {
                    return Value.ToString();
                }

                return Value + Next.Call();
            }
        }

        class SomeHook : SomeService, ISomeHook
        {
        }

        class SomeDelegateHook : SomeHook, IDelegateHookHandler<ISomeHook>
        {
            public ISomeHook InnerHandler
            {
                get { return (ISomeHook)Next; }
                set { Next = value; }
            }
        }

        private class HookA : IHookA
        {
        }

        private interface IHookB : IHookHandler
        {
            string GetStr();
        }

        private class HookB : IHookB, IDelegateHookHandler<IHookB>
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
                var builder = new StringBuilder();
                builder.Append(this.preStr);
                builder.Append("_");

                if (this.InnerHandler != null)
                {
                    builder.Append(this.InnerHandler.GetStr());
                }

                builder.Append(this.postStr);
                builder.Append("_");
                return builder.ToString();
            }
        }
    }
}
