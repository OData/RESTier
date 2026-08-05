// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.Restier.Core.DependencyInjection;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// The service for model generation.
    /// </summary>
    public interface IModelBuilder : IChainedService<IModelBuilder>
    {
        /// <summary>
        /// Asynchronously gets an API model for an API.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is the API model.
        /// </returns>
        IEdmModel GetEdmModel();

    }
}
