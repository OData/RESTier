// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if EF7
using Microsoft.Data.Entity;
#else
using System.Data.Entity;
#endif
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.EntityFramework.Model
{
    /// <summary>
    /// Represents a model mapper based on a DbContext.
    /// </summary>
    public class ModelMapper : IModelMapper
    {
        private readonly Type dbContextType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelMapper" /> class.
        /// </summary>
        /// <param name="dbContextType">
        /// The type of a DbContext class.
        /// </param>
        public ModelMapper(Type dbContextType)
        {
            Ensure.NotNull(dbContextType, "dbContextType");
            this.dbContextType = dbContextType;
        }

        /// <summary>
        /// Tries to get the relevant type of an entity
        /// set, singleton, or composable function import.
        /// </summary>
        /// <param name="context">
        /// A domain context.
        /// </param>
        /// <param name="name">
        /// The name of an entity set, singleton or composable function import.
        /// </param>
        /// <param name="relevantType">
        /// When this method returns, provides the
        /// relevant type of the queryable source.
        /// </param>
        /// <returns>
        /// <c>true</c> if the relevant type was
        /// provided; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetRelevantType(
            DomainContext context,
            string name,
            out Type relevantType)
        {
            // TODO GitHubIssue#39 : support something beyond entity sets
            relevantType = null;
            var property = this.dbContextType.GetProperty(name);
            if (property != null)
            {
                var type = property.PropertyType;
                var parent = GetGenericParent(type);
                if (parent != null)
                {
                    relevantType = parent.GetGenericArguments()[0];
                }
            }

            return relevantType != null;
        }

        /// <summary>
        /// Finds the <c>IDbSet</c> interface this type implements.
        /// </summary>
        /// <param name="type">
        /// The type of the entity set.
        /// </param>
        /// <returns>
        /// the type itself if it is defined as DbSet or IDbSet,
        /// type of IDbSet interface implemented by this type if there is;
        /// otherwise, null
        /// </returns>
        private Type GetGenericParent(Type type)
        {
            // Because usage of DbSet very common, first check for it to speed things up
            if (type.IsGenericType)
            {
                var generic = type.GetGenericTypeDefinition();
                if (generic == typeof(DbSet<>) || generic == typeof(IDbSet<>))
                {
                    return type;
                }
            }

            return
                type.GetInterfaces()
                    .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof (IDbSet<>));
        }

        /// <summary>
        /// Tries to get the relevant type of a composable function.
        /// </summary>
        /// <param name="context">
        /// A domain context.
        /// </param>
        /// <param name="namespaceName">
        /// The name of a namespace containing a composable function.
        /// </param>
        /// <param name="name">
        /// The name of composable function.
        /// </param>
        /// <param name="relevantType">
        /// When this method returns, provides the
        /// relevant type of the composable function.
        /// </param>
        /// <returns>
        /// <c>true</c> if the relevant type was
        /// provided; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetRelevantType(
            DomainContext context,
            string namespaceName,
            string name,
            out Type relevantType)
        {
            // TODO GitHubIssue#39 : support composable function imports
            relevantType = null;
            return false;
        }
    }
}
