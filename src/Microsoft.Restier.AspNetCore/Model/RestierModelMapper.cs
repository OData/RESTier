// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// Represents a model mapper based on the types added to the EdmModel.
    /// </summary>
    public class RestierModelMapper : IModelMapper
    {

        /// <summary>
        /// Gets or sets the inner mapper.
        /// </summary>
        public IModelMapper Inner { get; set; }

        /// <summary>
        /// Tries to get the relevant type of an entity set, singleton,
        /// or unbound function import (e.g. a keyless-view dispatch entry point).
        /// </summary>
        /// <param name="invocationContext">The invocationContext for model mapper.</param>
        /// <param name="name">The name of an entity set, singleton, or unbound function import.</param>
        /// <param name="relevantType">When this method returns, provides the relevant type of the queryable source.</param>
        /// <returns>
        /// <c>true</c> if the relevant type was provided; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetRelevantType(InvocationContext invocationContext, string name,  out Type relevantType)
        {
            Ensure.NotNull(invocationContext, nameof(invocationContext));

            var model = invocationContext.Api.Model;

            var element = model.EntityContainer.Elements.Where(e => e.Name == name).FirstOrDefault();

            if (element is not null)
            {
                IEdmType targetType = null;
                if (element is IEdmEntitySet entitySet)
                {
                    var entitySetType = entitySet.Type as IEdmCollectionType;
                    targetType = entitySetType.ElementType.Definition;
                }
                else if (element is IEdmSingleton singleton)
                {
                    targetType = singleton.Type;
                }
                else if (element is IEdmFunctionImport functionImport)
                {
                    // Keyless-view shape: an unbound function import returning Collection(<ComplexType>).
                    // The ClrTypeAnnotation is attached to the return-type element by EFModelBuilder
                    // when it demotes keyless DbSets/EntitySets to ComplexType<T> + FunctionImport.
                    var returnTypeRef = functionImport.Function.GetReturn()?.Type;
                    if (returnTypeRef is not null && returnTypeRef.IsCollection())
                    {
                        var collectionType = (IEdmCollectionType)returnTypeRef.Definition;
                        targetType = collectionType.ElementType.Definition;
                    }
                }

                if (targetType is not null)
                {
                    var annotation = model.GetAnnotationValue<ClrTypeAnnotation>(targetType);
                    if (annotation is not null)
                    {
                        relevantType = annotation.ClrType;
                        return true;
                    }
                }
            }

            return Inner.TryGetRelevantType(invocationContext, name, out relevantType);
        }

        /// <summary>
        /// Tries to get the relevant type of a composable function.
        /// </summary>
        /// <param name="context">The invocationContext for model mapper.</param>
        /// <param name="namespaceName">The name of a namespace containing a composable function.</param>
        /// <param name="name">The name of composable function.</param>
        /// <param name="relevantType">When this method returns, provides the relevant type of the composable function.</param>
        /// <returns>
        /// <c>true</c> if the relevant type was provided; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetRelevantType(InvocationContext context, string namespaceName, string name, out Type relevantType)
        {
            // TODO GitHubIssue#39 : support composable function imports
            // relevantType = null;
            return Inner.TryGetRelevantType(context, namespaceName, name, out relevantType);
        }
    }
}
