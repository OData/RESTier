// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Model;

/// <summary>
/// Merges models.
/// </summary>
public class ModelMerger
{
    /// <summary>
    /// Merges the source model into the target model.
    /// </summary>
    /// <param name="sourceModel">The source model.</param>
    /// <param name="targetModel"></param>
    public void Merge(IEdmModel sourceModel, EdmModel targetModel)
    {
        foreach (var element in sourceModel.SchemaElements)
        {
            if (element is not EdmEntityContainer)
            {
                targetModel.AddElement(element);
            }
        }

        foreach (var annotation in sourceModel.VocabularyAnnotations)
        {
            targetModel.AddVocabularyAnnotation(annotation);
        }

        var targetEntityContainer = (EdmEntityContainer)targetModel.EntityContainer;
        var sourceEntityContainer = (EdmEntityContainer)sourceModel.EntityContainer;
        if (sourceEntityContainer is null)
        {
            return;
        }

        foreach (var entityset in sourceEntityContainer.EntitySets())
        {
            if (targetEntityContainer.FindEntitySet(entityset.Name) is null)
            {
                targetEntityContainer.AddEntitySet(entityset.Name, entityset.EntityType);
            }
        }

        foreach (var singleton in sourceEntityContainer.Singletons())
        {
            if (targetEntityContainer.FindEntitySet(singleton.Name) is null)
            {
                targetEntityContainer.AddSingleton(singleton.Name, singleton.EntityType);
            }
        }

        foreach (var operation in sourceEntityContainer.OperationImports())
        {
            if (targetEntityContainer.FindOperationImports(operation.Name) is not null)
            {
                continue;
            }

            if (operation.IsFunctionImport())
            {
                targetEntityContainer.AddFunctionImport(operation.Name, (EdmFunction)operation.Operation,
                    operation.EntitySet);
            }
            else
            {
                targetEntityContainer.AddActionImport(operation.Name, (EdmAction)operation.Operation,
                    operation.EntitySet);
            }
        }
    }
}