﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
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
        public void CachedConfigurationIsCachedCorrectly()
        {
            IApi api = new TestApi();
            var configuration = api.Context.Configuration;

            IApi anotherApi = new TestApi();
            var cached = anotherApi.Context.Configuration;
            Assert.Same(configuration, cached);
        }

        [Fact]
        public void ConfigurationCannotAddHookHandlerOfWrongType()
        {
            var configuration = new ApiBuilder();
            Assert.Throws<InvalidOperationException>(
                () => configuration.AddHookHandler<TestModelBuilder>(new TestModelBuilder()));
        }

        [Fact]
        public void ConfigurationRegistersHookPointsCorrectly()
        {
            var builder = new ApiBuilder();
            var configuration = builder.Build();

            Assert.Null(configuration.GetHookHandler<IHookA>());
            Assert.Null(configuration.GetHookHandler<IHookB>());

            var singletonHookPoint = new HookA();
            builder.AddHookHandler<IHookA>(singletonHookPoint);
            configuration = builder.Build();
            Assert.Same(singletonHookPoint, configuration.GetHookHandler<IHookA>());
            Assert.Null(configuration.GetHookHandler<IHookB>());

            var multiCastHookPoint1 = new HookB();
            builder.AddHookHandler<IHookB>(multiCastHookPoint1);
            configuration = builder.Build();
            Assert.Same(singletonHookPoint, configuration.GetHookHandler<IHookA>());
            Assert.Equal(multiCastHookPoint1, configuration.GetHookHandler<IHookB>());

            var multiCastHookPoint2 = new HookB();
            builder = new ApiBuilder()
                .AddHookHandler<IHookB>(multiCastHookPoint1)
                .AddHookHandler<IHookB>(multiCastHookPoint2);
            configuration = builder.Build();
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
            var configuration = new ApiBuilder()
                .AddHookHandler<IHookB>(q1)
                .AddHookHandler<IHookB>(q2).Build();

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

        private interface IHookA : IHookHandler
        {
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
