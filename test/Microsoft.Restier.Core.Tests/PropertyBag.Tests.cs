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
            var context =api.Context;

            Assert.False(context.HasProperty("Test"));
            Assert.Null(context.GetProperty("Test"));
            Assert.Null(context.GetProperty<string>("Test"));
            Assert.Equal(default(int), context.GetProperty<int>("Test"));

            context.SetProperty("Test", "Test");
            Assert.True(context.HasProperty("Test"));
            Assert.Equal("Test", context.GetProperty("Test"));
            Assert.Equal("Test", context.GetProperty<string>("Test"));

            context.ClearProperty("Test");
            Assert.False(context.HasProperty("Test"));
            Assert.Null(context.GetProperty("Test"));
            Assert.Null(context.GetProperty<string>("Test"));
            Assert.Equal(default(int), context.GetProperty<int>("Test"));
        }

        [Fact]
        public void DifferentPropertyBagsDoNotConflict()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var context = api.Context;
            context.SetProperty("Test", 2);
            Assert.Equal(2, context.GetProperty<int>("Test"));
        }

        [Fact]
        public void PropertyBagsAreDisposedCorrectly()
        {
            var container = new RestierContainerBuilder(typeof(TestApi));
            var provider = container.BuildContainer();
            var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var scopedProvider  = scope.ServiceProvider;
            var api = scopedProvider.GetService<ApiBase>();
            var context = api.Context;

            Assert.NotNull(context.GetApiService<MyPropertyBag>());
            Assert.Equal(1, MyPropertyBag.InstanceCount);

            var scopedProvider2 = provider.GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider;
            var api2 = scopedProvider2.GetService<ApiBase>();
            var context2 = api2.Context;

            Assert.NotNull(context2.GetApiService<MyPropertyBag>());
            Assert.Equal(2, MyPropertyBag.InstanceCount);

            scope.Dispose();

            Assert.Equal(1, MyPropertyBag.InstanceCount);
        }

        /// <summary>
        /// <see cref="MyPropertyBag"/> has the same lifetime as PropertyBag thus
        /// use this class to test the lifetime of PropertyBag in ApiConfiguration
        /// and ApiContext.
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
