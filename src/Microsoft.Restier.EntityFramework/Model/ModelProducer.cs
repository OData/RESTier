// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if !EF7
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
#endif
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
#if EF7
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.EntityFramework
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
            Ensure.NotNull(context, nameof(context));

            var dbContext = (context.Api as IDbContextProvider).DbContext;
#if EF7
            context.ResourceSetTypeMap.AddRange(dbContext.GetType().GetProperties()
                .Where(e => e.PropertyType.FindGenericType(typeof(DbSet<>)) != null)
                .ToDictionary(e => e.Name, e => e.PropertyType.GetGenericArguments()[0]));
            context.ResourceTypeKeyPropertiesMap.AddRange(dbContext.Model.GetEntityTypes().ToDictionary(
                e => e.ClrType,
                e => ((ICollection<PropertyInfo>)
                    e.FindPrimaryKey().Properties.Select(p => e.ClrType.GetProperty(p.Name)).ToList())));
#else
            
            var efModel = (dbContext as IObjectContextAdapter).ObjectContext.MetadataWorkspace;

            // @robertmclaws: The query below actually returns all registered Containers
            // across all registered DbContexts.
            // It is likely a bug in some other part of OData. But we can roll with it.
            var efEntityContainers = efModel.GetItems<EntityContainer>(DataSpace.CSpace);

            // @robertmclaws: Because of the bug above, we should not make any assumptions about what is returned,
            // and get the specific container we want to use. Even if the bug gets fixed, the next line should still
            // continue to work.
            var efEntityContainer = efEntityContainers.FirstOrDefault(c => c.Name == dbContext.GetType().Name);

            // @robertmclaws: Now that we're doing a proper FirstOrDefault() instead of a Single(),
            // we wont' crash if more than one is returned, and we can check for null
            // and inform the user specifically what happened.
            if (efEntityContainer == null)
            {
                if (efEntityContainers.Count > 1)
                {
                    // @robertmclaws: In this case, we have multiple DbContexts available, but none of them match up.
                    //                Tell the user what we have, and what we were expecting, so they can fix it.
                    var containerNames = efEntityContainers.Aggregate(
                        string.Empty, (current, next) => next.Name + ", ");
                    throw new Exception(string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.MultipleDbContextsExpectedException,
                        containerNames.Substring(0, containerNames.Length - 2),
                        efEntityContainer.Name));
                }

                // @robertmclaws: In this case, we only had one DbContext available, and if wasn't the right one.
                throw new Exception(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.DbContextCouldNotBeFoundException,
                    dbContext.GetType().Name,
                    efEntityContainer.Name));
            }

            var itemCollection = (ObjectItemCollection)efModel.GetItemCollection(DataSpace.OSpace);

            foreach (var efEntitySet in efEntityContainer.EntitySets)
            {
                var efEntityType = efEntitySet.ElementType;
                var objectSpaceType = efModel.GetObjectSpaceType(efEntityType);
                var clrType = itemCollection.GetClrType(objectSpaceType);

                // As entity set name and type map
                context.ResourceSetTypeMap.Add(efEntitySet.Name, clrType);

                ICollection<PropertyInfo> keyProperties = new List<PropertyInfo>();
                foreach (var property in efEntityType.KeyProperties)
                {
                    keyProperties.Add(clrType.GetProperty(property.Name));
                }

                context.ResourceTypeKeyPropertiesMap.Add(clrType, keyProperties);
            }
#endif
            if (InnerModelBuilder != null)
            {
                return InnerModelBuilder.GetModelAsync(context, cancellationToken);
            }

            return Task.FromResult<IEdmModel>(null);
        }
    }
}
