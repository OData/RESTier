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

            EntityFrameworkCoreGetEntities(out var entitySetMap, out var entitySetKeyMap);
#endif
#if EF6
            EntityFramework6GetEntitySets(out var entitySetMap, out var entitySetKeyMap);
#endif
            // Get the inner model if it exists.
            var innerModel = Inner?.GetEdmModel();

            // Build the model from the Entity Framework Entity Sets.
            var result = BuildEdmModelFromEntitySetMaps(entitySetMap, entitySetKeyMap, _namingConvention, _spatialConvention, _dbContext);

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
            RestierNamingConvention namingConvention,
            SpatialModelConvention spatialConvention,
            object spatialProviderContext)
        {
            if (!entitySetMap.Any())
            {
                return new EdmModel();
            }

            // Collection of entity type and set name is set by EF now,
            // and EF model producer will not build model any more
            // Web Api OData conversion model built is been used here,
            // refer to Web Api OData document for the detail conversions been used for model built.
            var builder = new ODataConventionModelBuilder
            {
                // This namespace is used by container
                Namespace = entitySetMap.First().Value.Namespace
            };

            var method = typeof(ODataConventionModelBuilder).GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (var pair in entitySetMap)
            {
                // Build a method with the specific type argument
                var specifiedMethod = method.MakeGenericMethod(pair.Value);
                var parameters = new object[]
                {
                    pair.Key,
                };

                specifiedMethod.Invoke(builder, parameters);
            }

            foreach (var pair in entitySetKeyMap)
            {
                if (builder.GetTypeConfigurationOrNull(pair.Key) is not EntityTypeConfiguration edmTypeConfiguration)
                {
                    continue;
                }

                if (pair.Value is null)
                {
                    throw new InvalidOperationException($"The entity '{pair.Key}' does not have a key specified. Entities tagged with the [Keyless] attribute " +
                                                        $"(or otherwise do not have a key specified) are not supported in either OData or Restier. Please map the object as a ComplexType and " +
                                                        $"implement as an [UnboundOperation] on your API instead.");
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

            // Phase 1: capture spatial-typed properties and remove them from the convention builder so they
            // aren't published as the storage CLR type. Short-circuits when no spatial providers are registered.
            var entityClrTypes = entitySetMap.Values.Distinct().ToList();
            var spatialCaptures = spatialConvention.CapturePhase(builder, entityClrTypes, spatialProviderContext);

            var edmModel = (EdmModel)builder.GetEdmModel();

            // Phase 2: add the captured spatial properties to the resulting model as Microsoft.Spatial EDM primitives.
            spatialConvention.AugmentPhase(edmModel, spatialCaptures, namingConvention);

            return edmModel;
        }
    }
}
