// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class PropertyBagTests
    {
        [Fact]
        public void PropertyBagManipulatesPropertiesCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            Assert.False(api.HasProperty("Test"));
            Assert.Null(api.GetProperty("Test"));
            Assert.Null(api.GetProperty<string>("Test"));
            Assert.Equal(default(int), api.GetProperty<int>("Test"));

            api.SetProperty("Test", "Test");
            Assert.True(api.HasProperty("Test"));
            Assert.Equal("Test", api.GetProperty("Test"));
            Assert.Equal("Test", api.GetProperty<string>("Test"));

            api.RemoveProperty("Test");
            Assert.False(api.HasProperty("Test"));
            Assert.Null(api.GetProperty("Test"));
            Assert.Null(api.GetProperty<string>("Test"));
            Assert.Equal(default(int), api.GetProperty<int>("Test"));
        }

        [Fact]
        public void DifferentPropertyBagsDoNotConflict()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            api.SetProperty("Test", 2);
            Assert.Equal(2, api.GetProperty<int>("Test"));
        }

        [Fact]
        public void PropertyBagsAreDisposedCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var scopedProvider  = scope.ServiceProvider;
            var api = scopedProvider.GetService<ApiBase>();

            Assert.NotNull(api.GetApiService<MyPropertyBag>());
            Assert.Equal(1, MyPropertyBag.InstanceCount);

            var scopedProvider2 = provider.GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider;
            var api2 = scopedProvider2.GetService<ApiBase>();

            Assert.NotNull(api2.GetApiService<MyPropertyBag>());
            Assert.Equal(2, MyPropertyBag.InstanceCount);

            scope.Dispose();

            Assert.Equal(1, MyPropertyBag.InstanceCount);
        }

        /// <summary>
        /// <see cref="MyPropertyBag"/> has the same lifetime as PropertyBag thus
        /// use this class to test the lifetime of PropertyBag in ApiConfiguration
        /// and ApiBase.
        /// </summary>
        private class MyPropertyBag : IDisposable
        {
            public MyPropertyBag()
            {
                ++InstanceCount;
            }

            public static int InstanceCount { get; set; }

            public void Dispose()
            {
                --InstanceCount;
            }
        }

        private class TestApi : ApiBase
        {
            public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
            {
                return ApiBase.ConfigureApi(apiType, services).AddScoped<MyPropertyBag>();
            }

            public TestApi(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }
    }
}
