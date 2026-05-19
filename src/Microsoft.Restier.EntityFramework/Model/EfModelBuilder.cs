// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Microsoft.Restier.EntityFramework;

/// <summary>
/// Represents a model producer that uses the metadata workspace accessible from a <see cref="DbContext" />.
/// </summary>
public partial class EFModelBuilder<TDbContext> : IModelBuilder
    where TDbContext : DbContext
{
    private void EntityFramework6GetEntitySets(
        out Dictionary<string, Type> entitySetMap,
        out Dictionary<Type, ICollection<PropertyInfo>> entitySetKeyMap,
        out Dictionary<string, Func<object, IQueryable>> sourceFactoryMap)
    {
        var efModel = (_dbContext as IObjectContextAdapter).ObjectContext.MetadataWorkspace;

        // @robertmclaws: The query below actually returns all registered Containers
        // across all registered DbContexts.
        // It is likely a bug in some other part of OData. But we can roll with it.
        var efEntityContainers = efModel.GetItems<EntityContainer>(DataSpace.CSpace);

        // @robertmclaws: Because of the bug above, we should not make any assumptions about what is returned,
        // and get the specific container we want to use. Even if the bug gets fixed, the next line should still
        // continue to work.
        var efEntityContainer = efEntityContainers.FirstOrDefault(c => c.Name == _dbContext.GetType().Name);

        // @robertmclaws: Now that we're doing a proper FirstOrDefault() instead of a Single(),
        // we won't crash if more than one is returned, and we can check for null
        // and inform the user specifically what happened.
        if (efEntityContainer is null)
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
                    containerNames[..^2],
                    _dbContext.GetType().Name));
            }

            // @robertmclaws: In this case, we only had one DbContext available, and if wasn't the right one.
            throw new Exception(string.Format(
                CultureInfo.InvariantCulture,
                Resources.DbContextCouldNotBeFoundException,
                efEntityContainers[0].Name,
                _dbContext.GetType().Name));
        }

        entitySetMap = [];
        entitySetKeyMap = [];
        sourceFactoryMap = [];

        var itemCollection = (ObjectItemCollection)efModel.GetItemCollection(DataSpace.OSpace);
        var containerName = efEntityContainer.Name;

        // Capture the DbContext properties once for fast property-backed source-factory resolution.
        var contextProperties = _dbContext.GetType().GetProperties();

        foreach (var efEntitySet in efEntityContainer.EntitySets)
        {
            var efEntityType = efEntitySet.ElementType;
            var objectSpaceType = efModel.GetObjectSpaceType(efEntityType);
            var clrType = itemCollection.GetClrType(objectSpaceType);

            // RWM: We should not have to do this, and should not be getting here more than once.
            if (entitySetMap.ContainsKey(efEntitySet.Name))
            {
                continue;
            }

            // As entity set name and type map
            entitySetMap.Add(efEntitySet.Name, clrType);

            // Normalise: an empty list signals "keyless" to the shared builder.
            // (EF6 normally returns a populated list for keyed entities, empty for keyless views.)
            var keyProperties = efEntityType.KeyProperties
                .Select(property => clrType.GetProperty(property.Name))
                .Where(p => p != null)
                .ToList();
            entitySetKeyMap.Add(clrType, keyProperties);

            // Source factory: prefer reflection on a CLR property whose element type matches the
            // discovered EDM entity set. We scan for properties whose type is assignable to
            // IQueryable<clrType> — that covers DbSet<T>, IDbSet<T>, and DbQuery<T> in one check
            // (all three implement IQueryable<T>). If no property matches, fall back to
            // ObjectContext.CreateQuery for EDMX-only entity sets.
            //
            // NOTE: discovery happens via efEntityContainer.EntitySets above — i.e. the EDM
            // model. A DbQuery<T> or DbSet<T> that is NOT configured as an entity in the model
            // does not appear here. That's by design: ObjectContext can only query EDM entity
            // sets, so a property not in the EDM has no underlying entity set to query against.
            var iqueryableOfClr = typeof(IQueryable<>).MakeGenericType(clrType);
            var matchingProp = contextProperties.FirstOrDefault(p =>
                iqueryableOfClr.IsAssignableFrom(p.PropertyType));

            Func<object, IQueryable> sourceFactory;
            if (matchingProp is not null)
            {
                var capturedProp = matchingProp;
                sourceFactory = api =>
                {
                    var ctx = ((IEntityFrameworkApi)api).DbContext;
                    return (IQueryable)capturedProp.GetValue(ctx);
                };
            }
            else
            {
                var capturedClrType = clrType;
                var capturedContainerName = containerName;
                var capturedEntitySetName = efEntitySet.Name;
                sourceFactory = api =>
                {
                    var ctx = (DbContext)((IEntityFrameworkApi)api).DbContext;
                    var oc = ((IObjectContextAdapter)ctx).ObjectContext;
                    var createQuery = typeof(ObjectContext).GetMethod(nameof(ObjectContext.CreateQuery))
                        .MakeGenericMethod(capturedClrType);
                    var esql = $"[{capturedContainerName}].[{capturedEntitySetName}]";
                    return (IQueryable)createQuery.Invoke(oc, new object[] { esql, Array.Empty<ObjectParameter>() });
                };
            }

            sourceFactoryMap[efEntitySet.Name] = sourceFactory;
        }
    }
}
