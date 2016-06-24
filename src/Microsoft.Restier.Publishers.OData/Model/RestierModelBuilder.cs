// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Publishers.OData.Model
{
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

            var entitySetTypeMapDictionary = context.EntitySetTypeMapDictionary;
            if (entitySetTypeMapDictionary == null || entitySetTypeMapDictionary.Count == 0)
            {
                return null;
            }

            // Collection is set by EF now, and EF model producer will not build model any more
            // Web Api OData conversion model built is been used here,
            // refer to Web Api OData document for the detail conversions been used for model built.
            var builder = new ODataConventionModelBuilder();
            MethodInfo method = typeof(ODataConventionModelBuilder)
                .GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (var pair in entitySetTypeMapDictionary)
            {
                // Build a method with the specific type argument
                var specifiedMethod = method.MakeGenericMethod(pair.Value);
                var parameters = new object[]
                {
                      pair.Key
                };

                specifiedMethod.Invoke(builder, parameters);
            }

            entitySetTypeMapDictionary.Clear();

            var entityTypeKeyPropertiesMapDictionary = context.EntityTypeKeyPropertiesMapDictionary;
            if (entityTypeKeyPropertiesMapDictionary != null)
            {
                foreach (var pair in entityTypeKeyPropertiesMapDictionary)
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

                entityTypeKeyPropertiesMapDictionary.Clear();
            }

            return builder.GetEdmModel();
        }
    }
}
