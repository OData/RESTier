// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.AspNet;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core
{

    [TestClass]
    public class InvocationContextTests : RestierTestBase
    {
        [TestMethod]
        public void InvocationContext_GetsApiServicesCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            var context = new InvocationContext(api);
            context.GetApiService<IServiceA>().Should().BeSameAs(TestApi.ApiService);
        }

        #region Test Resources

        private class TestApi : ApiBase
        {
            private static ApiServiceA _service;

            public static ApiServiceA ApiService
            {
                get
                {
                    if (_service == null)
                    {
                        _service = new ApiServiceA();
                    }
                    return _service;
                }
            }

            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                var changeSetPreparer = new TestChangeSetInitializer();
                var submitExecutor = new TestSubmitExecutor();

                ApiBase.ConfigureApi(apiType, services);
                services.AddService<IChangeSetInitializer>((sp, next) => changeSetPreparer);
                services.AddService<ISubmitExecutor>((sp, next) => submitExecutor);

                services.AddService<IServiceA>((sp, next) => ApiService);

                return services;
            }

            public TestApi(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }

        private interface IServiceA
        {
        }

        private class ApiServiceA : IServiceA
        {
        }

        #endregion

    }
}
