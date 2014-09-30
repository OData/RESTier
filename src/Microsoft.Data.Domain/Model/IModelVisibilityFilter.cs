// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Microsoft.OData.Edm;

namespace Microsoft.Data.Domain.Model
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
