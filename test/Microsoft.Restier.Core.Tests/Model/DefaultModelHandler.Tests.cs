// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

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
    }
}
