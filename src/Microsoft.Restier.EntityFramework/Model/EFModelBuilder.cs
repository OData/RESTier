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
    internal class EFModelBuilder : IModelBuilder
    {

        #region Properties

        /// <summary>
        /// A way to chain ModelBuilders together.
        /// </summary>
        public IModelBuilder InnerModelBuilder { get; set; }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEdmModel GetModel(ModelContext context)
        {
            Ensure.NotNull(context, nameof(context));

#if EF7
            var dbContext = context.GetApiService<DbContext>();
            context.ResourceSetTypeMap.AddRange(dbContext.GetType().GetProperties()
                .Where(e => e.PropertyType.FindGenericType(typeof(DbSet<>)) != null)
                .ToDictionary(e => e.Name, e => e.PropertyType.GetGenericArguments()[0]));
            context.ResourceTypeKeyPropertiesMap.AddRange(dbContext.Model.GetEntityTypes().ToDictionary(
                e => e.ClrType,
                e => ((ICollection<PropertyInfo>)
                    e.FindPrimaryKey().Properties.Select(p => e.ClrType.GetProperty(p.Name)).ToList())));
#else

            if (!context.Api.GetType().GetProperties().Any(c => c.Name == "DbContext"))
            {
                //RWM: This isn't an EF context, don't build anything.
                return null;
            }

            dynamic api = context.Api;
            var dbContext = api.DbContext as DbContext;

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
            // we won't crash if more than one is returned, and we can check for null
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

                // RWM: We should not have to do this, and should not be getting here more than once.
                if (!context.ResourceSetTypeMap.ContainsKey(efEntitySet.Name))
                {

                    // As entity set name and type map
                    context.ResourceSetTypeMap.Add(efEntitySet.Name, clrType);

                    ICollection<PropertyInfo> keyProperties = new List<PropertyInfo>();
                    foreach (var property in efEntityType.KeyProperties)
                    {
                        keyProperties.Add(clrType.GetProperty(property.Name));
                    }

                    context.ResourceTypeKeyPropertiesMap.Add(clrType, keyProperties);
                }
            }
#endif
            if (InnerModelBuilder != null)
            {
                return InnerModelBuilder.GetModel(context);
            }

            //RWM: This doesn't return anything because the RestierModelBuilder in the ASP.NET project is the one that actually returns the model.
            return null;

        }


    }
}
