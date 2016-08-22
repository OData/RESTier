// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if !EF7
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
#endif
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
#if EF7
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Providers.EntityFramework
{
    /// <summary>
    /// Represents a model producer that uses the
    /// metadata workspace accessible from a DbContext.
    /// </summary>
    internal class ModelProducer : IModelBuilder
    {
        public IModelBuilder InnerModelBuilder { get; set; }

        /// <summary>
        /// This class will not real build a model, but only get entity set name and entity map from data source
        /// Then pass the information to publisher layer to build the model.
        /// </summary>
        /// <param name="context">
        /// The context for processing
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// Always a null model.
        /// </returns>
        public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");

#if EF7
            var dbContext = context.ApiContext.GetApiService<DbContext>();
            context.ResourceSetTypeMap = dbContext.GetType().GetProperties()
                .Where(e => e.PropertyType.FindGenericType(typeof(DbSet<>)) != null)
                .ToDictionary(e => e.Name, e => e.PropertyType.GetGenericArguments()[0]);
            context.ResourceTypeKeyPropertiesMap = dbContext.Model.GetEntityTypes().ToDictionary(
                e => e.ClrType,
                e => ((ICollection<PropertyInfo>)
                    e.FindPrimaryKey().Properties.Select(p => e.ClrType.GetProperty(p.Name)).ToList()));
#else
            var resourceSetTypeMap = new Dictionary<string, Type>();
            var resourceTypeKeyPropertiesMap = new Dictionary<Type, ICollection<PropertyInfo>>();
            var dbContext = context.ServiceProvider.GetService<DbContext>();

            var efModel = (dbContext as IObjectContextAdapter).ObjectContext.MetadataWorkspace;
            var efEntityContainer = efModel.GetItems<EntityContainer>(DataSpace.CSpace).Single();
            var itemCollection = (ObjectItemCollection)efModel.GetItemCollection(DataSpace.OSpace);

            foreach (var efEntitySet in efEntityContainer.EntitySets)
            {
                var efEntityType = efEntitySet.ElementType;
                var objectSpaceType = efModel.GetObjectSpaceType(efEntityType);
                Type clrType = itemCollection.GetClrType(objectSpaceType);

                // As entity set name and type map
                resourceSetTypeMap.Add(efEntitySet.Name, clrType);

                ICollection<PropertyInfo> keyProperties = new List<PropertyInfo>();
                foreach (var property in efEntityType.KeyProperties)
                {
                    keyProperties.Add(clrType.GetProperty(property.Name));
                }

                resourceTypeKeyPropertiesMap.Add(clrType, keyProperties);
            }

            context.ResourceSetTypeMap = resourceSetTypeMap;
            context.ResourceTypeKeyPropertiesMap = resourceTypeKeyPropertiesMap;
#endif
            if (InnerModelBuilder != null)
            {
                return InnerModelBuilder.GetModelAsync(context, cancellationToken);
            }

            return Task.FromResult<IEdmModel>(null);
        }
    }
}
