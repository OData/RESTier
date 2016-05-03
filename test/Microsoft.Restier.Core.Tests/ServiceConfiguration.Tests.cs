// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ServiceConfigurationTests
    {
        private class TestApiA : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                var i = 0;
                services.AddService<ISomeService>((sp, next) => new SomeService
                {
                    Inner = next,
                    Value = i++
                })
                    .AddService<ISomeService>((sp, next) => new SomeService
                    {
                        Inner = next,
                        Value = i++
                    })
                    .AddService<ISomeService>((sp, next) => new SomeService
                    {
                        Inner = next,
                        Value = i++
                    })
                    .AddService<ISomeService>((sp, next) => new SomeService
                    {
                        Inner = next,
                        Value = i++
                    })
                    .AddService<ISomeService, SomeService>();

                return services;
            }
        }

        private class TestApiB : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                services.AddService<ISomeService>((sp, next) => new SomeService
                {
                    Inner = next,
                    Value = 1
                })
                    .AddService<ISomeService, SomeService>()
                    .MakeTransient<ISomeService>();

                return services;
            }
        }

        private class TestApiC : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                services.MakeScoped<ISomeService>()
                    .AddService<ISomeService>((sp, next) => new SomeService());
                return services;
            }
        }

        private class TestApiD : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                services.AddService<ISomeService>((sp, next) => new SomeService
                {
                    Inner = next,
                    Value = 1
                })
                    .AddService<ISomeService, SomeServiceNoChain>()
                    .MakeTransient<ISomeService>();
                return services;
            }
        }

        private class TestApiE : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                var first = new SomeService
                {
                    Value = 42
                };
                services.MakeTransient<ISomeService>()
                    .AddService<ISomeService>((sp, next) => first)
                    .AddService<ISomeService, SomeService2>()
                    .AddInstance("Text");

                return services;
            }
        }

        private class TestApiF : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                services.AddService<ISomeService>((sp, next) => new SomeService
                {
                    Value = 2
                })
                    .MakeTransient<ISomeService>()
                    .AddService<ISomeService, SomeService2>();

                return services;
            }
        }

        private class TestApiG : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                services.AddService<ISomeService>((sp, next) => new SomeService
                {
                    Value = 1
                })
                    .AddService<ISomeService, SomeService3>()
                    .AddInstance(new SomeService
                    {
                        Value = 2
                    })
                    .MakeTransient<ISomeService>()
                    .AddService<string>((sp, next) => { return "0"; })
                    .MakeTransient<string>();

                return services;
            }
        }

        private class TestApiH : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                services.AddService<ISomeService>((sp, next) => new SomeService
                {
                    Value = 1
                })
                    .AddService<ISomeService, SomeService3>()
                    .MakeTransient<ISomeService>()
                    .AddService<string>((sp, next) => { return "0"; })
                    .MakeTransient<string>();

                return services;
            }
        }

        private class TestApiI : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                services.MakeTransient<ISomeService>()
                    .AddService<ISomeService>((sp, next) => new SomeService
                    {
                        Value = 2
                    })
                    .AddInstance(new SomeService
                    {
                        Value = 0
                    })
                    .AddService<ISomeService, SomeService4>();

                return services;
            }
        }


        private interface ISomeService
        {
            string Call();
        }

        private class SomeService : ISomeService
        {
            public int Value { get; set; }

            public ISomeService Inner { get; set; }

            public string Call()
            {
                if (Inner == null)
                {
                    return Value.ToString();
                }

                return Value + Inner.Call();
            }
        }

        private class SomeService2 : ISomeService
        {
            // The string value will be retrieved via api.Context.GetApiService<string>()
            public SomeService2(string value = "4")
            {
                Value = value;
            }

            public string Value { get; }

            protected ISomeService Inner { get; set; }

            public string Call()
            {
                if (Inner == null)
                {
                    return Value;
                }

                return Value + Inner.Call();
            }
        }

        private class SomeService3 : ISomeService
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

            public string Value { get; }

            public SomeService Param2 { get; }

            public SomeService Param3 { get; }

            public string Call()
            {
                return Value +
                       (next == null ? string.Empty : next.Call()) +
                       Param2.Call() +
                       Param3.Call();
            }
        }

        private class SomeService4 : SomeService3
        {
            public SomeService4(SomeService dep1)
                : base(dep1)
            {
            }
        }

        private class SomeServiceNoChain : ISomeService
        {
            public string Call()
            {
                return "42";
            }
        }

        [Fact]
        public void ContributorsAreCalledCorrectly()
        {
            var api = new TestApiA();
            var value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("03210", value);
        }

        [Fact]
        public void NextInjectedViaProperty()
        {
            var api = new TestApiB();
            var value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("01", value);

            // Test expression compilation.
            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("01", value);
        }

        [Fact]
        public void ContextApiScopeWorksCorrectly()
        {
            var api = new TestApiC();
            var service1 = api.Context.GetApiService<ISomeService>();

            var api2 = new TestApiC();
            var service2 = api2.Context.GetApiService<ISomeService>();

            Assert.NotEqual(service1, service2);

            var api3 = new TestApiC();
            var service3 = api3.Context.GetApiService<ISomeService>();

            Assert.NotEqual(service3, service2);
        }

        [Fact]
        public void NothingInjectedStillWorks()
        {
            // Outmost service does not call inner service
            var api = new TestApiD();
            var value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);

            // Test expression compilation.
            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);
            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);
        }

        [Fact]
        public void ServiceInjectedViaProperty()
        {
            var api = new TestApiE();

            var expected = "Text42";
            var value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal(expected, value);

            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal(expected, value);

            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal(expected, value);

            Assert.NotEqual(
                api.Context.GetApiService<ISomeService>(),
                api.Context.GetApiService<ISomeService>());
        }

        [Fact]
        public void DefaultValueInConstructorUsedIfNoService()
        {
            var api = new TestApiF();

            var value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);

            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);
            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);
        }


        [Fact]
        public void MultiInjectionViaConstructor()
        {
            var api = new TestApiG();

            var value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("0122", value);

            // Test expression compilation
            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("0122", value);
            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("0122", value);
        }

        [Fact]
        public void ThrowOnNoServiceFound()
        {
            var api = new TestApiH();


            Assert.Throws<InvalidOperationException>(() => { api.Context.GetApiService<ISomeService>(); });
        }

        [Fact]
        public void NextInjectedWithInheritedField()
        {
            var api = new TestApiI();

            var value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("4200", value);

            // Test expression compilation
            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("4200", value);
            value = api.Context.GetApiService<ISomeService>().Call();
            Assert.Equal("4200", value);
        }
    }
}
