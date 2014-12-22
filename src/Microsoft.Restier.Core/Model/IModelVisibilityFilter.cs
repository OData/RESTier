// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// Represents a hook point that controls the
    /// visibility of securable model elements.
    /// </summary>
    /// <remarks>
    /// This is a multi-cast hook point whose instances
    /// are used in the reverse order of registration.
    /// </remarks>
    public interface IModelVisibilityFilter
    {
        /// <summary>
        /// Indicates if a schema element is currently visible.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="context">
        /// An optional invocation context.
        /// </param>
        /// <param name="model">
        /// A model.
        /// </param>
        /// <param name="element">
        /// A schema element.
        /// </param>
        /// <returns>
        /// <c>true</c> if the element is currently
        /// visible; otherwise, <c>false</c>.
        /// </returns>
        bool IsVisible(
            DomainConfiguration configuration,
            InvocationContext context,
            IEdmModel model,
            IEdmSchemaElement element);

        /// <summary>
        /// Indicates if an entity container element is currently visible.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="context">
        /// An optional invocation context.
        /// </param>
        /// <param name="model">
        /// A model.
        /// </param>
        /// <param name="element">
        /// An entity container element.
        /// </param>
        /// <returns>
        /// <c>true</c> if the element is currently
        /// visible; otherwise, <c>false</c>.
        /// </returns>
        bool IsVisible(
            DomainConfiguration configuration,
            InvocationContext context,
            IEdmModel model,
            IEdmEntityContainerElement element);
    }
}
