// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// Represents the default model handler.
    /// </summary>
    public class DefaultModelHandler : IModelHandler
    {
        /// <summary>
        /// Asynchronously executes the model flow.
        /// </summary>
        /// <param name="context">
        /// The model context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a domain model.
        /// </returns>
        public async Task<IEdmModel> GetModelAsync(
            ModelContext context,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");

            // STEP 1: produce model
            var producer = context.GetHookPoint<IModelProducer>();
            if (producer != null)
            {
                context.Model = await producer.ProduceModelAsync(
                    context, cancellationToken);
            }
            if (context.Model == null)
            {
                context.Model = new EdmModel();
            }

            // STEP 2: extend model
            var extenders = context.GetHookPoints<IModelExtender>();
            foreach (var extender in extenders)
            {
                await extender.ExtendModelAsync(context, cancellationToken);
            }

            return context.Model;
        }
    }
}
