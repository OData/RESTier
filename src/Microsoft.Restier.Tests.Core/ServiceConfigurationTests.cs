﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.AspNet;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core
{

    [TestClass]
    public class ServiceConfigurationTests : RestierTestBase
    {

        [TestMethod]
        public async Task ContributorsAreCalledCorrectly()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApiA>();
            var value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("03210");
        }

        [TestMethod]
        public async Task NextInjectedViaProperty()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApiB>();
            var value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("01");
        }

        [TestMethod]
        public async Task ContextApiScopeWorksCorrectly()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApiC>();
            var service1 = api.GetApiService<ISomeService>();

            var api2 = await RestierTestHelpers.GetTestableApiInstance<TestApiC>();
            var service2 = api2.GetApiService<ISomeService>();

            service1.Should().NotBe(service2);

            var api3 = await RestierTestHelpers.GetTestableApiInstance<TestApiC>();
            var service3 = api3.GetApiService<ISomeService>();

            service3.Should().NotBe(service2);
        }

        //RWM: I don't think this actually tests anything of value.
        [TestMethod]
        public async Task NothingInjectedStillWorks()
        {
            // Outmost service does not call inner service
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApiD>();

            var value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("42");

            // Test expression compilation. (RWM: I don't think this works the way they thought it did.)
            value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("42");
            value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("42");
        }

        [TestMethod]
        public void ServiceInjectedViaProperty()
        {
            var container = new RestierContainerBuilder(typeof(TestApiE));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var expected = "Text42";
            var value = api.GetApiService<ISomeService>().Call();
            value.Should().Be(expected);

            value = api.GetApiService<ISomeService>().Call();
            value.Should().Be(expected);

            value = api.GetApiService<ISomeService>().Call();
            value.Should().Be(expected);

            api.GetApiService<ISomeService>().Should().NotBe(api.GetApiService<ISomeService>());
        }

        [TestMethod]
        public void DefaultValueInConstructorUsedIfNoService()
        {
            var container = new RestierContainerBuilder(typeof(TestApiF));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("42");

            value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("42");
            value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("42");
        }


        [TestMethod]
        public void MultiInjectionViaConstructor()
        {
            var container = new RestierContainerBuilder(typeof(TestApiG));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("0122");

            // Test expression compilation
            value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("0122");
            value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("0122");
        }

        [TestMethod]
        public void NextInjectedWithInheritedField()
        {
            var container = new RestierContainerBuilder(typeof(TestApiI));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("4200");

            // Test expression compilation
            value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("4200");
            value = api.GetApiService<ISomeService>().Call();
            value.Should().Be("4200");
        }

        #region Exceptions

        [TestMethod]
        public async Task ThrowOnNoServiceFound()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<TestApiH>();

            Action exceptionTest = () => { api.GetApiService<ISomeService>(); };
            exceptionTest.Should().Throw<InvalidOperationException>();
        }

        #endregion

        #region Test Resources

        private class TestApiA : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();

                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);
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
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();

                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);
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
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();

                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);

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
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();

                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);
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
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();
                var queryExpressionSourcer = new TestQueryExpressionSourcer();

                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);
                services.AddService<IQueryExpressionSourcer>((sp, next) => queryExpressionSourcer);

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
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();
                var queryExpressionSourcer = new TestQueryExpressionSourcer();

                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);
                services.AddService<IQueryExpressionSourcer>((sp, next) => queryExpressionSourcer);


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
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();
                var queryExpressionSourcer = new TestQueryExpressionSourcer();

                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);
                services.AddService<IQueryExpressionSourcer>((sp, next) => queryExpressionSourcer);

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
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();

                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);
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
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();
                var queryExpressionSourcer = new TestQueryExpressionSourcer();

                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);
                services.AddService<IQueryExpressionSourcer>((sp, next) => queryExpressionSourcer);

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

        #endregion


    }

}