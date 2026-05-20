// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using System.Linq;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.Infrastructure
{

    /// <summary>
    /// Self-contained CLR row type registered as an EDM <c>ComplexType</c> for the
    /// keyless-view OpenAPI fixture. Has no EF dependency.
    /// </summary>
    public class KeylessViewTestRow
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Minimal API surface used by the keyless-view OpenAPI integration test.
    /// Owns no entity sets — only the unbound function-import declared by
    /// <see cref="KeylessViewTestApiModelBuilder"/>.
    /// </summary>
    public class KeylessViewTestApi : ApiBase
    {

        public KeylessViewTestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

    }

    /// <summary>
    /// Inner <see cref="IModelBuilder"/> for <see cref="KeylessViewTestApi"/>. Declares
    /// <see cref="KeylessViewTestRow"/> as a ComplexType, adds an unbound
    /// <c>EdmFunction</c> returning <c>Collection(KeylessViewTestRow)</c>, exposes it
    /// as a function import named <c>TestViews</c>, and registers a matching entry on
    /// the <see cref="KeylessViewRegistry"/> resolved from DI so the OpenAPI generator
    /// sees the same shape that <c>EFModelBuilder</c> would produce.
    /// </summary>
    public class KeylessViewTestApiModelBuilder : IModelBuilder
    {

        private readonly KeylessViewRegistry registry;

        public KeylessViewTestApiModelBuilder(KeylessViewRegistry registry)
        {
            this.registry = registry;
        }

        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.ComplexType<KeylessViewTestRow>();

            var model = (EdmModel)builder.GetEdmModel();
            var ns = model.DeclaredNamespaces.FirstOrDefault() ?? "Default";
            var complex = (IEdmComplexType)model.FindDeclaredType($"{ns}.KeylessViewTestRow");
            var collectionType = new EdmCollectionTypeReference(
                new EdmCollectionType(new EdmComplexTypeReference(complex, isNullable: true)));

            var function = new EdmFunction(
                $"{ns}.Views",
                "TestViews",
                collectionType,
                isBound: false,
                entitySetPathExpression: null,
                isComposable: false);
            model.AddElement(function);

            var container = (EdmEntityContainer)model.EntityContainer;
            container.AddFunctionImport("TestViews", function, entitySet: null);

            registry.Register(
                "TestViews",
                typeof(KeylessViewTestRow),
                _ => new[]
                {
                    new KeylessViewTestRow { Name = "alpha", Count = 1 },
                    new KeylessViewTestRow { Name = "beta", Count = 2 },
                }.AsQueryable());

            return model;
        }

    }

}
