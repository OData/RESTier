// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Publishers.OData.Model
{
    /// <summary>
    /// This is a RESTier model build which retrieve information from providers like entity framework provider,
    /// then build entity set and entity type based on retrieved information.
    /// </summary>
    internal class RestierModelBuilder : IModelBuilder
    {
        public IModelBuilder InnerModelBuilder { get; set; }

        /// <inheritdoc/>
        public async Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            // This means user build a model with customized model builder registered as inner most,
            // no logic will be done here
            if (InnerModelBuilder != null)
            {
                var innerModel = await InnerModelBuilder.GetModelAsync(context, cancellationToken);
                if (innerModel != null)
                {
                    return innerModel;
                }
            }

            var entitySetTypeMap = context.EntitySetTypeMap;
            if (entitySetTypeMap == null || entitySetTypeMap.Count == 0)
            {
                return null;
            }

            // Collection of entity type and set name is set by EF now,
            // and EF model producer will not build model any more
            // Web Api OData conversion model built is been used here,
            // refer to Web Api OData document for the detail conversions been used for model built.
            var builder = new ODataConventionModelBuilder();

            // This namespace is used by container
            builder.Namespace = entitySetTypeMap.First().Value.Namespace;

            MethodInfo method = typeof(ODataConventionModelBuilder)
                .GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (var pair in entitySetTypeMap)
            {
                // Build a method with the specific type argument
                var specifiedMethod = method.MakeGenericMethod(pair.Value);
                var parameters = new object[]
                {
                      pair.Key
                };

                specifiedMethod.Invoke(builder, parameters);
            }

            entitySetTypeMap.Clear();

            var entityTypeKeyPropertiesMap = context.EntityTypeKeyPropertiesMap;
            if (entityTypeKeyPropertiesMap != null)
            {
                foreach (var pair in entityTypeKeyPropertiesMap)
                {
                    var edmTypeConfiguration = builder.GetTypeConfigurationOrNull(pair.Key) as EntityTypeConfiguration;
                    if (edmTypeConfiguration == null)
                    {
                        continue;
                    }

                    foreach (var property in pair.Value)
                    {
                        edmTypeConfiguration.HasKey(property);
                    }
                }

                entityTypeKeyPropertiesMap.Clear();
            }

            return builder.GetEdmModel();
        }
    }
}
