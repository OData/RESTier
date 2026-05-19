// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFramework.Shared.Model;
using System.Collections.Generic;


#if EF6
using System.Data.Entity;

namespace Microsoft.Restier.EntityFramework
#endif

#if EFCore
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.EntityFrameworkCore
#endif

{
    /// <summary>
    /// Represents a model producer that uses the metadata workspace accessible from a <see cref="DbContext" />.
    /// </summary>
    public partial class EFModelBuilder<TDbContext> : IModelBuilder
        where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private readonly ModelMerger _modelMerger;
        private readonly KeylessViewRegistry _keylessViewRegistry;
        private readonly RestierNamingConvention _namingConvention;
        private readonly SpatialModelConvention _spatialConvention;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFModelBuilder{TDbContext}"/> class.
        /// </summary>
        /// <param name="dbContext">The DbContext to use for model building.</param>
        /// <param name="modelMerger">The model merger to use.</param>
        /// <param name="keylessViewRegistry">The keyless view registry used to capture keyless CLR types discovered during model building.</param>
        /// <param name="namingConvention">The naming convention to use for the EDM model.</param>
        /// <param name="spatialMetadataProviders">
        /// Optional set of spatial metadata providers. When non-empty, spatial-typed entity properties are
        /// rewritten to Microsoft.Spatial EDM primitives by <see cref="SpatialModelConvention"/>. DI will
        /// auto-fill this enumerable; the parameter is optional so non-DI consumers compile unchanged.
        /// </param>
        public EFModelBuilder(
            TDbContext dbContext,
            ModelMerger modelMerger,
            KeylessViewRegistry keylessViewRegistry,
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase,
            IEnumerable<ISpatialModelMetadataProvider> spatialMetadataProviders = null)
        {
            Ensure.NotNull(dbContext, nameof(dbContext));
            Ensure.NotNull(modelMerger, nameof(modelMerger));
            Ensure.NotNull(keylessViewRegistry, nameof(keylessViewRegistry));
            this._dbContext = dbContext;
            this._modelMerger = modelMerger;
            this._keylessViewRegistry = keylessViewRegistry;
            this._namingConvention = namingConvention;
            this._spatialConvention = new SpatialModelConvention(spatialMetadataProviders);
        }

        /// <summary>
        /// A way to chain ModelBuilders together.
        /// </summary>
        public IModelBuilder Inner { get; set; }

        /// <inheritdoc />
        public IEdmModel GetEdmModel()
        {
            // Get the Entity set maps from the respective EF versions.
#if EFCore
            EntityFrameworkCoreGetEntities(out var entitySetMap, out var entitySetKeyMap, out var sourceFactoryMap);
#endif
#if EF6
            EntityFramework6GetEntitySets(out var entitySetMap, out var entitySetKeyMap, out var sourceFactoryMap);
#endif
            // Get the inner model if it exists.
            var innerModel = Inner?.GetEdmModel();

            // Build the model from the Entity Framework Entity Sets.
            var result = BuildEdmModelFromEntitySetMaps(entitySetMap, entitySetKeyMap, sourceFactoryMap, _namingConvention, _spatialConvention, _dbContext, _keylessViewRegistry);

            // merge the inner model into the result.
            if (innerModel is not null)
            {
                _modelMerger.Merge(innerModel, result);
            }

            return result;
        }

        private static EdmModel BuildEdmModelFromEntitySetMaps(
            Dictionary<string, Type> entitySetMap,
            Dictionary<Type, ICollection<PropertyInfo>> entitySetKeyMap,
            Dictionary<string, Func<object, IQueryable>> sourceFactoryMap,
            RestierNamingConvention namingConvention,
            SpatialModelConvention spatialConvention,
            object spatialProviderContext,
            KeylessViewRegistry keylessViewRegistry)
        {
            if (!entitySetMap.Any())
            {
                return new EdmModel();
            }

            // Split: keyed entity sets become EntitySet<T>; keyless DbSets/EntitySets become ComplexType<T> + FunctionImport.
            // A type is keyless if its key collection is null OR empty (EF Core reports null, EF6 reports an empty list).
            var keyedEntitySets = new Dictionary<string, Type>();
            var keylessViewSets = new Dictionary<string, Type>();
            foreach (var pair in entitySetMap)
            {
                var keyList = entitySetKeyMap.TryGetValue(pair.Value, out var keys) ? keys : null;
                if (keyList is null || keyList.Count == 0)
                {
                    keylessViewSets.Add(pair.Key, pair.Value);
                }
                else
                {
                    keyedEntitySets.Add(pair.Key, pair.Value);
                }
            }

            var builder = new ODataConventionModelBuilder
            {
                // This namespace is used by container
                Namespace = entitySetMap.First().Value.Namespace
            };

            var entitySetMethod = typeof(ODataConventionModelBuilder).GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var complexTypeMethod = typeof(ODataConventionModelBuilder).GetMethod("ComplexType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy, Type.EmptyTypes);

            foreach (var pair in keyedEntitySets)
            {
                var specifiedMethod = entitySetMethod.MakeGenericMethod(pair.Value);
                var parameters = new object[] { pair.Key };
                specifiedMethod.Invoke(builder, parameters);
            }

            foreach (var pair in keylessViewSets)
            {
                var specifiedMethod = complexTypeMethod.MakeGenericMethod(pair.Value);
                specifiedMethod.Invoke(builder, Array.Empty<object>());
            }

            foreach (var pair in entitySetKeyMap)
            {
                if (builder.GetTypeConfigurationOrNull(pair.Key) is not EntityTypeConfiguration edmTypeConfiguration)
                {
                    continue;
                }

                if (pair.Value is null || pair.Value.Count == 0)
                {
                    // Keyless types are handled above (registered as ComplexType, not EntityType).
                    continue;
                }

                foreach (var property in pair.Value)
                {
                    edmTypeConfiguration.HasKey(property);
                }
            }
            switch (namingConvention)
            {
                case RestierNamingConvention.LowerCamelCase:
                    builder.EnableLowerCamelCase();
                    break;
                case RestierNamingConvention.LowerCamelCaseWithEnumMembers:
                    builder.EnableLowerCamelCaseForPropertiesAndEnums();
                    break;
            }

            var entityClrTypes = entitySetMap.Values.Distinct().ToList();
            var spatialCaptures = spatialConvention.CapturePhase(builder, entityClrTypes, spatialProviderContext);

            var edmModel = (EdmModel)builder.GetEdmModel();

            spatialConvention.AugmentPhase(edmModel, spatialCaptures, namingConvention);

            AddKeylessViewFunctionImports(edmModel, keylessViewSets, sourceFactoryMap, keylessViewRegistry);

            return edmModel;
        }

        private static void AddKeylessViewFunctionImports(
            EdmModel edmModel,
            Dictionary<string, Type> keylessViewSets,
            Dictionary<string, Func<object, IQueryable>> sourceFactoryMap,
            KeylessViewRegistry keylessViewRegistry)
        {
            if (keylessViewSets.Count == 0)
            {
                return;
            }

            var container = edmModel.EntityContainer as EdmEntityContainer
                ?? throw new InvalidOperationException("Keyless view registration requires a writable EdmEntityContainer.");

            foreach (var pair in keylessViewSets)
            {
                var viewName = pair.Key;
                var clrType = pair.Value;
                var edmComplexType = edmModel.SchemaElements.OfType<IEdmComplexType>().FirstOrDefault(c => c.Name == clrType.Name)
                    ?? throw new InvalidOperationException(
                        $"Could not find ComplexType '{clrType.Name}' in the EDM model for keyless view '{viewName}'.");

                var complexTypeReference = new EdmComplexTypeReference(edmComplexType, isNullable: false);
                var collectionTypeReference = new EdmCollectionTypeReference(new EdmCollectionType(complexTypeReference));

                // The EdmFunction's schema-level name must be distinct from the ComplexType's
                // (they share a schema namespace under the convention builder). Putting the
                // function in a "<namespace>.Views" sub-namespace keeps the URL/import name
                // unchanged (clients still hit `GET /odata/<viewName>()`) while sidestepping
                // the OData CSDL uniqueness rule for schema-level elements.
                var functionNamespace = $"{container.Namespace}.Views";
                var function = new EdmFunction(
                    functionNamespace,
                    viewName,
                    collectionTypeReference,
                    isBound: false,
                    entitySetPathExpression: null,
                    isComposable: false);

                edmModel.AddElement(function);
                container.AddFunctionImport(viewName, function, entitySet: null);

                if (!sourceFactoryMap.TryGetValue(viewName, out var sourceFactory))
                {
                    throw new InvalidOperationException(
                        $"No source factory was supplied for keyless view '{viewName}'. " +
                        $"This is an internal bug in the EF model builder.");
                }

                keylessViewRegistry.Register(viewName, clrType, sourceFactory);
            }
        }
    }
}
