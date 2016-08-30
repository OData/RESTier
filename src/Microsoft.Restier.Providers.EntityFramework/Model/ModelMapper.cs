// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if EF7
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Providers.EntityFramework
{
    /// <summary>
    /// Represents a model mapper based on a DbContext.
    /// </summary>
    internal class ModelMapper : IModelMapper
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
        /// The context for model mapper.
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
            ModelContext context,
            string name,
            out Type relevantType)
        {
            // TODO GitHubIssue#39 : support something beyond entity sets
            relevantType = null;
            var property = this.dbContextType.GetProperty(name);
            if (property != null)
            {
                var type = property.PropertyType;
#if EF7
                var genericType = type.FindGenericType(typeof(DbSet<>));
#else
                var genericType = type.FindGenericType(typeof(IDbSet<>));
#endif
                if (genericType != null)
                {
                    relevantType = genericType.GetGenericArguments()[0];
                }
            }

            return relevantType != null;
        }

        /// <summary>
        /// Tries to get the relevant type of a composable function.
        /// </summary>
        /// <param name="context">
        /// The context for model mapper.
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
            ModelContext context,
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
