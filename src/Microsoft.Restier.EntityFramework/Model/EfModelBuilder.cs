// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
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
    private void EntityFramework6GetEntitySets(out Dictionary<string, Type> entitySetMap, out Dictionary<Type, ICollection<PropertyInfo>> entitySetKeyMap)
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

        var itemCollection = (ObjectItemCollection)efModel.GetItemCollection(DataSpace.OSpace);

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

            // Keyless entity sets are an EF6 EDMX-only construct. The keyless-views feature (#741)
            // is EF Core-only — EF6 code-first cannot declare keyless types, and the EDMX path is
            // explicitly out of scope. If we see an empty KeyProperties list here, surface it as
            // the same InvalidOperationException the original code threw for EFCore null keys, so
            // the user gets a clear "not supported" signal rather than a downstream OData error.
            if (efEntityType.KeyProperties.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The entity '{efEntitySet.Name}' does not have a key specified. Keyless entity types " +
                    $"are not supported on the Entity Framework 6 provider. Please define a key, or use the " +
                    $"Entity Framework Core provider (which supports keyless types via [Keyless] / HasNoKey()).");
            }

            var keyProperties = efEntityType.KeyProperties.Select(property => clrType.GetProperty(property.Name)).ToList();

            entitySetKeyMap.Add(clrType, keyProperties);
        }
    }
}
