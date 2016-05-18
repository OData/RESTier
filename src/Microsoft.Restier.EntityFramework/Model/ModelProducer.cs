// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if EF7
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.EntityFramework.Model
{
    /// <summary>
    /// Represents a model producer that uses the
    /// metadata workspace accessible from a DbContext.
    /// </summary>
    internal class ModelProducer : IModelBuilder
    {
        /// <summary>
        /// This class will not real build a model, but only get entityset name and eitity map from data source
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

            var entitySetTypeMapCollection = new Collection<KeyValuePair<string, Type>>();
            var apiContext = context.ApiContext;
            var dbContext = apiContext.GetApiService<DbContext>();

            List<PropertyInfo> props =GetDbSetProperties(dbContext);
            foreach (var prop in props)
            {
                var type = prop.PropertyType.GenericTypeArguments[0];
                var pair = new KeyValuePair<string, Type>(prop.Name, type);
                entitySetTypeMapCollection.Add(pair);
            }

            context.EntitySetTypeMapCollection = entitySetTypeMapCollection;
            return Task.FromResult<IEdmModel>(null);
        }

        internal static List<PropertyInfo> GetDbSetProperties(DbContext dbContext)
        {
            var dbSetProperties = new List<PropertyInfo>();
            var properties = dbContext.GetType().GetProperties();

            foreach (var property in properties)
            {
                var type = property.PropertyType;
#if EF7
                var genericType = type.FindGenericType(typeof(DbSet<>));
#else
                var genericType = type.FindGenericType(typeof(IDbSet<>));
#endif

                if (genericType != null)
                {
                    dbSetProperties.Add(property);
                }
            }

            return dbSetProperties;
        }
    }
}
