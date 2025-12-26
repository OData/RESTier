// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// The service for model generation.
    /// </summary>
    public interface IModelBuilder
    {
        /// <summary>
        /// Asynchronously gets an API model for an API.
        /// </summary>
        /// <param name="context">
        /// The context for processing
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is the API model.
        /// </returns>
        IEdmModel GetModel(ModelContext context);

    }

}
