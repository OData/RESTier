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
            public SomeService()
            {
            }

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
                })
                .ChainPrevious<ISomeService, SomeService>();

            var configuration = builder.Build();
            var value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("03210", value);
        }

        [Fact]
        public void NextInjectedViaProperty()
        {
            var builder = new ApiBuilder()
                .AddContributor<ISomeService>((sp, next) => new SomeService()
                {
                    Next = next(),
                    Value = 1,
                })
                .ChainPrevious<ISomeService, SomeService>()
                .MakeTransient<ISomeService>();

            var configuration = builder.Build();
            var value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("01", value);

            // Test expression compilation.
            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("01", value);
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

        class SomeServiceNoChain : ISomeService
        {
            public string Call()
            {
                return "42";
            }
        }

        [Fact]
        public void NothingInjectedStillWorks()
        {
            var builder = new ApiBuilder()
                .AddContributor<ISomeService>((sp, next) => new SomeService()
                {
                    Next = next(),
                    Value = 1,
                })
                .ChainPrevious<ISomeService, SomeServiceNoChain>()
                .MakeTransient<ISomeService>();

            var configuration = builder.Build();
            var value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("42", value);

            // Test expression compilation.
            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("42", value);
            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("42", value);
        }

        class SomeService2 : ISomeService
        {
            protected ISomeService shouldSetProperty = null;

            public SomeService2(string value = "4")
            {
                Value = value;
            }

            public string Value
            {
                get; set;
            }

            protected ISomeService WeirdName
            {
                get; set;
            }

            public string Call()
            {
                if (WeirdName == null)
                {
                    return Value.ToString();
                }

                return Value + WeirdName.Call();
            }
        }

        [Fact]
        public void ServiceInjectedViaProperty()
        {
            var first = new SomeService()
            {
                Value = 42,
            };
            var builder = new ApiBuilder()
                .MakeTransient<ISomeService>()
                .AddContributor<ISomeService>((sp, next) => first)
                .ChainPrevious<ISomeService, SomeService2>()
                .AddInstance<string>("Text");

            var configuration = builder.Build();
            var expected = "Text42";
            var value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal(expected, value);

            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal(expected, value);

            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal(expected, value);

            Assert.NotEqual(
                configuration.GetHookHandler<ISomeService>(),
                configuration.GetHookHandler<ISomeService>());
        }

        [Fact]
        public void DefaultValueInConstructorUsedIfNoService()
        {
            var builder = new ApiBuilder()
                .AddContributor<ISomeService>((sp, next) => new SomeService()
                {
                    Value = 2,
                })
                .MakeTransient<ISomeService>()
                .ChainPrevious<ISomeService, SomeService2>();

            var configuration = builder.Build();
            var value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("42", value);

            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("42", value);
            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("42", value);
        }

        class SomeService3 : ISomeService
        {
            protected ISomeService next = null;

            public SomeService3(string value, SomeService dep1, SomeService dep2)
            {
                Value = value;
                Param2 = dep1;
                Param3 = dep2;
            }

            public SomeService3(SomeService dep1)
            {
                Value = "4";
                Param2 = Param3 = dep1;
            }

            public string Value { get; set; }

            public SomeService Param2 { get; set; }

            public SomeService Param3 { get; set; }

            public string Call()
            {
                return Value +
                    (next == null ? string.Empty : next.Call()) +
                    Param2.Call() +
                    Param3.Call();
            }
        }

        [Fact]
        public void MultiInjectionViaConstructor()
        {
            var builder = new ApiBuilder()
                .AddContributor<ISomeService>((sp, next) => new SomeService()
                {
                    Value = 1,
                })
                .MakeTransient<ISomeService>()
                .AddContributor<string>((sp, next) =>
                {
                    return "0";
                })
                .MakeTransient<string>()
                .ChainPrevious<ISomeService, SomeService3>()
                .AddInstance<SomeService>(new SomeService()
                {
                    Value = 2,
                });

            var configuration = builder.Build();
            var value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("0122", value);

            // Test expression compilation
            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("0122", value);
            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("0122", value);
        }

        [Fact]
        public void ThrowOnNoServiceFound()
        {
            var builder = new ApiBuilder()
                .AddContributor<ISomeService>((sp, next) => new SomeService()
                {
                    Value = 1,
                })
                .MakeTransient<ISomeService>()
                .AddContributor<string>((sp, next) =>
                {
                    return "0";
                })
                .MakeTransient<string>()
                .ChainPrevious<ISomeService, SomeService3>();

            var configuration = builder.Build();
            Assert.Throws<InvalidOperationException>(() =>
            {
                configuration.GetHookHandler<ISomeService>();
            });
        }

        class SomeService4 : SomeService3
        {
            public SomeService4(SomeService dep1)
                : base(dep1)
            {
            }
        }

        [Fact]
        public void NextInjectedWithInheritedField()
        {
            var builder = new ApiBuilder()
                .MakeTransient<ISomeService>()
                .AddContributor<ISomeService>((sp, next) => new SomeService()
                {
                    Value = 2,
                })
                .AddInstance(new SomeService()
                {
                    Value = 0,
                })
                .ChainPrevious<ISomeService, SomeService4>();

            var configuration = builder.Build();
            var value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("4200", value);

            // Test expression compilation
            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("4200", value);
            value = configuration.GetHookHandler<ISomeService>().Call();
            Assert.Equal("4200", value);
        }
    }
}
