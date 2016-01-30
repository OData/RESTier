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
    public class ApiBuilderTests
    {
        interface ISomeService
        {
            string Call();
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

        [Fact]
        public void ContributorsAreCalledCorrectly()
        {
            int i = 0;
            var builder = new ApiBuilder()
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

            var configuration = builder.Build();
            var value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("3210", value);
        }

        interface ISomeHook : ISomeService, IHookHandler
        {
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

        [Fact]
        public void LegacyHookHandlerChainContributor()
        {
            var builder = new ApiBuilder()
                .AddContributor<ISomeHook>((sp, next) => new SomeHook()
                {
                    Next = next(),
                    Value = 0,
                })
                .AddHookHandler<ISomeHook>(new SomeDelegateHook()
                {
                    Value = 1,
                });

            var configuration = builder.Build();
            var value = configuration.GetHookHandler<ISomeHook>().Call();
            Assert.Equal("10", value);
        }

        [Fact]
        public void ContributorChainLegacyHookHandler()
        {
            var builder = new ApiBuilder()
                .AddHookHandler<ISomeHook>(new SomeDelegateHook()
                {
                    Value = 1,
                })
                .AddContributor<ISomeHook>((sp, next) => new SomeHook()
                {
                    Next = next(),
                    Value = 0,
                });

            var configuration = builder.Build();
            var value = configuration.GetHookHandler<ISomeHook>().Call();
            Assert.Equal("01", value);
        }

        [Fact]
        public void SharedApiScopeWorksCorrectly()
        {
            var builder = new ApiBuilder()
                .TryUseSharedApiScope()
                .MakeScoped<ISomeService>()
                .ChainPrevious<ISomeService>(next => new SomeService());

            var configuration = builder.Build();
            var service1 = configuration.GetHookHandler<ISomeService>();

            var context = new ApiContext(configuration);
            var service2 = context.GetApiService<ISomeService>();

            Assert.Equal(service1, service2);
        }

        [Fact]
        public void ContextApiScopeWorksCorrectly()
        {
            var builder = new ApiBuilder()
                .MakeScoped<ISomeService>()
                .ChainPrevious<ISomeService>(next => new SomeService());

            var configuration = builder.Build();
            var service1 = configuration.GetHookHandler<ISomeService>();

            var context = new ApiContext(configuration);
            var service2 = context.GetApiService<ISomeService>();

            Assert.NotEqual(service1, service2);

            var context3 = new ApiContext(configuration);
            var service3 = context3.GetApiService<ISomeService>();

            Assert.NotEqual(service3, service2);
        }
    }
}
