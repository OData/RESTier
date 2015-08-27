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
        private class TestModelProducer : HookHandler<ModelBuilderContext>
        {
            public override Task HandleAsync(
                ModelBuilderContext context,
                CancellationToken cancellationToken)
            {
                var model = new EdmModel();
                var entityType = new EdmEntityType(
                    "TestNamespace", "TestName");
                var entityContainer = new EdmEntityContainer(
                    "TestNamespace", "Entities");
                entityContainer.AddEntitySet("TestEntitySet", entityType);
                model.AddElement(entityType);
                model.AddElement(entityContainer);

                context.Model = model;
                return Task.FromResult<object>(null);
            }
        }

        private class TestModelExtender : HookHandler<ModelBuilderContext>
        {
            private int _index;

            public TestModelExtender(int index)
            {
                _index = index;
            }

            public override async Task HandleAsync(
                ModelBuilderContext context,
                CancellationToken cancellationToken)
            {
                await base.HandleAsync(context, cancellationToken);

                var entityType = new EdmEntityType(
                    "TestNamespace", "TestName" + _index);

                var model = context.Model as EdmModel;
                Assert.NotNull(model);

                model.AddElement(entityType);
                (model.EntityContainer as EdmEntityContainer)
                    .AddEntitySet("TestEntitySet" + _index, entityType);
            }
        }

        private class TestModelVisibilityFilter : IModelVisibilityFilter
        {
            public bool IsVisible(
                DomainConfiguration configuration,
                InvocationContext context,
                IEdmModel model, IEdmSchemaElement element)
            {
                if (element.Name == "TestName")
                {
                    return false;
                }
                return true;
            }

            public bool IsVisible(
                DomainConfiguration configuration,
                InvocationContext context,
                IEdmModel model, IEdmEntityContainerElement element)
            {
                if (element.Name == "TestEntitySet")
                {
                    return false;
                }
                return true;
            }
        }

        [Fact]
        public async Task GetModelUsingDefaultModelHandler()
        {
            var configuration = new DomainConfiguration();
            configuration.AddHookHandler(new TestModelProducer());
            configuration.AddHookHandler(new TestModelExtender(2));
            configuration.AddHookHandler(new TestModelExtender(3));

            configuration.AddHookPoint(
                typeof(IModelVisibilityFilter),
                new TestModelVisibilityFilter());
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);

            var model = await Domain.GetModelAsync(context);
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
