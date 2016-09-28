// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ApiConfigurationTests
    {
        [Fact]
        public void ConfigurationRegistersApiServicesCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApiA));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            Assert.Null(api.GetApiService<IServiceA>());
            Assert.Null(api.GetApiService<IServiceB>());

            container = new RestierContainerBuilder(typeof(TestApiB));
            var provider2 = container.BuildContainer();
            var apiB = provider2.GetService<ApiBase>();

            Assert.Same(TestApiB.serviceA, apiB.GetApiService<IServiceA>());

            var serviceBInstance = apiB.GetApiService<ServiceB>();
            var serviceBInterface = apiB.GetApiService<IServiceB>();
            Assert.Equal(serviceBInstance, serviceBInterface);

            // AddService will call services.TryAddTransient
            Assert.Same(serviceBInstance, serviceBInterface);

            var serviceBFirst = serviceBInterface as ServiceB;
            Assert.NotNull(serviceBFirst);

            Assert.Same(TestApiB.serviceB, serviceBFirst.InnerHandler);
        }

        [Fact]
        public void ServiceChainTest()
        {
            var container = new RestierContainerBuilder(typeof(TestApiC));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var handler = api.GetApiService<IServiceB>();
            Assert.Equal("q2Pre_q1Pre_q1Post_q2Post_", handler.GetStr());
        }

        private class TestApiA : ApiBase
        {
            public TestApiA(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestApiB : ApiBase
        {
            private static ServiceA _serviceA;

            private static ServiceB _serviceB;

            public static ServiceA serviceA
            {
                get
                {
                    if (_serviceA == null)
                    {
                        _serviceA = new ServiceA();
                    }
                    return _serviceA;
                }
            }

            public static ServiceB serviceB
            {
                get
                {
                    if (_serviceB == null)
                    {
                        _serviceB = new ServiceB();
                    }
                    return _serviceB;
                }
            }

            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IServiceA>((sp, next) => serviceA);
                services.AddService<IServiceB>((sp, next) => serviceB);
                services.AddService<IServiceB, ServiceB>();
                services.AddSingleton(new ServiceB());
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
                var q1 = new ServiceB("q1Pre", "q1Post");
                var q2 = new ServiceB("q2Pre", "q2Post");
                services.AddService<IServiceB>((sp, next) => q1)
                    .AddService<IServiceB>((sp, next) =>
                    {
                        q2.InnerHandler = next;
                        return q2;
                    });

                return services;
            }

            public TestApiC(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private class TestModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private interface IServiceA
        {
        }

        private class ServiceA : IServiceA
        {
        }

        private interface IServiceB
        {
            string GetStr();
        }

        private class ServiceB : IServiceB
        {
            public IServiceB InnerHandler { get; set; }

            private readonly string preStr;

            private readonly string postStr;

            public ServiceB(string preStr = "DefaultPre", string postStr = "DefaultPost")
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
