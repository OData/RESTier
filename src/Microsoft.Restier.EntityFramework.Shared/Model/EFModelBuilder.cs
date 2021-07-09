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
#if EF7
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

#if EF7
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
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

            if (context.Api is not IEntityFrameworkApi frameworkApi)
            {
                //RWM: This isn't an EF context, don't build anything.
                return null;
            }

            var dbContext = frameworkApi.DbContext;

#if EF7

            //RWM: Grab Owned EntityTypes and make sure there are no DbSets that map to that type.
            var ownedTypes = dbContext.Model.GetEntityTypes().Where(c => c.IsOwned()).ToList();

            /* JHC TODO: for each entry in ownedTypes, check to see if there is a DbSet<> mapping in the context.  If there is, we need to throw an
             *           exception because there will be an EF failure in the call to GetModel().  The exception should inform the user that they have
             *           created a DbSet<> mapping for an Owned type and that this is not supported.
             * */

            // @caldwell0414: This code is looking for all of the DBSets on the context and generating a dictionary of DbSet Name and the Entity type.
            AddRange(context.ResourceSetTypeMap, dbContext.GetType().GetProperties()
                .Where(e => e.PropertyType.FindGenericType(typeof(DbSet<>)) != null)
                .ToDictionary(e => e.Name, e => e.PropertyType.GetGenericArguments()[0]));

            // @caldwell0414: This code goes through all of the Entity types in the model, and where not marked as "owned" builds a dictionary of name and primary-key type.
            var keys = dbContext.Model.GetEntityTypes().Where(c => !c.IsOwned()).ToDictionary(
                e => e.ClrType,
                e => ((ICollection<PropertyInfo>)e.FindPrimaryKey()?.Properties.Select(p => e.ClrType.GetProperty(p.Name)).ToList()));

            AddRange(context.ResourceTypeKeyPropertiesMap, keys/*.Where(c => c.Value != null)*/);   // JHC NOTE: only add this .Where() if we need it
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

        private static void AddRange<TKey, TValue>(
          IDictionary<TKey, TValue> source,
          IDictionary<TKey, TValue> collection)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (var item in collection)
            {
                if (!source.ContainsKey(item.Key))
                {
                    source.Add(item.Key, item.Value);
                }
            }
        }
    }
}
