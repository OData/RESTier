// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core.Model;
using Xunit;

namespace Microsoft.Restier.Core.Tests.Model
{
    public class DefaultModelHandlerTests
    {
        private class TestModelProducer : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                var model = new EdmModel();
                var entityType = new EdmEntityType(
                    "TestNamespace", "TestName");
                var entityContainer = new EdmEntityContainer(
                    "TestNamespace", "Entities");
                entityContainer.AddEntitySet("TestEntitySet", entityType);
                model.AddElement(entityType);
                model.AddElement(entityContainer);

                return Task.FromResult<IEdmModel>(model);
            }
        }

        private class TestModelExtender : IModelBuilder
        {
            private int _index;

            public TestModelExtender(int index)
            {
                _index = index;
            }

            public IModelBuilder InnerHandler { get; set; }

            public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                IEdmModel innerModel = null;
                if (this.InnerHandler != null)
                {
                    innerModel = await this.InnerHandler.GetModelAsync(context, cancellationToken);
                }

                var entityType = new EdmEntityType(
                     "TestNamespace", "TestName" + _index);

                var model = innerModel as EdmModel;
                Assert.NotNull(model);

                model.AddElement(entityType);
                (model.EntityContainer as EdmEntityContainer)
                    .AddEntitySet("TestEntitySet" + _index, entityType);

                return model;
            }
        }

        [Fact]
        public async Task GetModelUsingDefaultModelHandler()
        {
            var builder = new ApiBuilder();
            builder.CutoffPrevious<IModelBuilder>(new TestModelProducer());
            builder.ChainPrevious<IModelBuilder>(next => new TestModelExtender(2)
            {
                InnerHandler = next,
            });
            builder.ChainPrevious<IModelBuilder>(next => new TestModelExtender(3)
            {
                InnerHandler = next,
            });

            var configuration = builder.Build();
            var context = new ApiContext(configuration);

            var model = await Api.GetModelAsync(context);
            Assert.Equal(4, model.SchemaElements.Count());
            Assert.NotNull(model.SchemaElements
                .SingleOrDefault(e => e.Name == "TestName"));
            Assert.NotNull(model.SchemaElements
                .SingleOrDefault(e => e.Name == "TestName2"));
            Assert.NotNull(model.SchemaElements
                .SingleOrDefault(e => e.Name == "TestName3"));
            Assert.NotNull(model.EntityContainer);
            Assert.NotNull(model.EntityContainer.Elements
                .SingleOrDefault(e => e.Name == "TestEntitySet"));
            Assert.NotNull(model.EntityContainer.Elements
                .SingleOrDefault(e => e.Name == "TestEntitySet2"));
            Assert.NotNull(model.EntityContainer.Elements
                .SingleOrDefault(e => e.Name == "TestEntitySet3"));
        }

        private class TestSingleCallModelBuilder : IModelBuilder
        {
            public int CalledCount;

            public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                await Task.Delay(30);

                Interlocked.Increment(ref CalledCount);
                return new EdmModel();
            }
        }

        private static Task<IEdmModel>[] PrepareThreads(int count, ApiConfiguration configuration, ManualResetEventSlim wait)
        {
            var tasks = new Task<IEdmModel>[count];
            var result = Parallel.For(0, count, (inx, state) =>
            {
                var source = new TaskCompletionSource<IEdmModel>();
                new Thread(() =>
                {
                    // To make threads better aligned.
                    wait.Wait();

                    var context = new ApiContext(configuration);
                    try
                    {
                        var model = Api.GetModelAsync(context).Result;
                        source.SetResult(model);
                    }
                    catch (Exception e)
                    {
                        source.SetException(e);
                    }
                }).Start();
                tasks[inx] = source.Task;
            });

            Assert.True(result.IsCompleted);
            return tasks;
        }

        [Fact]
        public async Task ModelBuilderShouldBeCalledOnlyOnceIfSucceeded()
        {
            var builder = new ApiBuilder();
            var service = new TestSingleCallModelBuilder();
            builder.CutoffPrevious<IModelBuilder>(service);
            var configuration = builder.Build();

            using (var wait = new ManualResetEventSlim(false))
            {
                for (int i = 0; i < 2; i++)
                {
                    var tasks = PrepareThreads(50, configuration, wait);
                    wait.Set();

                    var models = await Task.WhenAll(tasks);
                    Assert.Equal(1, service.CalledCount);
                    Assert.True(models.All(e => object.ReferenceEquals(e, models[42])));
                }
            }
        }

        private class TestRetryModelBuilder : IModelBuilder
        {
            public int CalledCount;

            public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                if (CalledCount++ == 0)
                {
                    await Task.Delay(100);
                    throw new Exception("Deliberate failure");
                }

                return new EdmModel();
            }
        }

        [Fact]
        public async Task GetModelAsyncRetriableAfterFailure()
        {
            var builder = new ApiBuilder();
            var service = new TestRetryModelBuilder();
            builder.CutoffPrevious<IModelBuilder>(service);
            var configuration = builder.Build();

            using (var wait = new ManualResetEventSlim(false))
            {
                var tasks = PrepareThreads(6, configuration, wait);
                wait.Set();

                await Task.WhenAll(tasks).ContinueWith(t =>
                {
                    Assert.True(t.IsFaulted);
                    Assert.True(tasks.All(e => e.IsFaulted));
                });
                Assert.Equal(1, service.CalledCount);

                tasks = PrepareThreads(150, configuration, wait);

                var models = await Task.WhenAll(tasks);
                Assert.Equal(2, service.CalledCount);
                Assert.True(models.All(e => object.ReferenceEquals(e, models[42])));
            }
        }
    }
}
