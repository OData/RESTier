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
            // This means user build a model with customized model builder registered as inner most
            // Its element will be added to built model.
            IEdmModel innerModel = null;
            if (InnerModelBuilder != null)
            {
                innerModel = await InnerModelBuilder.GetModelAsync(context, cancellationToken);
            }

            var entitySetTypeMap = context.ResourceSetTypeMap;
            if (entitySetTypeMap == null || entitySetTypeMap.Count == 0)
            {
                return innerModel;
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

            var entityTypeKeyPropertiesMap = context.ResourceTypeKeyPropertiesMap;
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

            var model = (EdmModel)builder.GetEdmModel();

            // Add all Inner model content into existing model
            // When WebApi OData make conversion model builder accept an existing model, this can be removed.
            if (innerModel != null)
            {
                foreach (var element in innerModel.SchemaElements)
                {
                    if (!(element is EdmEntityContainer))
                    {
                        model.AddElement(element);
                    }
                }

                foreach (var annotation in innerModel.VocabularyAnnotations)
                {
                    model.AddVocabularyAnnotation(annotation);
                }

                var entityContainer = (EdmEntityContainer)model.EntityContainer;
                var innerEntityContainer = (EdmEntityContainer)innerModel.EntityContainer;
                if (innerEntityContainer != null)
                {
                    foreach (var entityset in innerEntityContainer.EntitySets())
                    {
                        if (entityContainer.FindEntitySet(entityset.Name) == null)
                        {
                            entityContainer.AddEntitySet(entityset.Name, entityset.EntityType());
                        }
                    }

                    foreach (var singleton in innerEntityContainer.Singletons())
                    {
                        if (entityContainer.FindEntitySet(singleton.Name) == null)
                        {
                            entityContainer.AddSingleton(singleton.Name, singleton.EntityType());
                        }
                    }

                    foreach (var operation in innerEntityContainer.OperationImports())
                    {
                        if (entityContainer.FindOperationImports(operation.Name) == null)
                        {
                            if (operation.IsFunctionImport())
                            {
                                entityContainer.AddFunctionImport(
                                    operation.Name, (EdmFunction)operation.Operation, operation.EntitySet);
                            }
                            else
                            {
                                entityContainer.AddActionImport(
                                    operation.Name, (EdmAction)operation.Operation, operation.EntitySet);
                            }
                        }
                    }
                }
            }

            return model;
        }
    }
}
