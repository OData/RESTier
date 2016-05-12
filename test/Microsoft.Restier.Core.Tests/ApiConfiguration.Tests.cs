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
            ApiBase api = new TestApiA();
            var configuration = api.Context.Configuration;

            ApiBase anotherApi = new TestApiA();
            var cached = anotherApi.Context.Configuration;
            Assert.Same(configuration, cached);
        }

        [Fact]
        public void ConfigurationRegistersApiServicesCorrectly()
        {
            var api = new TestApiA();
            Assert.Null(api.Context.GetApiService<IServiceA>());
            Assert.Null(api.Context.GetApiService<IServiceB>());

            var apiB = new TestApiB();
            
            Assert.Same(apiB.serviceA, apiB.Context.GetApiService<IServiceA>());

            var serviceBInstance = apiB.Context.GetApiService<ServiceB>();
            var serviceBInterface = apiB.Context.GetApiService<IServiceB>();
            Assert.Equal(serviceBInstance, serviceBInterface);

            // AddService will call services.TryAddTransient
            Assert.Same(serviceBInstance, serviceBInterface);

            var serviceBFirst = serviceBInterface as ServiceB;
            Assert.NotNull(serviceBFirst);
            Assert.Same(apiB.serviceB, serviceBFirst.InnerHandler);
        }

        [Fact]
        public void ServiceChainTest()
        {
            var api = new TestApiC();

            var handler = api.Context.GetApiService<IServiceB>();
            Assert.Equal("q2Pre_q1Pre_q1Post_q2Post_", handler.GetStr());
        }

        private class TestApiA : ApiBase
        {
        }

        private class TestApiB : ApiBase
        {
            private ServiceA _serviceA;

            private ServiceB _serviceB;

            public ServiceA serviceA
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

            public ServiceB serviceB
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

            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                services.AddService<IServiceA>((sp, next) => serviceA);
                services.AddService<IServiceB>((sp, next) => serviceB);
                services.AddService<IServiceB, ServiceB>();
                services.AddInstance(new ServiceB());

                return services;
            }
        }
        private class TestApiC : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
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
