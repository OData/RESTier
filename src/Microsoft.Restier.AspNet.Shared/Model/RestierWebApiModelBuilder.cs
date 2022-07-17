// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

#if NETCOREAPP3_1_OR_GREATER
namespace Microsoft.Restier.AspNetCore.Model
#else
namespace Microsoft.Restier.AspNet.Model
#endif
{
    /// <summary>
    /// This is a RESTier model build which retrieve information from providers like entity framework provider,
    /// then build entity set and entity type based on retrieved information.
    /// </summary>
    internal class RestierWebApiModelBuilder : IModelBuilder
    {
        /// <summary>
        /// Gets or sets the Inner model builder.
        /// </summary>
        public IModelBuilder InnerModelBuilder { get; set; }

        /// <inheritdoc/>
        public IEdmModel GetModel(ModelContext context)
        {
            // This means user build a model with customized model builder registered as inner most
            // Its element will be added to built model.
            IEdmModel innerModel = null;
            if (InnerModelBuilder is not null)
            {
                innerModel = InnerModelBuilder.GetModel(context);
            }

            var entitySetTypeMap = context.ResourceSetTypeMap;
            if (entitySetTypeMap is null || entitySetTypeMap.Count == 0)
            {
                return innerModel;
            }

            // Collection of entity type and set name is set by EF now,
            // and EF model producer will not build model any more
            // Web Api OData conversion model built is been used here,
            // refer to Web Api OData document for the detail conversions been used for model built.
            var builder = new ODataConventionModelBuilder
            {
                // This namespace is used by container
                Namespace = entitySetTypeMap.First().Value.Namespace
            };

            var method = typeof(ODataConventionModelBuilder).GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (var pair in entitySetTypeMap)
            {
                // Build a method with the specific type argument
                var specifiedMethod = method.MakeGenericMethod(pair.Value);
                var parameters = new object[]
                {
                      pair.Key,
                };

                specifiedMethod.Invoke(builder, parameters);
            }

            entitySetTypeMap.Clear();

            if (context.ResourceTypeKeyPropertiesMap is not null)
            {
                foreach (var pair in context.ResourceTypeKeyPropertiesMap)
                {
                    if (builder.GetTypeConfigurationOrNull(pair.Key) is not EntityTypeConfiguration edmTypeConfiguration)
                    {
                        continue;
                    }

#if NETCOREAPP3_1_OR_GREATER
                    if (pair.Value is null)
                    {
                        throw new InvalidOperationException($"The entity '{pair.Key}' does not have a key specified. Entities tagged with the [Keyless] attribute " +
                            $"(or otherwise do not have a key specified) are not supported in either OData or Restier. Please map the object as a ComplexType and " +
                            $"implement as an [UnboundOperation] on your API instead.");
                    }
#endif

                    foreach (var property in pair.Value)
                    {
                        edmTypeConfiguration.HasKey(property);
                    }
                }

                context.ResourceTypeKeyPropertiesMap.Clear();
            }

            var model = (EdmModel)builder.GetEdmModel();

            // Add all Inner model content into existing model
            // When WebApi OData make conversion model builder accept an existing model, this can be removed.
            if (innerModel is not null)
            {
                foreach (var element in innerModel.SchemaElements)
                {
                    if (element is not EdmEntityContainer)
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
                if (innerEntityContainer is not null)
                {
                    foreach (var entityset in innerEntityContainer.EntitySets())
                    {
                        if (entityContainer.FindEntitySet(entityset.Name) is null)
                        {
                            entityContainer.AddEntitySet(entityset.Name, entityset.EntityType());
                        }
                    }

                    foreach (var singleton in innerEntityContainer.Singletons())
                    {
                        if (entityContainer.FindEntitySet(singleton.Name) is null)
                        {
                            entityContainer.AddSingleton(singleton.Name, singleton.EntityType());
                        }
                    }

                    foreach (var operation in innerEntityContainer.OperationImports())
                    {
                        if (entityContainer.FindOperationImports(operation.Name) is null)
                        {
                            if (operation.IsFunctionImport())
                            {
                                entityContainer.AddFunctionImport(operation.Name, (EdmFunction)operation.Operation, operation.EntitySet);
                            }
                            else
                            {
                                entityContainer.AddActionImport(operation.Name, (EdmAction)operation.Operation, operation.EntitySet);
                            }
                        }
                    }
                }
            }

            return model;
        }
    }
}
