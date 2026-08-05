// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.EntityFrameworkCore;

/// <summary>
/// Represents a model producer that uses the metadata workspace accessible from a <see cref="DbContext" />.
/// </summary>
public partial class EFModelBuilder<TDbContext> : IModelBuilder
    where TDbContext : DbContext
{
    private void EntityFrameworkCoreGetEntities(
        out Dictionary<string, Type> entitySetMap,
        out Dictionary<Type, ICollection<PropertyInfo>> entitySetKeyMap,
        out Dictionary<string, Func<object, IQueryable>> sourceFactoryMap)
    {
        // @robertmclaws: Validate that no Owned Types are mapped to DbSet<>. If there are, EFCore calls to GetModel will fail.
        var ownedTypes = _dbContext.Model.GetEntityTypes().Where(c => c.IsOwned()).ToList();
        var dbSetMappedTypes = ownedTypes.Where(c => _dbContext.IsDbSetMapped(c.ClrType)).ToList();

        if (dbSetMappedTypes.Count > 0)
        {
            throw new EdmModelValidationException($"The '{_dbContext.GetType().Name}' DbContext has 'Owned Types' (the EFCore equivalent of EF6's 'Complex Types') mapped to DbSets. " +
                                                  $"You must remove the following DbSet mappings for EFCore to function properly with Restier: {string.Join(",", dbSetMappedTypes.Select(c => c.ShortName()))}");
        }

        // Map { DbSet property name -> CLR type }.
        var dbSetProperties = _dbContext.GetType().GetProperties()
            .Where(e => e.PropertyType.FindGenericType(typeof(DbSet<>)) is not null)
            .ToList();

        entitySetMap = dbSetProperties.ToDictionary(e => e.Name, e => e.PropertyType.GetGenericArguments()[0]);

        // Map { entity-set name -> source factory } via reflection on the DbSet property captured here.
        sourceFactoryMap = dbSetProperties.ToDictionary(
            p => p.Name,
            p =>
            {
                var capturedProp = p;
                Func<object, IQueryable> factory = api =>
                {
                    var ctx = ((IEntityFrameworkApi)api).DbContext;
                    return (IQueryable)capturedProp.GetValue(ctx);
                };
                return factory;
            });

        entitySetKeyMap = _dbContext.Model.GetEntityTypes().Where(c => !c.IsOwned() && !IsImplicitManyToManyJoinEntity(c)).ToDictionary(
                        e => e.ClrType,
                        e => ((ICollection<PropertyInfo>)e.FindPrimaryKey()?.Properties.Select(p => e.ClrType?.GetProperty(p.Name)).ToList()));
    }

    /// <summary>
    /// A replacement for IsImplicitlyCreatedJoinEntityType, since on EF Core 6.0 Model.GetEntityTypes() returns RuntimeEntityTypes instead of EntityTypes.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private static bool IsImplicitManyToManyJoinEntity(IEntityType entity) =>
        entity.ClrType == typeof(Dictionary<string, object>) && entity.GetForeignKeys().Count() == 2 && entity.GetProperties().Count() == 2;
}