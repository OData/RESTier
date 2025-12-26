// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace Microsoft.Restier.Tests.Core.Model
{

    [TestClass]
    public class DefaultModelHandlerTests
#if NET6_0_OR_GREATER
        : RestierTestBase<TestableEmptyApi>
#else
        : RestierTestBase
#endif
    {

        void addTestServices(IServiceCollection services)
        {
            services.AddChainedService<IChangeSetInitializer>((sp, next) => new StoreChangeSetInitializer())
                .AddChainedService<ISubmitExecutor>((sp, next) => new DefaultSubmitExecutor())
                .AddChainedService<IQueryExpressionSourcer>((sp, next) => new StoreQueryExpressionSourcer());
        }

        [TestMethod]
        public async Task GetModelUsingDefaultModelHandler()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<TestableEmptyApi>(serviceCollection: (services) =>
            {
                addTestServices(services);
                services.AddChainedService<IModelBuilder>((sp, next) => new TestModelProducer())
                    .AddChainedService<IModelBuilder>((sp, next) => new TestModelExtender(2)
                    {
                        InnerHandler = next,
                    })
                    .AddChainedService<IModelBuilder>((sp, next) => new TestModelExtender(3)
                    {
                        InnerHandler = next,
                    });
            });
            model.SchemaElements.Should().HaveCount(4);
            model.SchemaElements.SingleOrDefault(e => e.Name == "TestName").Should().NotBeNull();
            model.SchemaElements.SingleOrDefault(e => e.Name == "TestName2").Should().NotBeNull();
            model.SchemaElements.SingleOrDefault(e => e.Name == "TestName3").Should().NotBeNull();
            model.EntityContainer.Should().NotBeNull();
            model.EntityContainer.Elements.SingleOrDefault(e => e.Name == "TestEntitySet").Should().NotBeNull();
            model.EntityContainer.Elements.SingleOrDefault(e => e.Name == "TestEntitySet2").Should().NotBeNull();
            model.EntityContainer.Elements.SingleOrDefault(e => e.Name == "TestEntitySet3").Should().NotBeNull();
        }

        [TestMethod]
        public async Task ModelBuilderShouldBeCalledOnlyOnceIfSucceeded()
        {
            using var wait = new ManualResetEventSlim(false);
            for (var i = 0; i < 2; i++)
            {
                var container = new RestierContainerBuilder(builder =>
                {
                    builder.AddRestierApi<TestableEmptyApi>(services =>
                    {
                        services.AddChainedService<IModelBuilder>((sp, next) => new TestSingleCallModelBuilder());
                        addTestServices(services);

                    });
                });
                container.routeBuilder = new RestierRouteBuilder().MapApiRoute<TestableEmptyApi>(i.ToString(CultureInfo.InvariantCulture), "", true);

                var provider = container.BuildContainer();
                var tasks = PrepareThreads(50, provider, wait);
                wait.Set();

                var models = await Task.WhenAll(tasks);
                models.All(e => object.ReferenceEquals(e, models[42])).Should().BeTrue();
            }
        }

        [Ignore]
        [TestMethod]
        public async Task GetModelAsyncRetriableAfterFailure()
        {
            using (var wait = new ManualResetEventSlim(false))
            {
                var container = new RestierContainerBuilder(builder =>
                {
                    builder.AddRestierApi<TestableEmptyApi>(services =>
                    {
                        services.AddChainedService<IModelBuilder>((sp, next) => new TestRetryModelBuilder());
                        addTestServices(services);

                    });
                });
                var provider = container.BuildContainer();

                var tasks = PrepareThreads(6, provider, wait);
                wait.Set();

#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
                await Task.WhenAll(tasks).ContinueWith(t =>
                {
                    t.IsFaulted.Should().BeTrue();
                    tasks.All(e => e.IsFaulted).Should().BeTrue();
                });
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler

                tasks = PrepareThreads(150, provider, wait);

                var models = await Task.WhenAll(tasks);
                models.All(e => ReferenceEquals(e, models[42])).Should().BeTrue();
            }
        }

        #region Test Resources

        private class TestModelProducer : IModelBuilder
        {
            public IEdmModel GetModel(ModelContext context)
            {
                var model = new EdmModel();
                var entityType = new EdmEntityType("TestNamespace", "TestName");
                var entityContainer = new EdmEntityContainer("TestNamespace", "Entities");
                entityContainer.AddEntitySet("TestEntitySet", entityType);
                model.AddElement(entityType);
                model.AddElement(entityContainer);

                return model;
            }
        }

        private class TestModelExtender : IModelBuilder
        {
            private readonly int _index;

            public TestModelExtender(int index) => _index = index;

            public IModelBuilder InnerHandler { get; set; }

            public IEdmModel GetModel(ModelContext context)
            {
                IEdmModel innerModel = null;
                if (InnerHandler is not null)
                {
                    innerModel = InnerHandler.GetModel(context);
                }

                var entityType = new EdmEntityType("TestNamespace", "TestName" + _index);

                var model = innerModel as EdmModel;
                model.Should().NotBeNull();

                model.AddElement(entityType);
                (model.EntityContainer as EdmEntityContainer).AddEntitySet("TestEntitySet" + _index, entityType);

                return model;
            }
        }

        private class TestSingleCallModelBuilder : IModelBuilder
        {
            public int CalledCount;

            public IEdmModel GetModel(ModelContext context)
            {
                Thread.Sleep(30);

                Interlocked.Increment(ref CalledCount);
                return new EdmModel();
            }
        }

        private static Task<IEdmModel>[] PrepareThreads(int count, IServiceProvider provider, ManualResetEventSlim wait)
        {
            var tasks = new Task<IEdmModel>[count];
            var result = Parallel.For(0, count, (inx, state) =>
            {
                var source = new TaskCompletionSource<IEdmModel>();
                new Thread(() =>
                {
                    // To make threads better aligned.
                    wait.Wait();

                    var scopedProvider = provider.GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider;
                    var api = scopedProvider.GetService<ApiBase>();
                    try
                    {
                        var model = api.GetModel();
                        source.SetResult(model);
                    }
                    catch (Exception e)
                    {
                        source.SetException(e);
                    }
                }).Start();
                tasks[inx] = source.Task;
            });

            result.IsCompleted.Should().BeTrue();
            return tasks;
        }

        private class TestRetryModelBuilder : IModelBuilder
        {
            public int CalledCount;

            public IEdmModel GetModel(ModelContext context)
            {
                if (CalledCount++ == 0)
                {
                    Thread.Sleep(100);
                    throw new Exception("Deliberate failure");
                }

                return new EdmModel();
            }
        }

        #endregion

    }

}