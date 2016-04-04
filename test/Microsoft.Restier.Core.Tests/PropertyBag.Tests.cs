// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core.Model;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class PropertyBagTests
    {
        [Fact]
        public void PropertyBagManipulatesPropertiesCorrectly()
        {
            var context = new EmptyApi().Context;

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
            var api = new PropertyBagTestApi(c =>
            {
                // Primarily we want to make sure that this property won't override
                // the one in the ApiContext scope.
                // If unfortunately this happens, the second check below would fail.
                c.SetProperty("Test", 3);
                Assert.Equal(3, c.GetProperty<int>("Test"));
            });
            var context = api.Context;
            var configuration = context.Configuration;

            configuration.SetProperty("Test", 1);
            context.SetProperty("Test", 2);
            context.GetModelAsync().Wait();

            Assert.Equal(1, configuration.GetProperty<int>("Test"));
            Assert.Equal(2, context.GetProperty<int>("Test"));
        }

        [Fact]
        public void PropertyBagsAreDisposedCorrectly()
        {
            var api = new LifeTimeTestApi(c =>
            {
                Assert.NotNull(c.GetApiService<MyPropertyBag>());
                Assert.Equal(3, MyPropertyBag.InstanceCount);
            });
            var context = api.Context;
            var configuration = context.Configuration;

            Assert.NotNull(configuration.GetApiService<MyPropertyBag>());
            Assert.Equal(1, MyPropertyBag.InstanceCount);

            Assert.NotNull(context.GetApiService<MyPropertyBag>());
            Assert.Equal(2, MyPropertyBag.InstanceCount);

            // The InvocationContext scope will be disposed at the end of the call.
            context.GetModelAsync().Wait();

            // The MyPropertyBag instance should also vanish with the InvocationContext
            // scope thus the instance count of MyPropertyBag should remain the same.
            Assert.Equal(2, MyPropertyBag.InstanceCount);

            // This will dispose all the scoped and transient instances registered
            // in the ApiContext scope.
            api.Dispose();

            // The one in ApiConfiguration will NOT be disposed until the service ends.
            Assert.Equal(1, MyPropertyBag.InstanceCount);
        }

        /// <summary>
        /// <see cref="MyPropertyBag"/> has the same lifetime as PropertyBag thus
        /// use this class to test the lifetime of PropertyBag in ApiConfiguration,
        /// ApiContext and InvocationContext.
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

        private class MyModelBuilder : IModelBuilder
        {
            private readonly Action<InvocationContext> testAction;

            public MyModelBuilder(Action<InvocationContext> testAction)
            {
                this.testAction = testAction;
            }

            public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                testAction(context);
                return Task.FromResult<IEdmModel>(new EdmModel());
            }
        }

        private class EmptyApi : ApiBase
        {
        }

        private class PropertyBagTestApi : ApiBase
        {
            private readonly Action<InvocationContext> testAction;

            public PropertyBagTestApi(Action<InvocationContext> testAction)
            {
                this.testAction = testAction;
            }

            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                return base.ConfigureApi(services)
                    .AddScoped<MyPropertyBag>()
                    .CutoffPrevious<IModelBuilder>(new MyModelBuilder(testAction));
            }
        }

        /// <summary>
        /// Use a separate API class to prevent conflicts between test cases
        /// since the model builders of one API only get called once.
        /// </summary>
        private class LifeTimeTestApi : PropertyBagTestApi
        {
            public LifeTimeTestApi(Action<InvocationContext> testAction)
                : base(testAction)
            {
            }
        }
    }
}
