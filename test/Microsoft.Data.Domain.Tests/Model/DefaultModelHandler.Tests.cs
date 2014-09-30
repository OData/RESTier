// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Domain.Model.Tests
{
    [TestClass]
    public class DefaultModelHandlerTests
    {
        private class TestModelProducer : IModelProducer
        {
            public Task<EdmModel> ProduceModelAsync(
                ModelContext context,
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
                return Task.FromResult(model);
            }
        }

        private class TestModelExtender : IModelExtender
        {
            private int _index;

            public TestModelExtender(int index)
            {
                this._index = index;
            }

            public async Task ExtendModelAsync(
                ModelContext context,
                CancellationToken cancellationToken)
            {
                var entityType = new EdmEntityType(
                    "TestNamespace", "TestName" + this._index);
                context.Model.AddElement(entityType);
                (context.Model.EntityContainer as EdmEntityContainer)
                    .AddEntitySet("TestEntitySet" + this._index, entityType);
                await Task.Yield();
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

        [TestMethod]
        public async Task GetModelUsingDefaultModelHandler()
        {
            var configuration = new DomainConfiguration();
            configuration.SetHookPoint(
                typeof(IModelProducer), new TestModelProducer());
            configuration.AddHookPoint(
                typeof(IModelExtender), new TestModelExtender(2));
            configuration.AddHookPoint(
                typeof(IModelExtender), new TestModelExtender(3));
            configuration.AddHookPoint(
                typeof(IModelVisibilityFilter),
                new TestModelVisibilityFilter());
            configuration.EnsureCommitted();
            var context = new DomainContext(configuration);

            var model = await Domain.GetModelAsync(context);
            Assert.AreEqual(3, model.SchemaElements.Count());
            Assert.IsNull(model.SchemaElements
                .SingleOrDefault(e => e.Name == "TestName"));
            Assert.IsNotNull(model.SchemaElements
                .SingleOrDefault(e => e.Name == "TestName2"));
            Assert.IsNotNull(model.SchemaElements
                .SingleOrDefault(e => e.Name == "TestName3"));
            Assert.IsNotNull(model.EntityContainer);
            Assert.IsNull(model.EntityContainer.Elements
                .SingleOrDefault(e => e.Name == "TestEntitySet"));
            Assert.IsNotNull(model.EntityContainer.Elements
                .SingleOrDefault(e => e.Name == "TestEntitySet2"));
            Assert.IsNotNull(model.EntityContainer.Elements
                .SingleOrDefault(e => e.Name == "TestEntitySet3"));
        }
    }
}
