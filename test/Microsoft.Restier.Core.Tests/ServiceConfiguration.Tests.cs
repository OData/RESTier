// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ServiceConfigurationTests
    {
        private class TestApiA : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
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

            public TestApiA(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestApiB : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
                services.AddService<ISomeService>((sp, next) => new SomeService
                {
                    Inner = next,
                    Value = 1
                })
                    .AddService<ISomeService, SomeService>()
                    .MakeTransient<ISomeService>();
                return services;
            }

            public TestApiB(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestApiC : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
                services.MakeScoped<ISomeService>()
                    .AddService<ISomeService>((sp, next) => new SomeService());
                return services;
            }

            public TestApiC(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestApiD : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
                services.AddService<ISomeService>((sp, next) => new SomeService
                {
                    Inner = next,
                    Value = 1
                })
                    .AddService<ISomeService, SomeServiceNoChain>()
                    .MakeTransient<ISomeService>();
                return services;
            }

            public TestApiD(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestApiE : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
                var first = new SomeService
                {
                    Value = 42
                };
                services.MakeTransient<ISomeService>()
                    .AddService<ISomeService>((sp, next) => first)
                    .AddService<ISomeService, SomeService2>()
                    .AddSingleton("Text");
                return services;
            }

            public TestApiE(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestApiF : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
                services.AddService<ISomeService>((sp, next) => new SomeService
                {
                    Value = 2
                })
                    .MakeTransient<ISomeService>()
                    .AddService<ISomeService, SomeService2>();

                return services;
            }

            public TestApiF(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestApiG : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
                services.AddService<ISomeService>((sp, next) => new SomeService
                {
                    Value = 1
                })
                    .AddService<ISomeService, SomeService3>()
                    .AddSingleton(new SomeService
                    {
                        Value = 2
                    })
                    .MakeTransient<ISomeService>()
                    .AddService<string>((sp, next) => { return "0"; })
                    .MakeTransient<string>();
                return services;
            }

            public TestApiG(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestApiH : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
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

            public TestApiH(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestApiI : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
                services.MakeTransient<ISomeService>()
                    .AddService<ISomeService>((sp, next) => new SomeService
                    {
                        Value = 2
                    })
                    .AddSingleton(new SomeService
                    {
                        Value = 0
                    })
                    .AddService<ISomeService, SomeService4>();

                return services;
            }

            public TestApiI(IServiceProvider serviceProvider) : base(serviceProvider)
            {
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
            private string _value;
            // The string value will be retrieved via api.GetApiService<string>()
            public SomeService2(string value = "4")
            {
                _value = value;
            }

            public string Value
            {
                get
                {
                    return _value;
                }
            }

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
            private string _value;
            private SomeService _service1;
            private SomeService _service2;

            protected ISomeService next = null;

            public SomeService3(string value, SomeService dep1, SomeService dep2)
            {
                _value = value;
                _service1 = dep1;
                _service2 = dep2;
            }

            public SomeService3(SomeService dep1)
            {
                _value = "4";
                _service1 = _service2 = dep1;
            }

            public string Value
            {
                get
                {
                    return _value;
                }
            }

            public SomeService Param2
            {
                get
                {
                    return _service1;
                }
            }

            public SomeService Param3
            {
                get
                {
                    return _service2;
                }
            }

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
            var container = new RestierContainerBuilder(typeof(TestApiA));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            var value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("03210", value);
        }

        [Fact]
        public void NextInjectedViaProperty()
        {
            var container = new RestierContainerBuilder(typeof(TestApiB));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            var value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("01", value);

            // Test expression compilation.
            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("01", value);
        }

        [Fact]
        public void ContextApiScopeWorksCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApiC));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var service1 = api.GetApiService<ISomeService>();
            
            container = new RestierContainerBuilder(typeof(TestApiC));
            provider = container.BuildContainer();
            var api2 = provider.GetService<ApiBase>();

            var service2 = api2.GetApiService<ISomeService>();

            Assert.NotEqual(service1, service2);

            container = new RestierContainerBuilder(typeof(TestApiC));
            provider = container.BuildContainer();
            var api3 = provider.GetService<ApiBase>();
            var service3 = api3.GetApiService<ISomeService>();

            Assert.NotEqual(service3, service2);
        }

        [Fact]
        public void NothingInjectedStillWorks()
        {
            // Outmost service does not call inner service
            var container = new RestierContainerBuilder(typeof(TestApiD));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);

            // Test expression compilation.
            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);
            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);
        }

        [Fact]
        public void ServiceInjectedViaProperty()
        {
            var container = new RestierContainerBuilder(typeof(TestApiE));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var expected = "Text42";
            var value = api.GetApiService<ISomeService>().Call();
            Assert.Equal(expected, value);

            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal(expected, value);

            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal(expected, value);

            Assert.NotEqual(
                api.GetApiService<ISomeService>(),
                api.GetApiService<ISomeService>());
        }

        [Fact]
        public void DefaultValueInConstructorUsedIfNoService()
        {
            var container = new RestierContainerBuilder(typeof(TestApiF));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);

            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);
            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("42", value);
        }


        [Fact]
        public void MultiInjectionViaConstructor()
        {
            var container = new RestierContainerBuilder(typeof(TestApiG));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("0122", value);

            // Test expression compilation
            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("0122", value);
            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("0122", value);
        }

        [Fact]
        public void ThrowOnNoServiceFound()
        {
            var container = new RestierContainerBuilder(typeof(TestApiH));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            Assert.Throws<InvalidOperationException>(() => { api.GetApiService<ISomeService>(); });
        }

        [Fact]
        public void NextInjectedWithInheritedField()
        {
            var container = new RestierContainerBuilder(typeof(TestApiI));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("4200", value);

            // Test expression compilation
            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("4200", value);
            value = api.GetApiService<ISomeService>().Call();
            Assert.Equal("4200", value);
        }
    }
}
