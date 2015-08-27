// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Xunit;

namespace Microsoft.Restier.Conventions.Test
{
    public class ConventionalModelExtenderTest
    {
        [Theory]
        [InlineData(typeof(OneDomain))]
        [InlineData(typeof(OtherDomain))]
        public async Task ExtendModelAsync_UpdatesModel_IfHasOnModelCreatingMethod(Type type)
        {
            // Arrange
            var domain = Activator.CreateInstance(type);
            var extender = new ConventionalModelExtender(type);
            var domainConfig = new DomainConfiguration();
            domainConfig.EnsureCommitted();
            var domainContext = new DomainContext(domainConfig);
            domainContext.SetProperty(type.AssemblyQualifiedName, domain);
            var model = GetModel();
            var context = new ModelBuilderContext(domainContext) { Model = model };

            // Act
            await extender.HandleAsync(context, new CancellationToken());

            // Assert
            Assert.Same(model, context.Model);
            var operations = model.SchemaElements.OfType<IEdmOperation>();
            Assert.Single(operations);
            var operation = operations.Single();
            Assert.True(operation.IsBound);
            Assert.True(operation.IsFunction());
            Assert.Equal("MostExpensive", operation.Name);
            Assert.Equal("ns", operation.Namespace);
        }

        [Fact]
        public async Task ExtendModelAsync_DoesntUpdatesModel_IfWithoutOnModelCreatingMethod()
        {
            // Arrange
            var domain = new AnyDomain();
            var type = domain.GetType();
            var extender = new ConventionalModelExtender(type);
            var domainConfig = new DomainConfiguration();
            domainConfig.EnsureCommitted();
            var domainContext = new DomainContext(domainConfig);
            domainContext.SetProperty(type.AssemblyQualifiedName, domain);
            var model = GetModel();
            var context = new ModelBuilderContext(domainContext) { Model = model };

            // Act
            await extender.HandleAsync(context, new CancellationToken());

            // Assert
            Assert.Same(model, context.Model);
            Assert.Empty(model.SchemaElements.OfType<IEdmOperation>());
        }

        private static EdmModel GetModel()
        {
            const string ns = "ns";
            var builder = new ODataConventionModelBuilder { Namespace = ns };
            builder.EntityType<Product>().Namespace = ns;
            return (EdmModel)builder.GetEdmModel();
        }

        public class Product
        {
            public int Id { get; set; }
        }

        public class OneDomain
        {
            private EdmModel OnModelExtending(EdmModel model)
            {
                return OtherDomain.OnModelExtending(model);
            }
        }

        public class OtherDomain
        {
            internal static EdmModel OnModelExtending(EdmModel model)
            {
                var ns = model.DeclaredNamespaces.First();
                var product = (IEdmEntityType)model.FindDeclaredType(ns + "." + "Product");
                var products = EdmCoreModel.GetCollection(new EdmEntityTypeReference(product, false));
                var mostExpensive = new EdmFunction(ns, "MostExpensive",
                    EdmCoreModel.Instance.GetPrimitive(EdmPrimitiveTypeKind.Double, isNullable: false), isBound: true,
                    entitySetPathExpression: null, isComposable: false);
                mostExpensive.AddParameter("bindingParameter", products);
                model.AddElement(mostExpensive);
                return model;
            }
        }

        public class AnyDomain
        {
        }
    }
}
